using DreamKnight.Enemy;
using DreamKnight.Player;
using UnityEngine;

namespace Mv
{
	[DisallowMultipleComponent]
	public class MvEm0060 : MvEnemyBase
	{
		public enum As : byte
		{
			Attack = (byte)AsCommon.Max,
			Guard,
			Max
		}

		[Header("Guard")]
		[SerializeField] private Collider2D guardShieldCollider;
		[SerializeField] private float guardDuration = 0.6f;
		[SerializeField] private bool guardWhenAttackOnCooldown = true;
		[SerializeField] private string guardStateName = "Guard";
		[SerializeField] private float guardBlockRecoilX = 1.2f;
		[SerializeField] private float guardBlockRecoilDuration = 0.08f;

		[Header("Transform Unlock")]
		[SerializeField] private bool spawnTransformUnlockOnDeath = true;
		[SerializeField] private PlayerFormDataSO unlockFormData;
		[SerializeField] private EnemyTransformUnlockPickup transformUnlockZone;

		private bool isGuarding;
		private Animator guardAnimator;
		private Rigidbody2D guardRb;
		private float guardBlockRecoilTimer;
		private Vector2 guardBlockRecoilVelocity;

		protected override string IdleStateName => "Idle";
		protected override string RunStateName => "Run";
		protected override string AttackSignStateName => "AtkSign";
		protected override string AttackStateName => "Atk1";
		protected override string HitStateName => "Hit";
		protected override string DeadStateName => "Death";

		protected override EnemyState CreateAttackState(EnemyContext context)
		{
			return new Em0060AttackState(context);
		}

		protected override EnemyState CreateIdleState(EnemyContext context)
		{
			return new Em0060IdleState(context);
		}

		protected override EnemyState CreateRunState(EnemyContext context)
		{
			return new Em0060RunState(context);
		}

		protected override void RegisterAdditionalStates(EnemyStateMachine stateMachine, EnemyContext context)
		{
			stateMachine?.Register(new Em0060GuardState(context));
		}

		protected override void Awake()
		{
			base.Awake();
			guardAnimator = GetComponentInChildren<Animator>();
			guardRb = GetComponent<Rigidbody2D>();
			SetGuarding(false);
		}

		public bool CanEnterGuard()
		{
			if (!guardWhenAttackOnCooldown) return false;
			if (!IsAlive) return false;
			if (CurrentState is Em0060GuardState) return false;
			if (!HasTarget) return false;
			if (!IsTargetInAttackRange) return false;
			if (CanStartAttackNow) return false;
			return true;
		}

		public float GuardDuration => Mathf.Max(0.05f, guardDuration);

		public bool IsGuardBlockRecoilActive => guardBlockRecoilTimer > 0f;

		public void SetGuarding(bool guarding)
		{
			isGuarding = guarding;

			if (guardShieldCollider != null)
				guardShieldCollider.enabled = guarding;
		}

		public void PlayGuardAnimation()
		{
			if (guardAnimator == null) return;
			if (string.IsNullOrWhiteSpace(guardStateName)) return;
			guardAnimator.Play(guardStateName, 0, 0f);
		}

		public override bool CanReceiveDamage(float damage, GameObject damageSource = null, Collider2D hitCollider = null)
		{
			if (isGuarding && guardShieldCollider != null && hitCollider == guardShieldCollider)
				return false;

			return base.CanReceiveDamage(damage, damageSource, hitCollider);
		}

		public override void OnDamageBlocked(float damage, GameObject damageSource = null, Collider2D hitCollider = null)
		{
			if (hitCollider == guardShieldCollider)
			{
				FaceByDeltaX(damageSource != null ? damageSource.transform.position.x - transform.position.x : 0f);

				if (guardRb != null)
				{
					float direction = 1f;
					if (damageSource != null)
					{
						float delta = transform.position.x - damageSource.transform.position.x;
						if (Mathf.Abs(delta) > 0.01f)
							direction = Mathf.Sign(delta);
					}

					guardBlockRecoilVelocity = new Vector2(
						direction * Mathf.Max(0f, guardBlockRecoilX),
						0f);
					guardBlockRecoilTimer = Mathf.Max(0f, guardBlockRecoilDuration);
					guardRb.linearVelocity = guardBlockRecoilVelocity;
				}
			}
		}

		public void TickGuardBlockRecoil()
		{
			if (guardRb == null) return;
			if (guardBlockRecoilTimer <= 0f) return;

			guardBlockRecoilTimer -= Time.deltaTime;
			guardRb.linearVelocity = new Vector2(guardBlockRecoilVelocity.x, guardRb.linearVelocity.y);
		}

		protected override void Die()
		{
			base.Die();

			if (spawnTransformUnlockOnDeath)
				ActivateTransformUnlockZone();

			if (guardRb == null)
				return;

			guardRb.linearVelocity = Vector2.zero;
			guardRb.angularVelocity = 0f;
			guardRb.bodyType = RigidbodyType2D.Static;
		}

		private void ActivateTransformUnlockZone()
		{
			PlayerFormDataSO resolvedForm = ResolveUnlockFormData();
			if (resolvedForm == null)
				return;

			EnemyTransformUnlockPickup zone = ResolveTransformUnlockZone();
			if (zone == null)
			{
				Debug.LogWarning("[MvEm0060] Transform unlock zone not found.", this);
				return;
			}

			zone.Initialize(resolvedForm, gameObject);
			zone.gameObject.SetActive(true);
			EnableTransformUnlockZoneColliders(zone);
		}

		private void EnableTransformUnlockZoneColliders(EnemyTransformUnlockPickup zone)
		{
			if (zone == null)
				return;

			Collider2D[] colliders = zone.GetComponentsInChildren<Collider2D>(true);
			for (int i = 0; i < colliders.Length; i++)
			{
				Collider2D col = colliders[i];
				if (col == null)
					continue;

				col.enabled = true;
				col.isTrigger = true;
			}

			int layer = LayerMask.NameToLayer("EnemyCanTranform");
			if (layer >= 0)
				SetLayerRecursively(zone.transform, layer);
		}

		private void SetLayerRecursively(Transform root, int layer)
		{
			if (root == null)
				return;

			root.gameObject.layer = layer;
			for (int i = 0; i < root.childCount; i++)
				SetLayerRecursively(root.GetChild(i), layer);
		}

		private EnemyTransformUnlockPickup ResolveTransformUnlockZone()
		{
			if (transformUnlockZone != null)
				return transformUnlockZone;

			transformUnlockZone = GetComponentInChildren<EnemyTransformUnlockPickup>(true);
			if (transformUnlockZone != null)
				return transformUnlockZone;

			Transform layered = FindChildByLayer("EnemyCanTranform");
			if (layered == null)
				return null;

			transformUnlockZone = layered.GetComponent<EnemyTransformUnlockPickup>();
			if (transformUnlockZone == null)
				transformUnlockZone = layered.gameObject.AddComponent<EnemyTransformUnlockPickup>();

			return transformUnlockZone;
		}

		private Transform FindChildByLayer(string layerName)
		{
			int targetLayer = LayerMask.NameToLayer(layerName);
			if (targetLayer < 0)
				return null;

			Transform[] children = GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < children.Length; i++)
			{
				if (children[i] != null && children[i].gameObject.layer == targetLayer)
					return children[i];
			}

			return null;
		}

		private PlayerFormDataSO ResolveUnlockFormData()
		{
			if (unlockFormData != null)
				return unlockFormData;

			PlayerFormConfig config = FindAnyObjectByType<PlayerFormConfig>();
			if (config == null)
				return null;

			for (int i = 0; i < config.forms.Count; i++)
			{
				PlayerFormDataSO entry = config.forms[i];
				if (entry == null || entry.enemySourcePrefab == null)
					continue;

				if (entry.enemySourcePrefab.GetComponent<MvEm0060>() != null)
					return entry;
			}

			return null;
		}
	}
}
