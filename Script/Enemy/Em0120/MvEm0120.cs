using System.Collections;
using Pathfinding;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0120 : MvEnemyBase
    {
        // Flying enemy: dùng A* Pathfinding AIPath, đặt Rigidbody2D.gravityScale = 0 trong Inspector.

        [Header("AIPath Movement")]
        [SerializeField] private float patrolSpeed = 2.4f;
        [SerializeField] private float chaseSpeed = 4.2f;
        [SerializeField] private float returnToOriginAStarSpeed = 2.8f;
        [SerializeField] private float destinationStopDistance = 0.25f;

        [Header("Patrol")]
        [SerializeField] private float flyPatrolRadius = 3.5f;
        [SerializeField] private float patrolRetargetMinTime = 0.8f;
        [SerializeField] private float patrolRetargetMaxTime = 1.6f;

        [Header("Damage Retreat")]
        [SerializeField] private float damageRetreatDistance = 1.2f;
        [SerializeField] private float damageRetreatDuration = 0.18f;
        [SerializeField] private float damageRetreatSpeed = 5.2f;
        [SerializeField] private float aiPathResumeDelay = 1.2f;

        // --- Internal state ---
        private Rigidbody2D cachedRb;
        private IAstarAI aStarAI;          // Interface trực tiếp, không dùng Reflection
        private Vector2 patrolAnchor;
        private Vector2 patrolDestination;
        private bool hasPatrolDestination;
        private float nextPatrolRetargetTime;
        private bool isDamageRetreatActive;
        private float damageRetreatTimer;
        private Vector2 damageRetreatDestination;
        private bool isAStarPausedByHit;
        private float aStarResumeTimer;
        private Coroutine hitRecoveryRoutine;

        // ----------------------------------------------------------------
        // State factory
        // ----------------------------------------------------------------
        protected override EnemyState CreateIdleState(EnemyContext context)
            => new Em0120IdleState(context);

        protected override EnemyState CreateRunState(EnemyContext context)
            => new Em0120RunState(context);

        protected override EnemyState CreateHitState(EnemyContext context)
            => new Em0120HitState(context);

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------
        protected override void Awake()
        {
            base.Awake();

            cachedRb     = GetComponent<Rigidbody2D>();
            patrolAnchor = transform.position;

            // AIPath cần Kinematic body để dùng MovePosition()
            if (cachedRb != null)
            {
                if (cachedRb.bodyType == RigidbodyType2D.Static)
                    cachedRb.bodyType = RigidbodyType2D.Kinematic;

                cachedRb.gravityScale          = 0f;
                cachedRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                cachedRb.simulated             = true;
            }

            // Lấy IAstarAI trực tiếp — sẽ tìm AIPath, AILerp, RichAI
            RefreshAStarReferences();

            if (aStarAI == null)
                Debug.LogError($"[MvEm0120] Không tìm thấy IAstarAI (AIPath) trên {gameObject.name}. " +
                               "Hãy thêm component AIPath vào cùng GameObject.", this);
        }

        /// <summary>
        /// Override để ngăn base class gọi ApplyKinematicVerticalMotion()
        /// — AIPath đã tự di chuyển body, base class can thiệp thêm sẽ xung đột.
        /// </summary>
        protected override void FixedUpdate()
        {
            // Không gọi base.FixedUpdate() →
            // ApplyKinematicVerticalMotion() không chạy → không gravity thủ công.
            // AIPath xử lý toàn bộ movement qua rb.MovePosition() nội bộ.
        }

        private void OnDisable()
        {
            StopAStarMovement();

            isDamageRetreatActive = false;
            damageRetreatTimer = 0f;
            isAStarPausedByHit = false;
            aStarResumeTimer = 0f;
            hitRecoveryRoutine = null;
        }

        public override void TakeDamage(float damage, GameObject damageSource = null)
        {
            TakeDamage(damage, damageSource, null);
        }

        public override void TakeDamage(float damage, GameObject damageSource = null, Vector3? damageTextWorldPosition = null)
        {
            if (!IsAlive)
                return;

            base.TakeDamage(damage, damageSource, damageTextWorldPosition);

            if (!IsAlive)
                return;

            StartDamageRetreat(damageSource);
            ForceHitReaction();
        }

        internal bool IsDamageRetreatActive => isDamageRetreatActive;
        internal bool IsAStarPausedByHit => isAStarPausedByHit;

        // ----------------------------------------------------------------
        // Public API gọi từ Em0120RunState
        // ----------------------------------------------------------------
        public void TickFlyMotion(bool updateAnimation = true)
        {
            if (!IsAlive)
                return;

            if (aStarAI == null)
            {
                // Thử lấy lại lần cuối (phòng trường hợp Awake bị gọi sai thứ tự)
                RefreshAStarReferences();

                if (aStarAI == null)
                {
                    SetRunAnimation(false, true);
                    return;
                }
            }

            if (isDamageRetreatActive)
            {
                StopAStarMovement();
                return;
            }

            if (isAStarPausedByHit)
            {
                StopAStarMovement();
                return;
            }

            // Đảm bảo agent được phép di chuyển/tìm path khi không còn hit pause.
            EnsureAStarMovementEnabled();

            Vector2 targetDestination;
            float   speed;

            if (CurrentTarget != null)
            {
                // Có mục tiêu → đuổi theo
                targetDestination    = CurrentTarget.position;
                speed                = Mathf.Max(0f, chaseSpeed);
                ReturnToOriginTimer  = 0f;
            }
            else if (ShouldReturnToOriginDistance())
            {
                // Quá xa điểm xuất phát → về gốc
                ReturnToOriginTimer += Time.deltaTime;
                if (EnableReturnRespawn &&
                    ReturnToOriginTimer >= Mathf.Max(0.5f, ReturnRespawnTimeoutDuration))
                {
                    RespawnAtOrigin();
                    return;
                }

                targetDestination    = PatrolCenter;
                speed                = Mathf.Max(0f, returnToOriginAStarSpeed);
                hasPatrolDestination = false;
            }
            else
            {
                // Tuần tra
                ReturnToOriginTimer  = 0f;
                targetDestination    = ResolvePatrolDestination();
                speed                = Mathf.Max(0f, patrolSpeed);
            }

            MoveAStar(targetDestination, speed);
            UpdateFacingFromAStarVelocity();
            if (updateAnimation)
                SetRunAnimation(speed > 0.01f, false);
        }

        // ----------------------------------------------------------------
        // Movement helpers
        // ----------------------------------------------------------------
        private void MoveAStar(Vector2 destination, float speed)
        {
            if (aStarAI == null) return;

            // Gán destination trực tiếp qua interface — không cần Reflection
            aStarAI.destination = (Vector3)destination;
            aStarAI.maxSpeed    = speed;
            aStarAI.canMove     = speed > 0.01f;

            // KHÔNG gọi SearchPath() ở đây — AIPath tự recalculate theo Dynamic mode.
            // Gọi mỗi frame sẽ hủy path liên tục → enemy đứng yên.
        }

        private void UpdateFacingFromAStarVelocity()
        {
            if (aStarAI == null) return;

            Vector3 vel = aStarAI.velocity;
            if (Mathf.Abs(vel.x) > 0.001f)
                FaceByDeltaX(vel.x);
        }

        // ----------------------------------------------------------------
        // Patrol helpers
        // ----------------------------------------------------------------
        private bool ShouldReturnToOriginDistance()
        {
            if (!ReturnToOriginWhenTargetLost)
                return false;

            float dist      = ((Vector2)transform.position - PatrolCenter).magnitude;
            float threshold = Mathf.Max(0.05f, ReturnToOriginDistance);
            return dist > threshold;
        }

        private Vector2 ResolvePatrolDestination()
        {
            bool needNew = !hasPatrolDestination
                        || IsNearDestination(patrolDestination)
                        || Time.time >= nextPatrolRetargetTime;

            if (needNew)
                PickNewPatrolDestination();

            return patrolDestination;
        }

        private void PickNewPatrolDestination()
        {
            float  radius       = Mathf.Max(0.1f, flyPatrolRadius);
            Vector2 randomOffset = Random.insideUnitCircle * radius;
            patrolDestination    = patrolAnchor + randomOffset;
            hasPatrolDestination = true;

            float minT = Mathf.Max(0.05f, Mathf.Min(patrolRetargetMinTime, patrolRetargetMaxTime));
            float maxT = Mathf.Max(minT + 0.01f, Mathf.Max(patrolRetargetMinTime, patrolRetargetMaxTime));
            nextPatrolRetargetTime = Time.time + Random.Range(minT, maxT);
        }

        private bool IsNearDestination(Vector2 destination)
        {
            float stop = Mathf.Max(0.05f, destinationStopDistance);
            return Vector2.Distance(transform.position, destination) <= stop;
        }

        private void StartDamageRetreat(GameObject damageSource)
        {
            Vector2 from = transform.position;

            Vector2 awayDirection = Vector2.right;
            if (damageSource != null)
            {
                Vector2 sourcePos = damageSource.transform.position;
                awayDirection = from - sourcePos;
            }
            else if (CurrentTarget != null)
            {
                awayDirection = from - (Vector2)CurrentTarget.position;
            }

            if (awayDirection.sqrMagnitude < 0.0001f)
                awayDirection = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

            awayDirection.Normalize();
            float retreatDistance = Mathf.Max(0.1f, damageRetreatDistance);

            damageRetreatDestination = from + awayDirection * retreatDistance;
            damageRetreatTimer = Mathf.Max(0.02f, damageRetreatDuration);
            isDamageRetreatActive = true;
            isAStarPausedByHit = true;
            aStarResumeTimer = Mathf.Max(0f, aiPathResumeDelay);

            if (hitRecoveryRoutine != null)
                StopCoroutine(hitRecoveryRoutine);

            hitRecoveryRoutine = StartCoroutine(DamageHitRecoveryRoutine());
        }

        private IEnumerator DamageHitRecoveryRoutine()
        {
            StopAStarMovement();

            if (cachedRb != null)
                cachedRb.linearVelocity = Vector2.zero;

            if (cachedRb == null)
            {
                isDamageRetreatActive = false;
                isAStarPausedByHit = false;
                hitRecoveryRoutine = null;
                yield break;
            }

            float retreatSpeed = Mathf.Max(0.05f, damageRetreatSpeed);
            float retreatTimer = Mathf.Max(0.02f, damageRetreatDuration);
            float stopDistance = Mathf.Max(0.05f, destinationStopDistance);
            while (retreatTimer > 0f && IsAlive)
            {
                StopAStarMovement();
                retreatTimer -= Time.deltaTime;

                Vector2 current = cachedRb.position;
                Vector2 next = Vector2.MoveTowards(current, damageRetreatDestination, retreatSpeed * Time.deltaTime);
                cachedRb.position = next;

                Vector2 retreatVelocity = next - current;
                if (Mathf.Abs(retreatVelocity.x) > 0.0001f)
                    FaceByDeltaX(retreatVelocity.x);

                if (Vector2.Distance(next, damageRetreatDestination) <= stopDistance)
                    break;

                yield return null;
            }

            isDamageRetreatActive = false;
            damageRetreatTimer = 0f;

            aStarResumeTimer = Mathf.Max(0f, aiPathResumeDelay);
            while (aStarResumeTimer > 0f && IsAlive)
            {
                StopAStarMovement();
                aStarResumeTimer -= Time.deltaTime;
                yield return null;
            }

            aStarResumeTimer = 0f;
            isAStarPausedByHit = false;
            ResumeAStarMovement();
            hitRecoveryRoutine = null;
        }

        private void StopAStarMovement()
        {
            RefreshAStarReferences();

            if (aStarAI != null)
            {
                aStarAI.canMove = false;
                aStarAI.canSearch = false;
            }
        }

        private void EnsureAStarMovementEnabled()
        {
            RefreshAStarReferences();

            if (aStarAI != null)
                aStarAI.canSearch = true;
        }

        private void ResumeAStarMovement()
        {
            RefreshAStarReferences();

            if (aStarAI != null)
            {
                aStarAI.canSearch = true;
                aStarAI.canMove = true;
                aStarAI.SearchPath();
            }
        }

        private void RefreshAStarReferences()
        {
            if (aStarAI == null)
                aStarAI = GetComponent<IAstarAI>();
            if (aStarAI == null)
                aStarAI = GetComponentInChildren<IAstarAI>(true);
        }

        private void RespawnAtOrigin()
        {
            transform.position   = PatrolCenter;
            ReturnToOriginTimer  = 0f;
            hasPatrolDestination = false;

            if (cachedRb != null)
                cachedRb.linearVelocity = Vector2.zero;

            // Sau khi teleport vị trí thay đổi đột ngột → cần tính lại path ngay
            aStarAI?.SearchPath();

            Debug.Log($"[MvEm0120] {gameObject.name} respawn về PatrolCenter.", this);
        }
    }
}
