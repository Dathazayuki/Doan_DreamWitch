using System.Collections.Generic;
using DreamKnight.Interfaces;
using DreamKnight.Player;
using UnityEngine;

namespace Mv
{
	/// <summary>
	/// Component xử lý va chạm và gây sát thương của đòn đánh dành cho Enemy.
	/// Hỗ trợ cả kích hoạt tức thời, kích hoạt theo animation event, và cơ chế khung sát thương duy trì (Attack Window).
	/// </summary>
	[DisallowMultipleComponent]
	public class MvAttack : MonoBehaviour, IMvAnimEventLiteListener
	{
		#region Serialized Fields
		[Header("Attack Settings")]
		[SerializeField] private float damage = 12f;
		[SerializeField] private float attackCooldown = 0.8f;
		[SerializeField] private bool requireAnimEventAtkS = true;
		[SerializeField] private bool allowAutoHitFallback = true;
		[SerializeField] private float autoHitDelay = 0.2f;
		[SerializeField] private float attackWindowStartDelay = 0f;
		[SerializeField] private float attackWindowTickInterval = 0.05f;
		[SerializeField] private float attackWindowMinDuration = 0.35f;
		[SerializeField] private bool allowRepeatedHitsDuringWindow = false;
		[SerializeField] private bool closeAttackWindowImmediatelyOnEnd = false;
		[SerializeField] private Collider2D attackTrigger;
		[SerializeField] private bool forceIsTrigger = true;

		[Header("AI Desync")]
		[SerializeField] private Vector2 initialAttackDelayJitter = new Vector2(0f, 0.25f);
		[SerializeField] private Vector2 attackCooldownJitter = new Vector2(0f, 0.2f);
		#endregion

		#region Private Fields
		private readonly HashSet<IDamageable> hitBuffer = new HashSet<IDamageable>();
		private MvEnemyBase owner;
		private PlayerController cachedPlayer;

		private float lastAttackTime = -999f;
		private bool attackQueued;
		private float attackQueuedTime;

		private bool attackWindowActive;
		private bool attackWindowCloseRequested;
		private float attackWindowEndTime;
		private float nextDamageTime;
		private int pendingHitFrame = -1;
		private float nextAttackReadyTime;
		#endregion

		/// <summary>
		/// Sự kiện static được phát khi Player hồi sinh, yêu cầu tất cả MvAttack trong scene clear hitBuffer.
		/// </summary>
		public static event System.Action OnPlayerRevived;

		#region Unity Callbacks
		private void Awake()
		{
			if (attackTrigger == null)
				attackTrigger = ResolveAttackTrigger();

			if (attackTrigger != null && forceIsTrigger && !attackTrigger.isTrigger)
				attackTrigger.isTrigger = true;

			nextAttackReadyTime = Time.time + GetRandomDelay(initialAttackDelayJitter);
		}

		private void OnEnable()
		{
			OnPlayerRevived += ClearHitBuffer;
			ClearHitBuffer();
		}

		private void OnDisable()
		{
			OnPlayerRevived -= ClearHitBuffer;
			ClearHitBuffer();
		}

		private void LateUpdate()
		{
			if (!enabled) return;
			if (!attackQueued && !attackWindowActive) return;

			if (attackWindowActive && attackWindowCloseRequested && Time.time >= attackWindowEndTime)
			{
				attackWindowActive = false;
				attackWindowCloseRequested = false;
				attackQueued = false;
			}

			if (pendingHitFrame >= 0 && Time.frameCount >= pendingHitFrame)
			{
				pendingHitFrame = -1;
				Physics2D.SyncTransforms();
				DoHit();
			}

			if (attackQueued && requireAnimEventAtkS && allowAutoHitFallback && Time.time >= attackQueuedTime + autoHitDelay)
			{
				Physics2D.SyncTransforms();
				DoHit();
			}

			if (attackWindowActive && Time.time >= nextDamageTime)
			{
				Physics2D.SyncTransforms();
				if (allowRepeatedHitsDuringWindow)
					hitBuffer.Clear();
				DoHit();
				nextDamageTime = Time.time + Mathf.Max(0f, attackWindowTickInterval);
			}
		}
		#endregion

		#region Public API
		public void SetOwner(MvEnemyBase ownerEnemy) => owner = ownerEnemy;
		public bool CanStartAttack() => (owner == null || owner.IsAlive) && Time.time >= nextAttackReadyTime;
		public float CooldownRemaining => Mathf.Max(0f, nextAttackReadyTime - Time.time);
		public bool HasTriggeredAttack => lastAttackTime > -100f;
		public float LastAttackTime => lastAttackTime;
		public bool AllowAutoHitFallback { get => allowAutoHitFallback; set => allowAutoHitFallback = value; }
		public event System.Action<MvAttack> AttackWindowStarted;
		public event System.Action<MvAttack> AttackWindowEnded;

		public void SetAllowAutoHitFallback(bool value) => allowAutoHitFallback = value;
		public void SetAutoHitDelay(float delay) => autoHitDelay = Mathf.Max(0f, delay);
		public void SetAttackWindowStartDelay(float delay) => attackWindowStartDelay = Mathf.Max(0f, delay);
		public void SetAttackWindowTickInterval(float interval) => attackWindowTickInterval = Mathf.Max(0f, interval);
		public void SetAllowRepeatedHitsDuringWindow(bool value) => allowRepeatedHitsDuringWindow = value;
		public void SetCloseAttackWindowImmediatelyOnEnd(bool value) => closeAttackWindowImmediatelyOnEnd = value;
		public void SetRequireAnimEventAtkS(bool value) => requireAnimEventAtkS = value;

		/// <summary>
		/// Xóa bộ đệm đã đánh, cho phép đánh lại mục tiêu (dùng khi Player respawn).
		/// </summary>
		public void ClearHitBuffer()
		{
			hitBuffer.Clear();
			attackQueued = false;
			attackWindowActive = false;
			attackWindowCloseRequested = false;
			pendingHitFrame = -1;
		}

		/// <summary>
		/// Phát sự kiện thông báo Player đã hồi sinh cho tất cả MvAttack instances.
		/// Gọi từ PlayerController khi respawn hoàn tất.
		/// </summary>
		public static void BroadcastPlayerRevived() => OnPlayerRevived?.Invoke();

		public bool TryStartAttack()
		{
			if (!CanStartAttack()) return false;

			lastAttackTime = Time.time;
			nextAttackReadyTime = Time.time + Mathf.Max(0f, attackCooldown) + GetRandomDelay(attackCooldownJitter);
			attackQueued = true;
			attackQueuedTime = Time.time;
			hitBuffer.Clear();

			if (!requireAnimEventAtkS)
				DoHit();

			return true;
		}

		private static float GetRandomDelay(Vector2 range)
		{
			float min = Mathf.Min(range.x, range.y);
			float max = Mathf.Max(range.x, range.y);
			if (max <= 0f)
				return 0f;

			return Random.Range(Mathf.Max(0f, min), Mathf.Max(0f, max));
		}
		#endregion

		#region Animation Events
		public void OnMvAnimEvent(string eventName, MvAnimEventLite source)
		{
			if (!enabled) return;

			if (eventName == "AtkS" || eventName == "Attack/AtkS")
			{
				if (!attackQueued && !TryStartAttack())
					return;

				Physics2D.SyncTransforms();
				attackWindowActive = true;
				attackWindowCloseRequested = false;
				AttackWindowStarted?.Invoke(this);
				float startDelay = Mathf.Max(0f, attackWindowStartDelay);
				attackWindowEndTime = Time.time + startDelay + Mathf.Max(0f, attackWindowMinDuration);
				nextDamageTime = Time.time + startDelay;

				if (startDelay <= 0f)
				{
					pendingHitFrame = Time.frameCount + 1;
					DoHit();
				}
				else
				{
					pendingHitFrame = -1;
				}
			}
			else if (eventName == "AtkE" || eventName == "Attack/AtkE" || eventName == "CancelE")
			{
				if (closeAttackWindowImmediatelyOnEnd)
				{
					attackWindowActive = false;
					attackWindowCloseRequested = false;
					attackQueued = false;
					pendingHitFrame = -1;
					AttackWindowEnded?.Invoke(this);
					return;
				}

				attackWindowCloseRequested = true;
				attackWindowEndTime = Mathf.Max(attackWindowEndTime, Time.time + Mathf.Max(0f, attackWindowMinDuration));
				pendingHitFrame = -1;
			}
		}
		#endregion

		#region Collision Detection
		private PlayerController GetPlayerController()
		{
			if (cachedPlayer == null)
				cachedPlayer = FindFirstObjectByType<PlayerController>();
			return cachedPlayer;
		}

		private void DoHit()
		{
			if (!attackQueued && !attackWindowActive) return;

			PlayerController player = GetPlayerController();
			if (player == null || !player.IsAlive) return;

			Collider2D playerCol = player.ActiveCollider;
			if (playerCol == null || attackTrigger == null) return;

			if (IsColliderOverlapping(attackTrigger, playerCol))
			{
				if (hitBuffer.Add(player))
				{
					player.TakeDamage(damage, owner != null ? owner.gameObject : gameObject);
				}
			}

			if (!attackWindowActive)
				attackQueued = false;
		}

		public bool HasAttackTrigger => attackTrigger != null;

		public bool IsPlayerInsideAttackTrigger()
		{
			if (attackTrigger == null) return false;

			PlayerController player = GetPlayerController();
			if (player == null || !player.IsAlive) return false;

			Collider2D playerCol = player.ActiveCollider;
			return playerCol != null && IsColliderOverlapping(attackTrigger, playerCol);
		}

		private static bool IsColliderOverlapping(Collider2D attackCollider, Collider2D targetCollider)
		{
			if (attackCollider == null || targetCollider == null)
				return false;

			if (!attackCollider.enabled || !targetCollider.enabled)
				return false;

			ColliderDistance2D distance = Physics2D.Distance(attackCollider, targetCollider);
			if (distance.isValid)
				return distance.isOverlapped;

			return attackCollider.IsTouching(targetCollider) || attackCollider.bounds.Intersects(targetCollider.bounds);
		}

		private Collider2D ResolveAttackTrigger()
		{
			Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
			Collider2D rootNonTrigger = null;
			Collider2D childNonTrigger = null;

			for (int i = 0; i < colliders.Length; i++)
			{
				Collider2D collider = colliders[i];
				if (collider == null || !collider.enabled) continue;

				if (collider.isTrigger) return collider;
				if (collider.transform != transform) childNonTrigger ??= collider;
				else rootNonTrigger ??= collider;
			}
			return childNonTrigger != null ? childNonTrigger : rootNonTrigger;
		}
		#endregion

		#region Debug Gizmos
		private void OnDrawGizmosSelected()
		{
			if (attackTrigger == null) return;
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(attackTrigger.bounds.center, attackTrigger.bounds.size);
		}
		#endregion
	}
}
