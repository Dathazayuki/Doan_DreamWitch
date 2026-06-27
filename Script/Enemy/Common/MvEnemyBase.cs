using System;
using DreamKnight.Interfaces;
using DreamKnight.Player;
using DreamKnight.Systems.Combat;
using DreamKnight.Systems.Zone;
using UnityEngine;

namespace Mv
{
	[DisallowMultipleComponent]
	public abstract partial class MvEnemyBase : MonoBehaviour, IDamageable
	{
		public enum AsCommon : byte
		{
			Idle = 0,
			Run = 1,
			Turn = 2,
			AtkAfter = 3,
			Hit = 4,
			HitKnockBack = 5,
			HitKnockUp = 6,
			HitBounce = 7,
			HitNeedle = 8,
			CondFrozen = 9,
			CondStone = 10,
			CondStun = 11,
			DeathPotDive = 12,
			DeathPre = 13,
			Cage = 14,
			Death = 15,
			Inactive = 16,
			Max = 17
		}

		[Header("Stats")]
		[SerializeField] private float maxHealth = 80f;
		[SerializeField] private float moveSpeed = 2.5f;
		[Tooltip("Hệ số nhân tốc độ di chuyển khi đuổi theo Player (1 = giữ nguyên, > 1 = chạy nhanh hơn).")]
		[SerializeField] private float chaseSpeedMultiplier = 2f;
		[SerializeField] private float attackRange = 1.4f;
		[SerializeField] private float baseFaceFlipDeadZoneX = 0.15f;
		[SerializeField] private float attackSignDuration = 0.45f;
		[SerializeField] private float attackAnimLockDuration = 0.28f;
		[SerializeField] private bool retreatDuringAttackCooldown = true;
		[SerializeField] private float retreatSpeedMultiplier = 0.6f;
		[SerializeField] private bool useVerticalAttackCheck = false;
		[SerializeField] private float attackVerticalTolerance = 1.2f;
		[SerializeField] private float hitStunDuration = 0.2f;
		[SerializeField] private float knockbackForceX = 2.5f;

		[Header("AI Desync")]
		[SerializeField, Range(0f, 0.25f)] private float moveSpeedJitterPercent = 0.08f;
		[SerializeField] private bool randomizeInitialSearchTime = true;

		[Header("Combat Mode")]
		[Tooltip("World = patrol/detect/mất dấu. Arena = luôn theo dõi Player, không bao giờ mất target.")]
		[SerializeField] private EnemyCombatMode defaultCombatMode = EnemyCombatMode.World;

		private EnemyCombatMode currentCombatMode;

		[Header("Search (moveSearch style)")]
		[SerializeField] private float searchInterval = 0.2f;
		[SerializeField] private float searchRadius = 10f;
		[SerializeField] private float searchVerticalTolerance = 3f;
		[SerializeField, Range(0f, 360f)] private float searchFanAngle = 360f;
		[SerializeField] private LayerMask playerSearchMask = ~0;
		[SerializeField] private bool requireLineOfSight = true;
		[SerializeField] private LayerMask lineOfSightBlockMask = ~0;
		[SerializeField] private float loseTargetDelay = 1.2f;
		[SerializeField] private float scoreDistanceWeight = 1f;
		[SerializeField] private float scoreFacingWeight = 0.3f;

		[Header("Search Gizmos")]
		[SerializeField] private bool drawSearchGizmos = true;
		[SerializeField] private Color searchRadiusGizmoColor = new Color(0.1f, 0.9f, 0.7f, 0.8f);
		[SerializeField] private Color searchFanGizmoColor = new Color(1f, 0.8f, 0.2f, 0.9f);

		[Header("Wall Detection (Patrol Turn)")]
		[SerializeField] private LayerMask movementBlockMask = ~0;
		[SerializeField] private float movementProbeDistance = 0.08f;

		// Physics: Dùng Dynamic Rigidbody2D — Unity tự xử lý gravity và va chạm.
		// Flying enemy: đặt Rigidbody2D.gravityScale = 0 trong Inspector.

		[Header("Patrol")]
		[SerializeField] private bool usePatrol = true;
		[SerializeField] private float patrolRadius = 3.5f;
		[SerializeField] private float patrolReachDistance = 0.2f;
		[SerializeField] private Vector2 patrolPauseMinMax = new Vector2(0.8f, 1.6f);

		[Header("Return To Origin")]
		[SerializeField] private bool returnToOriginWhenTargetLost = true;
		[SerializeField] private float returnToOriginDistance = 6f;
		[SerializeField] private float returnToOriginStopDistance = 0.25f;
		[SerializeField] private float returnToOriginSpeedMultiplier = 1.15f;
		[SerializeField] private bool enableReturnRespawn = true;
		[SerializeField] private float returnRespawnTimeoutDuration = 12f;

		[Header("References")]
		[SerializeField] private Rigidbody2D rb;
		[SerializeField] private Animator animator;
		[SerializeField] private SpriteRenderer spriteRenderer;
		[SerializeField] private MvAttack attack;
		[SerializeField] private Transform target;

		[Header("Animator State Fallback (No Params)")]
		[SerializeField] private string idleStateName = "Idle";
		[SerializeField] private string runStateName = "Run";
		[SerializeField] private string attackSignStateName = "AtkSign";
		[SerializeField] private string attackStateName = "Atk1";
		[SerializeField] private string hitStateName = "Hit";
		[SerializeField] private string deadStateName = "Death";

		[Header("Death Finalize")]
		[SerializeField] private GameObject deathStreamEffect;
		[SerializeField] private float deathFinalizeFallbackDelay = 1.2f;

		protected virtual string IdleStateName => idleStateName;
		protected virtual string RunStateName => runStateName;
		protected virtual string AttackSignStateName => attackSignStateName;
		protected virtual string AttackStateName => attackStateName;
		protected virtual string HitStateName => hitStateName;
		protected virtual string DeadStateName => deadStateName;

		protected virtual bool UseAttackTriggerRangeCheck => true;
		protected virtual bool TryEvaluateCustomAttackRange(float absX, float absY, float edgeDistanceX, out bool inAttackRange)
		{
			inAttackRange = false;
			return false;
		}

		private EnemyAnimationController animationController;
		private EnemyContext enemyContext;
		private EnemyStateMachine enemyStateMachine;
		private byte idleStateId;
		private byte runStateId;
		private byte turnStateId;
		private byte atkAfterStateId;
		private byte attackStateId;
		private byte hitStateId;
		private byte deadStateId;
		private byte inactiveStateId;

		private float currentHealth;
		private bool isDead;
		private float hitStunTimer;

		private Collider2D selfCollider;
		private Collider2D targetCollider;
		private readonly Collider2D[] searchResults = new Collider2D[24];
		private readonly RaycastHit2D[] movementHits = new RaycastHit2D[8];
		private float nextSearchTime;
		private float lastTargetSeenTime = -999f;
		private Vector2 patrolCenter;
		private Vector2 patrolPoint;
		private bool hasPatrolPoint;
		private float patrolPauseUntil;
		private bool isAttackSigning;
		private float attackSignEndTime;
		private float attackAnimLockUntil;
		private float deadStateEnterTime;
		private bool deathFinalizeProcessed;
		private float returnToOriginTimer;
		private bool isCurrentlyReturning;
		private float runtimeMoveSpeedMultiplier = 1f;

		public bool IsAlive => !isDead && currentHealth > 0f;
		public float CurrentHealth => currentHealth;
		public float MaxHealth => maxHealth;
		public MvAttack ActiveAttack => attack;

		public event Action<float, float> OnHealthChanged;
		public event Action OnDeath;

		protected virtual void Awake()
		{
			if (rb == null) rb = GetComponent<Rigidbody2D>();
			if (animator == null) animator = GetComponentInChildren<Animator>();
			if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			selfCollider = GetComponent<Collider2D>();
			InitializeRuntimeDesync();

			if (attack == null) attack = GetComponent<MvAttack>();
            if (attack == null) attack = GetComponentInChildren<MvAttack>();
            if (attack != null) attack.SetOwner(this);
			animationController = new EnemyAnimationController(
				animator,
				IdleStateName,
				RunStateName,
				AttackSignStateName,
				AttackStateName,
				HitStateName,
				DeadStateName);

			enemyContext = new EnemyContext(this);
			enemyStateMachine = new EnemyStateMachine();
			EnemyState idleState = CreateIdleState(enemyContext);
			EnemyState runState = CreateRunState(enemyContext);
			EnemyState turnState = CreateTurnState(enemyContext);
			EnemyState atkAfterState = CreateAtkAfterState(enemyContext);
			EnemyState attackState = CreateAttackState(enemyContext);
			EnemyState hitState = CreateHitState(enemyContext);
			EnemyState deadState = CreateDeadState(enemyContext);
			EnemyState inactiveState = CreateInactiveState(enemyContext);

			idleStateId = idleState.StateId;
			runStateId = runState.StateId;
			turnStateId = turnState.StateId;
			atkAfterStateId = atkAfterState.StateId;
			attackStateId = attackState.StateId;
			hitStateId = hitState.StateId;
			deadStateId = deadState.StateId;
			inactiveStateId = inactiveState.StateId;

			enemyStateMachine.Register(idleState);
			enemyStateMachine.Register(runState);
			enemyStateMachine.Register(turnState);
			enemyStateMachine.Register(atkAfterState);
			enemyStateMachine.Register(attackState);
			enemyStateMachine.Register(hitState);
			enemyStateMachine.Register(deadState);
			enemyStateMachine.Register(inactiveState);
			RegisterAdditionalStates(enemyStateMachine, enemyContext);
			enemyStateMachine.ChangeState(idleStateId);
			patrolCenter = transform.position;
			nextSearchTime = Time.time + (randomizeInitialSearchTime ? UnityEngine.Random.Range(0f, Mathf.Max(0f, searchInterval)) : 0f);
			returnToOriginTimer = 0f;
			isCurrentlyReturning = false;

			currentHealth = maxHealth;
			OnHealthChanged?.Invoke(currentHealth, maxHealth);

			// Khởi tạo combat mode từ Inspector
			currentCombatMode = defaultCombatMode;
		}

		// Subclass override để thêm logic riêng (dash, v.v.)
		protected virtual void FixedUpdate() { }

		private void InitializeRuntimeDesync()
		{
			float jitter = Mathf.Clamp(moveSpeedJitterPercent, 0f, 0.25f);
			runtimeMoveSpeedMultiplier = jitter > 0f ? UnityEngine.Random.Range(1f - jitter, 1f + jitter) : 1f;
		}

		protected virtual void Update()
		{
			if (enemyStateMachine == null) return;

			if (isDead)
			{
				enemyStateMachine.ChangeState(deadStateId);
				enemyStateMachine.Tick();
				return;
			}

			UpdateSearchTarget();
			RefreshContextData();

			if (hitStunTimer > 0f)
				enemyStateMachine.ChangeState(hitStateId);

			enemyStateMachine.Tick();
		}

		public bool IsCurrentlyAttacking => CurrentState != null && CurrentState.IsAttackState;

		public virtual void TakeDamage(float damage, GameObject damageSource = null)
		{
			TakeDamage(damage, damageSource, null);
		}

		public virtual void TakeDamage(float damage, GameObject damageSource = null, Vector3? damageTextWorldPosition = null)
		{
			if (!IsAlive) return;

			currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, damage));
			OnHealthChanged?.Invoke(currentHealth, maxHealth);
			DamageTextService.ShowEnemyDamage(damage, ResolveDamageTextPosition(damageTextWorldPosition));
			
			if (currentHealth <= 0f)
			{
				isAttackSigning = false;
				ApplyKnockback(damageSource);
				Die();
				return;
			}

			if (IsCurrentlyAttacking)
			{
				return;
			}

			isAttackSigning = false;
			ApplyKnockback(damageSource);
			TriggerHit();
		}

		public virtual bool CanReceiveDamage(float damage, GameObject damageSource = null, Collider2D hitCollider = null)
		{
			return true;
		}

		public virtual void OnDamageBlocked(float damage, GameObject damageSource = null, Collider2D hitCollider = null)
		{
		}

		public virtual void Heal(float amount)
		{
			if (isDead) return;
			currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0f, amount));
			OnHealthChanged?.Invoke(currentHealth, maxHealth);
		}

		protected virtual bool DiesOnPlayerTrapZone => true;
		protected virtual bool DisableCollidersOnDie => true;

		protected virtual void OnTriggerEnter2D(Collider2D other)
		{
			TryDieFromPlayerTrapZone(other);
		}

		protected virtual void OnTriggerStay2D(Collider2D other)
		{
			TryDieFromPlayerTrapZone(other);
		}

		private void TryDieFromPlayerTrapZone(Collider2D other)
		{
			if (!DiesOnPlayerTrapZone || !IsAlive || other == null)
				return;

			PlayerTrapZone trapZone = other.GetComponentInParent<PlayerTrapZone>();
			if (trapZone == null)
				return;

			currentHealth = 0f;
			OnHealthChanged?.Invoke(currentHealth, maxHealth);
			Die();
		}

		private Vector3 ResolveDamageTextPosition(Vector3? damageTextWorldPosition = null)
		{
			if (damageTextWorldPosition.HasValue)
				return damageTextWorldPosition.Value;

			if (selfCollider != null)
				return selfCollider.bounds.center;

			if (spriteRenderer != null)
				return spriteRenderer.bounds.center;

			return transform.position;
		}

		protected virtual void Die()
		{
			isDead = true;
			currentHealth = 0f;
			deathFinalizeProcessed = false;
			SetHorizontalVelocity(0f);
			animationController?.SetRun(false, false);
			animationController?.SetDead(true);

			if (deathStreamEffect != null)
				deathStreamEffect.SetActive(true);

			if (DisableCollidersOnDie)
			{
				Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
				for (int i = 0; i < colliders.Length; i++)
				{
					if (colliders[i] != null)
						colliders[i].enabled = false;
				}
			}

			OnDeath?.Invoke();

			if (attack != null)
				attack.enabled = false;
		}

		private void ApplyKnockback(GameObject damageSource)
		{
			if (rb == null) return;

			float direction = 1f;
			if (damageSource != null)
			{
				float delta = transform.position.x - damageSource.transform.position.x;
				if (Mathf.Abs(delta) > 0.01f)
					direction = Mathf.Sign(delta);
			}

			float horizontal = direction * knockbackForceX;
			// Dynamic RB: physics tự xử lý khi văng vào tường, không cần pre-check

			float vertical = rb.linearVelocity.y;
			rb.linearVelocity = new Vector2(horizontal, vertical);
			hitStunTimer = Mathf.Max(0f, hitStunDuration);
		}

		private void UpdateSearchTarget()
		{
			// Luôn kiểm tra target hiện tại còn alive không (áp dụng cả World lẫn Arena)
			if (target != null)
			{
				PlayerController currentPlayer = target.GetComponent<PlayerController>();
				if (currentPlayer == null)
					currentPlayer = target.GetComponentInParent<PlayerController>();

				if (currentPlayer == null || !currentPlayer.IsAlive)
				{
					ClearTarget();
				}
			}

			if (Time.time < nextSearchTime)
				return;

			nextSearchTime = Time.time + Mathf.Max(0.02f, searchInterval);

			PlayerController bestTarget = FindBestTarget();
			if (bestTarget != null)
			{
				target = bestTarget.transform;
				targetCollider = bestTarget.ActiveCollider;

				lastTargetSeenTime = Time.time;
				returnToOriginTimer = 0f;
				isCurrentlyReturning = false;
				return;
			}

			// Arena mode: không bao giờ mất dấu do timeout — chỉ mất target khi player chết (xử lý trên)
			if (currentCombatMode == EnemyCombatMode.Arena)
			{
				if (target == null)
				{
					PlayerController player = null;
					if (PersistentPlayerRoot.Instance != null)
						player = PersistentPlayerRoot.Instance.GetComponent<PlayerController>();
					if (player == null)
						player = FindAnyObjectByType<PlayerController>();

					if (player != null && player.IsAlive)
					{
						target = player.transform;
						targetCollider = player.ActiveCollider;
						lastTargetSeenTime = Time.time;
						returnToOriginTimer = 0f;
						isCurrentlyReturning = false;
					}
				}
				return;
			}

			// World mode: mất dấu sau loseTargetDelay
			if (target != null && Time.time >= lastTargetSeenTime + Mathf.Max(0.1f, loseTargetDelay))
			{
				ClearTarget();
			}
		}

		private PlayerController FindBestTarget()
		{
			ContactFilter2D filter = new ContactFilter2D();
			filter.useLayerMask = true;
			filter.layerMask = playerSearchMask;
			filter.useTriggers = true;
			int hitCount = Physics2D.OverlapCircle(transform.position, searchRadius, filter, searchResults);
			PlayerController best = null;
			float bestScore = float.NegativeInfinity;

			for (int i = 0; i < hitCount; i++)
			{
				Collider2D hit = searchResults[i];
				if (hit == null) continue;

				PlayerController player = hit.GetComponent<PlayerController>();
				if (player == null)
					player = hit.GetComponentInParent<PlayerController>();
				if (player == null || !player.IsAlive) continue;

				Vector2 delta = player.transform.position - transform.position;
				if (Mathf.Abs(delta.y) > searchVerticalTolerance) continue;
				if (!IsDeltaInSearchFan(delta)) continue;

				float distance = delta.magnitude;
				if (distance > searchRadius) continue;

				Collider2D playerCol = hit;
				if (playerCol == null)
					playerCol = player.GetComponent<Collider2D>();
				if (playerCol == null)
					playerCol = player.GetComponentInChildren<Collider2D>();

				if (requireLineOfSight && !HasLineOfSight(playerCol))
					continue;

				float score = ScoreTarget(delta, distance);
				if (score > bestScore)
				{
					bestScore = score;
					best = player;
				}
			}

			for (int i = 0; i < hitCount; i++)
				searchResults[i] = null;

			return best;
		}

		private bool IsDeltaInSearchFan(Vector2 delta)
		{
			if (delta.sqrMagnitude <= 0.0001f)
				return true;

			float fanAngle = Mathf.Clamp(searchFanAngle, 0f, 360f);
			if (fanAngle >= 359.9f)
				return true;

			float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
			Vector2 forward = new Vector2(facingSign, 0f);
			float dot = Vector2.Dot(forward, delta.normalized);
			float halfAngle = fanAngle * 0.5f;
			float minDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
			return dot >= minDot;
		}

		private bool HasLineOfSight(Collider2D playerCol)
		{
			if (playerCol == null) return false;

			Vector2 eye = selfCollider != null ? selfCollider.bounds.center : transform.position;
			Vector2 targetPos = playerCol.bounds.center;
			Vector2 dir = targetPos - eye;
			float distance = dir.magnitude;
			if (distance <= 0.001f) return true;

			RaycastHit2D[] hits = Physics2D.RaycastAll(eye, dir / distance, distance, lineOfSightBlockMask);
			if (hits == null || hits.Length == 0) return true;

			for (int i = 0; i < hits.Length; i++)
			{
				Collider2D hitCollider = hits[i].collider;
				if (hitCollider == null) continue;
				if (hitCollider.isTrigger) continue;

				Transform hitRoot = hitCollider.transform.root;
				if (hitRoot == transform.root) continue;

				if (hitRoot == playerCol.transform.root)
					return true;

				return false;
			}

			return true;
		}

		private float ScoreTarget(Vector2 delta, float distance)
		{
			float normalizedDistance = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, searchRadius));
			float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
			Vector2 facingDir = new Vector2(facingSign, 0f);
			float facing = Vector2.Dot(facingDir, delta.normalized);

			return normalizedDistance * scoreDistanceWeight + facing * scoreFacingWeight;
		}

		private void OnDrawGizmosSelected()
		{
			if (!drawSearchGizmos)
				return;

			Vector3 center = transform.position;
			float radius = Mathf.Max(0.01f, searchRadius);

			Gizmos.color = searchRadiusGizmoColor;
			Gizmos.DrawWireSphere(center, radius);

			float fanAngle = Mathf.Clamp(searchFanAngle, 0f, 360f);
			if (fanAngle >= 359.9f)
				return;

			float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
			Vector2 forward = new Vector2(facingSign, 0f);
			float halfAngle = fanAngle * 0.5f;
			Vector2 upper = Quaternion.Euler(0f, 0f, halfAngle) * forward;
			Vector2 lower = Quaternion.Euler(0f, 0f, -halfAngle) * forward;

			Gizmos.color = searchFanGizmoColor;
			Gizmos.DrawLine(center, center + (Vector3)(forward * radius));
			Gizmos.DrawLine(center, center + (Vector3)(upper * radius));
			Gizmos.DrawLine(center, center + (Vector3)(lower * radius));

			const int arcSegments = 24;
			float startAngle = -halfAngle;
			float step = fanAngle / arcSegments;
			Vector3 prevPoint = center + (Vector3)(Quaternion.Euler(0f, 0f, startAngle) * forward * radius);
			for (int i = 1; i <= arcSegments; i++)
			{
				float currentAngle = startAngle + step * i;
				Vector3 nextPoint = center + (Vector3)(Quaternion.Euler(0f, 0f, currentAngle) * forward * radius);
				Gizmos.DrawLine(prevPoint, nextPoint);
				prevPoint = nextPoint;
			}
		}

		private void ClearTarget()
		{
			target = null;
			targetCollider = null;
			isAttackSigning = false;
			returnToOriginTimer = 0f;
			isCurrentlyReturning = false;
		}

		// Dừng chuyển động patrol (tái sử dụng ở nhiều chỗ)
		private void StopPatrolMotion()
		{
			animationController?.SetRun(false, true);
			SetHorizontalVelocity(0f);
		}

		private float GetRandomPatrolPause()
		{
			float min = Mathf.Min(patrolPauseMinMax.x, patrolPauseMinMax.y);
			float max = Mathf.Max(patrolPauseMinMax.x, patrolPauseMinMax.y);
			return UnityEngine.Random.Range(min, max);
		}

		private void MovePatrol()
		{
			if (!usePatrol || Time.time < patrolPauseUntil)
			{
				StopPatrolMotion();
				return;
			}

			if (!hasPatrolPoint)
			{
				float offsetX = UnityEngine.Random.Range(-patrolRadius, patrolRadius);
				patrolPoint = patrolCenter + new Vector2(offsetX, 0f);
				hasPatrolPoint = true;
			}

			float dx = patrolPoint.x - transform.position.x;
			if (Mathf.Abs(dx) <= patrolReachDistance)
			{
				hasPatrolPoint = false;
				patrolPauseUntil = Time.time + GetRandomPatrolPause();
				StopPatrolMotion();
				return;
			}

			float direction = Mathf.Sign(dx);

			// Phát hiện tường phía trước → quay đầu (Dynamic RB tự dừng khi va tường)
			if (IsWallAhead(direction))
			{
				patrolPauseUntil = Time.time + GetRandomPatrolPause();
				StopPatrolMotion();
				float safeOffsetX = UnityEngine.Random.Range(0f, patrolRadius) * (-direction);
				patrolPoint = patrolCenter + new Vector2(safeOffsetX, 0f);
				hasPatrolPoint = true;
				return;
			}

			SetHorizontalVelocity(direction * moveSpeed * runtimeMoveSpeedMultiplier);
			Face(direction);
			animationController?.SetRun(true, false);
		}

		private void MoveRetreatFromTarget(float deltaX)
		{
			float targetDir = ResolveFacingDirection(deltaX);
			float retreatDir = -targetDir;
			float speed = moveSpeed * runtimeMoveSpeedMultiplier * Mathf.Max(0f, retreatSpeedMultiplier);
			SetHorizontalVelocity(retreatDir * speed);
			Face(targetDir);
			animationController?.SetRun(speed > 0.01f, false);
		}

		private bool ShouldReturnToOrigin()
		{
			if (!returnToOriginWhenTargetLost || HasTarget)
				return false;

			float threshold = Mathf.Max(0.05f, returnToOriginDistance);
			float distanceToOriginX = Mathf.Abs(patrolCenter.x - transform.position.x);
			return distanceToOriginX > threshold;
		}

		private void MoveReturnToOrigin()
		{
			if (!isCurrentlyReturning)
			{
				isCurrentlyReturning = true;
				returnToOriginTimer = 0f;
			}

			float deltaX = patrolCenter.x - transform.position.x;
			float stopDistance = Mathf.Max(0.01f, returnToOriginStopDistance);
			if (Mathf.Abs(deltaX) <= stopDistance)
			{
				hasPatrolPoint = false;
				animationController?.SetRun(false, true);
				SetHorizontalVelocity(0f);
				isCurrentlyReturning = false;
				returnToOriginTimer = 0f;
				return;
			}

			returnToOriginTimer += Time.deltaTime;
			if (enableReturnRespawn && returnToOriginTimer >= Mathf.Max(0.5f, returnRespawnTimeoutDuration))
			{
				RespawnAtOrigin();
				return;
			}

			float direction = Mathf.Sign(deltaX);
			float speed = moveSpeed * runtimeMoveSpeedMultiplier * Mathf.Max(0f, returnToOriginSpeedMultiplier);
			SetHorizontalVelocity(direction * speed);
			Face(direction);
			animationController?.SetRun(speed > 0.01f, false);
		}

		private void RespawnAtOrigin()
		{
			transform.position = patrolCenter;
			SetHorizontalVelocity(0f);
			hasPatrolPoint = false;
			isCurrentlyReturning = false;
			returnToOriginTimer = 0f;
			animationController?.SetRun(false, true);
		}

		/// <summary>Phát hiện tường phía trước — dùng để quay đầu patrol, không chặn movement.</summary>
		private bool IsWallAhead(float direction)
		{
			if (rb == null || Mathf.Abs(direction) < 0.001f) return false;

			ContactFilter2D filter = new ContactFilter2D();
			filter.useLayerMask = true;
			filter.layerMask = movementBlockMask;
			filter.useTriggers = false;

			float probe = Mathf.Max(0.02f, movementProbeDistance);
			int count = rb.Cast(new Vector2(direction, 0f), filter, movementHits, probe);
			for (int i = 0; i < count; i++)
			{
				RaycastHit2D hit = movementHits[i];
				if (hit.collider == null) continue;
				if (hit.collider.transform.root == transform.root) continue;
				if (hit.distance <= 0.001f) continue;
				return true;
			}
			return false;
		}

		private void SetHorizontalVelocity(float x)
		{
			if (rb == null) return;
			rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
		}

		private void Face(float direction)
		{
			if (!CanFlip) return;
			if (Mathf.Abs(direction) < 0.001f) return;

			Vector3 scale = transform.localScale;
			float absX = Mathf.Abs(scale.x);
			scale.x = direction >= 0f ? absX : -absX;
			transform.localScale = scale;
		}

		private void TriggerAttack()
		{
			attackAnimLockUntil = Time.time + Mathf.Max(0f, attackAnimLockDuration);
			animationController?.TriggerAttack();
		}

		private void BeginAttackSign()
		{
			isAttackSigning = true;
			attackSignEndTime = Time.time + Mathf.Max(0f, attackSignDuration);
			animationController?.TriggerAttackSign();

			if (attackSignDuration <= 0f)
				attackSignEndTime = Time.time;

			if (enemyContext != null)
			{
				FaceByDeltaX(enemyContext.DeltaX);
			}
		}

		private void TriggerHit()
		{
			animationController?.TriggerHit();
		}

		protected void ForceHitReaction()
		{
			isAttackSigning = false;
			hitStunTimer = Mathf.Max(hitStunTimer, hitStunDuration);
			TriggerHit();
			ChangeEnemyState(hitStateId);
		}

		protected virtual EnemyState CreateIdleState(EnemyContext context) => new AsEm_Idle(context);
		protected virtual EnemyState CreateRunState(EnemyContext context) => new AsEm_Run(context);
		protected virtual EnemyState CreateTurnState(EnemyContext context) => new AsEm_Common_Turn(context);
		protected virtual EnemyState CreateAtkAfterState(EnemyContext context) => new AsEm_Common_AtkAfter(context);
		protected virtual EnemyState CreateAttackState(EnemyContext context) => new AsEm_DefaultAttack(context);
		protected virtual EnemyState CreateHitState(EnemyContext context) => new AsEm_Common_Hit(context);
		protected virtual EnemyState CreateDeadState(EnemyContext context) => new AsEm_Common_Death(context);
		protected virtual EnemyState CreateInactiveState(EnemyContext context) => new AsEm_Common_Inactive(context);
		protected virtual void RegisterAdditionalStates(EnemyStateMachine stateMachine, EnemyContext context) { }

		private void RefreshContextData()
		{
			if (enemyContext == null)
				return;

			if (target == null)
			{
				enemyContext.DeltaX = 0f;
				enemyContext.AbsX = 0f;
				enemyContext.AbsY = 0f;
				enemyContext.EdgeDistanceX = float.MaxValue;
				enemyContext.InAttackRange = false;
				return;
			}

			Vector2 delta = target.position - transform.position;
			float absX = Mathf.Abs(delta.x);
			float absY = Mathf.Abs(delta.y);
			float edgeDistanceX = absX;

			PlayerController targetPlayer = target.GetComponent<PlayerController>();
			if (targetPlayer == null)
				targetPlayer = target.GetComponentInParent<PlayerController>();

			if (targetPlayer != null)
			{
				targetCollider = targetPlayer.ActiveCollider;
			}

			if (selfCollider != null && targetCollider != null)
			{
				float selfHalf = selfCollider.bounds.extents.x;
				float targetHalf = targetCollider.bounds.extents.x;
				edgeDistanceX = Mathf.Max(0f, absX - (selfHalf + targetHalf));
			}

			bool inAttackRange;
			if (!TryEvaluateCustomAttackRange(absX, absY, edgeDistanceX, out inAttackRange))
			{
				if (UseAttackTriggerRangeCheck && attack != null && attack.HasAttackTrigger)
				{
					inAttackRange = attack.IsPlayerInsideAttackTrigger();
					if (!inAttackRange)
					{
						inAttackRange = edgeDistanceX <= attackRange && (!useVerticalAttackCheck || absY <= attackVerticalTolerance);
					}
				}
				else
				{
					inAttackRange = edgeDistanceX <= attackRange && (!useVerticalAttackCheck || absY <= attackVerticalTolerance);
				}
			}

			enemyContext.DeltaX = delta.x;
			enemyContext.AbsX = absX;
			enemyContext.AbsY = absY;
			enemyContext.EdgeDistanceX = edgeDistanceX;
			enemyContext.InAttackRange = inAttackRange;
		}

		internal bool UsePatrol => usePatrol;
		internal bool HasTarget => target != null;

		/// <summary>True khi đang ở Arena CombatMode (luôn theo dõi Player, không patrol, không mất dấu).</summary>
		internal bool IsArenaMode => currentCombatMode == EnemyCombatMode.Arena;

		/// <summary>
		/// Chuyển đổi Combat Mode của enemy tại runtime.
		/// Gọi từ <see cref="ArenaEncounterZone"/> hoặc subclass khi cần lock encounter.
		/// </summary>
		public void SetCombatMode(EnemyCombatMode mode)
		{
			currentCombatMode = mode;

			// Khi chuyển sang Arena: reset lose-target timer để không ngay lập tức mất dấu
			if (mode == EnemyCombatMode.Arena)
				lastTargetSeenTime = Time.time;
		}

		/// <summary>Combat mode hiện tại của enemy.</summary>
		public EnemyCombatMode CurrentCombatMode => currentCombatMode;
		protected Transform CurrentTarget => target;
		internal bool IsTargetInAttackRange => enemyContext != null && enemyContext.InAttackRange;
		internal bool IsAttackSigningActive => isAttackSigning;
		internal bool IsAttackAnimLocked => Time.time < attackAnimLockUntil;
		internal bool IsAttackSignElapsed => Time.time >= attackSignEndTime;
		internal bool IsHitStunActive => hitStunTimer > 0f;

		/// <summary>
		/// True khi animation tấn công hiện tại đã phát ĐỦ (normalizedTime >= exitThreshold).
		/// Ưu tiên đọc từ Animator thực tế thay vì timer cứng attackAnimLockDuration.
		/// Dùng để Attack State chờ animation phát hết trước khi chuyển sang state tiếp theo.
		/// Fallback về IsAttackAnimLocked (timer) nếu Animator null hoặc không nhận ra attack state.
		/// </summary>
		/// <param name="exitThreshold">Tỷ lệ animation đã phát xong, mặc định 0.95 (95%)</param>
		internal bool IsAttackAnimFinished(float exitThreshold = 0.95f)
		{
			if (animator == null)
				return !IsAttackAnimLocked; // fallback: dùng timer

			AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

			// Kiểm tra xem đang ở attack state không (tên chứa Attack/Atk)
			bool isInAttackAnim = info.IsName(AttackStateName)
			                   || info.IsName(AttackSignStateName)
			                   || info.IsTag("Attack");

			if (!isInAttackAnim)
			{
				// Animator đã tự chuyển sang state khác → animation xong
				return true;
			}

			float threshold = Mathf.Clamp(exitThreshold, 0f, 1f);
			// normalizedTime >= threshold: animation đã phát đủ %
			// Với animation loop: normalizedTime tăng vô hạn, dùng % phần dư
			float normalized = info.loop
				? (info.normalizedTime % 1f)
				: info.normalizedTime;

			return normalized >= threshold;
		}

		internal bool CanStartAttackNow => attack != null && attack.CanStartAttack();
		internal bool CanRetreatDuringCooldown => attack != null && retreatDuringAttackCooldown && attack.HasTriggeredAttack && attack.CooldownRemaining > 0f;

		internal void ChangeEnemyState(byte stateId)
		{
			enemyStateMachine?.ChangeState(stateId);
		}

		internal byte IdleStateId => idleStateId;
		internal byte RunStateId => runStateId;
		internal byte TurnStateId => turnStateId;
		internal byte AtkAfterStateId => atkAfterStateId;
		internal byte AttackStateId => attackStateId;
		internal byte HitStateId => hitStateId;
		internal byte DeadStateId => deadStateId;
		internal byte InactiveStateId => inactiveStateId;
		internal EnemyState CurrentState => enemyStateMachine != null ? enemyStateMachine.CurrentState : null;

		internal void PlayIdleMotion()
		{
			animationController?.SetRun(false, true);
			SetHorizontalVelocity(0f);
		}

		internal void SetRunAnimation(bool running, bool allowIdleFallback = false)
		{
			animationController?.SetRun(running, allowIdleFallback);
		}

		internal void MovePatrolMotion() => MovePatrol();

		internal void MoveChaseMotion(float deltaX)
		{
			float direction = ResolveFacingDirection(deltaX);

			SetHorizontalVelocity(direction * moveSpeed * runtimeMoveSpeedMultiplier * chaseSpeedMultiplier);
			Face(direction);
			animationController?.SetRun(true, false);
		}

		internal void MoveRetreatMotion(float deltaX) => MoveRetreatFromTarget(deltaX);

		internal void FaceByDeltaX(float deltaX)
		{
			Face(ResolveFacingDirection(deltaX));
		}

		/// <summary>
		/// Cho phép subclass tắt FaceByDeltaX khi nhận damage.
		/// Em0050 (dùng rotation Z) override = false để không bị flip sprite khi bị đánh.
		/// </summary>
		protected virtual bool FaceOnHitEnabled => true;
		internal bool IsFaceOnHitEnabled => FaceOnHitEnabled;

		/// <summary>
		/// Xác định xem Enemy có được phép lật hình (flip scale X) hay không.
		/// </summary>
		protected virtual bool CanFlip => true;

		private float ResolveFacingDirection(float deltaX)
		{
			float deadZone = Mathf.Max(0.001f, baseFaceFlipDeadZoneX);
			if (Mathf.Abs(deltaX) <= deadZone)
				return transform.localScale.x >= 0f ? 1f : -1f;

			return Mathf.Sign(deltaX);
		}

		internal void StopHorizontalMotion() => SetHorizontalVelocity(0f);

		internal void BeginAttackSignIfNeeded()
		{
			if (!isAttackSigning)
				BeginAttackSign();
		}

		internal void CancelAttackSign() => isAttackSigning = false;

		internal bool TryStartAttackAndTrigger()
		{
			if (attack == null) return false;
			if (!attack.TryStartAttack()) return false;
			TriggerAttack();
			return true;
		}

		internal void TriggerAttackAnimationOnly()
		{
			TriggerAttack();
		}

		/// <summary>
		/// Cho phép subclass hoán đổi component <see cref="MvAttack"/> đang hoạt động.
		/// Gọi trong Enter() của mỗi Attack State để activate đúng hitbox trigger.
		/// </summary>
		/// <param name="newAttack">MvAttack cần kích hoạt. Null = giữ nguyên attack hiện tại.</param>
		protected void SetActiveAttack(MvAttack newAttack)
		{
			if (newAttack == null) return;
			attack = newAttack;
			if (attack != null) attack.SetOwner(this);
		}

		internal void PlayAttackSignMotion(float deltaX)
		{
			animationController?.SetRun(false, false);
			SetHorizontalVelocity(0f);
		}

		internal void PlayAttackMotion(float deltaX)
		{
			animationController?.SetRun(false, false);
			SetHorizontalVelocity(0f);
		}

		internal void TickHitStunMotion()
		{
			hitStunTimer -= Time.deltaTime;
			if (hitStunTimer < 0f)
				hitStunTimer = 0f;

			isAttackSigning = false;
			animationController?.SetRun(false, false);
		}

		internal void EnterDeadState()
		{
			deadStateEnterTime = Time.time;
		}

		internal void TickDeadState()
		{
			if (deathFinalizeProcessed)
				return;

			if (deathStreamEffect == null)
			{
				deathFinalizeProcessed = true;
				return;
			}

			if (!HasDeathStateFinished())
				return;

			deathFinalizeProcessed = true;
			gameObject.SetActive(false);
		}

		private bool HasDeathStateFinished()
		{
			float fallbackDelay = Mathf.Max(0.05f, deathFinalizeFallbackDelay);
			if (animator == null)
				return Time.time >= deadStateEnterTime + fallbackDelay;

			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			int deadHash = Animator.StringToHash(DeadStateName);
			bool isDeadState = stateInfo.shortNameHash == deadHash || stateInfo.IsName(DeadStateName);

			if (isDeadState && stateInfo.normalizedTime >= 1f)
				return true;

			return Time.time >= deadStateEnterTime + fallbackDelay;
		}

		internal bool ShouldUseRunState()
		{
			if (HasTarget)
				return !IsTargetInAttackRange || CanRetreatDuringCooldown;

			// Arena mode không có target: đứng im chờ — không patrol, không return to origin
			if (IsArenaMode)
				return false;

			if (ShouldReturnToOrigin())
				return true;

			return UsePatrol;
		}

		internal void TickRunMotion(float deltaX)
		{
			if (HasTarget)
			{
				if (IsTargetInAttackRange)
					MoveRetreatMotion(deltaX);
				else
					MoveChaseMotion(deltaX);
				return;
			}

			// Arena mode không có target: không chạy patrol
			if (IsArenaMode)
			{
				PlayIdleMotion();
				return;
			}

			if (ShouldReturnToOrigin())
			{
				MoveReturnToOrigin();
				return;
			}

			MovePatrolMotion();
		}

		protected Vector2 PatrolCenter => patrolCenter;
		protected bool ReturnToOriginWhenTargetLost => returnToOriginWhenTargetLost;
		protected float ReturnToOriginDistance => returnToOriginDistance;
		protected bool EnableReturnRespawn => enableReturnRespawn;
		protected float ReturnRespawnTimeoutDuration => returnRespawnTimeoutDuration;
		protected float ReturnToOriginTimer 
		{
			get => returnToOriginTimer;
			set => returnToOriginTimer = value;
		}
	}
}
