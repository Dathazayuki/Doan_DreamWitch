using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0260 : MvEnemyBase, IMvAnimEventLiteListener
    {
        [Header("HeatGas Hover")]
        [SerializeField] private bool forceZeroGravity = true;
        [SerializeField] private float hoverAmplitude = 0.18f;
        [SerializeField] private float hoverFrequency = 1.3f;
        [SerializeField] private float hoverReturnSpeed = 1.8f;
        [SerializeField] private float idleReturnHorizontalSpeed = 1.2f;
        [SerializeField] private float chaseHorizontalSpeed = 2.2f;
        [SerializeField] private float chaseVerticalSpeed = 1.7f;
        [SerializeField] private float chaseHeightOffset = 0.15f;
        [SerializeField] private float attackStopDamping = 12f;

        [Header("HeatGas VFX")]
        [SerializeField] private GameObject gasVfxObject;
        [SerializeField] private bool disableGasVfxOnAwake = true;

        [Header("HeatGas Attack Range Fallback")]
        [SerializeField] private float fallbackGasRadiusX = 1.6f;
        [SerializeField] private float fallbackGasRadiusY = 1.2f;

        protected override string IdleStateName => "Idle";
        protected override string RunStateName => "Run";
        protected override string AttackSignStateName => "AtkSign";
        protected override string AttackStateName => "Atk";
        protected override string HitStateName => "Hit";
        protected override string DeadStateName => "Death";

        private Rigidbody2D cachedRb;
        private Vector2 spawnPosition;
        private float originalGravityScale;

        protected override EnemyState CreateIdleState(EnemyContext context) => new Em0260IdleState(context);
        protected override EnemyState CreateRunState(EnemyContext context) => new Em0260FloatChaseState(context);
        protected override EnemyState CreateAttackState(EnemyContext context) => new Em0260GasAttackState(context);

        protected override void Awake()
        {
            base.Awake();

            cachedRb = GetComponent<Rigidbody2D>();
            spawnPosition = transform.position;

            if (cachedRb != null)
            {
                originalGravityScale = cachedRb.gravityScale;
                if (forceZeroGravity)
                    cachedRb.gravityScale = 0f;
            }

            if (ActiveAttack != null)
            {
                ActiveAttack.SetCloseAttackWindowImmediatelyOnEnd(true);
                ActiveAttack.SetAllowAutoHitFallback(false);
            }

            if (disableGasVfxOnAwake)
                SetGasVfxActive(false);
        }

        private void OnDisable()
        {
            SetGasVfxActive(false);
        }

        protected override bool TryEvaluateCustomAttackRange(float absX, float absY, float edgeDistanceX, out bool inAttackRange)
        {
            if (ActiveAttack != null && ActiveAttack.HasAttackTrigger)
            {
                inAttackRange = ActiveAttack.IsPlayerInsideAttackTrigger();
                return true;
            }

            float radiusX = Mathf.Max(0.05f, fallbackGasRadiusX);
            float radiusY = Mathf.Max(0.05f, fallbackGasRadiusY);
            float normalizedX = absX / radiusX;
            float normalizedY = absY / radiusY;
            inAttackRange = normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
            return true;
        }

        public void TickHoverIdle()
        {
            SetRunAnimation(false, true);

            if (cachedRb == null)
                return;

            float bob = Mathf.Sin(Time.time * Mathf.Max(0.01f, hoverFrequency) * Mathf.PI * 2f) * hoverAmplitude;
            float targetY = spawnPosition.y + bob;
            float vertical = Mathf.Clamp((targetY - cachedRb.position.y) * hoverReturnSpeed, -chaseVerticalSpeed, chaseVerticalSpeed);
            float returnX = spawnPosition.x - cachedRb.position.x;
            float horizontal = Mathf.Abs(returnX) > 0.05f
                ? Mathf.Clamp(returnX * idleReturnHorizontalSpeed, -idleReturnHorizontalSpeed, idleReturnHorizontalSpeed)
                : Mathf.Lerp(cachedRb.linearVelocity.x, 0f, Time.deltaTime * attackStopDamping);
            cachedRb.linearVelocity = new Vector2(horizontal, vertical);
        }

        public void TickFloatChase(float deltaX)
        {
            if (cachedRb == null)
                return;

            if (CurrentTarget == null)
            {
                TickHoverIdle();
                return;
            }

            FaceByDeltaX(deltaX);
            SetRunAnimation(true, false);

            Vector2 current = cachedRb.position;
            Vector2 target = CurrentTarget.position;
            target.y += chaseHeightOffset;

            float vx = Mathf.Clamp((target.x - current.x) * chaseHorizontalSpeed, -chaseHorizontalSpeed, chaseHorizontalSpeed);
            float vy = Mathf.Clamp((target.y - current.y) * chaseVerticalSpeed, -chaseVerticalSpeed, chaseVerticalSpeed);
            cachedRb.linearVelocity = new Vector2(vx, vy);
        }

        public void TickGasAttackMotion(float deltaX)
        {
            FaceByDeltaX(deltaX);
            SetRunAnimation(false, false);

            if (cachedRb == null)
                return;

            Vector2 currentVelocity = cachedRb.linearVelocity;
            float damp = Mathf.Clamp01(Time.deltaTime * attackStopDamping);
            cachedRb.linearVelocity = Vector2.Lerp(currentVelocity, Vector2.zero, damp);
        }

        public void OnMvAnimEvent(string eventName, MvAnimEventLite source)
        {
            if (eventName == "AtkS" || eventName == "Attack/AtkS")
            {
                SetGasVfxActive(true);
                return;
            }

            if (eventName == "AtkE" || eventName == "Attack/AtkE" || eventName == "CancelE")
            {
                SetGasVfxActive(false);
            }
        }

        private void SetGasVfxActive(bool active)
        {
            if (gasVfxObject != null && gasVfxObject.activeSelf != active)
                gasVfxObject.SetActive(active);
        }

        protected override void Die()
        {
            SetGasVfxActive(false);

            if (cachedRb != null && forceZeroGravity)
                cachedRb.gravityScale = originalGravityScale;

            base.Die();
        }
    }
}
