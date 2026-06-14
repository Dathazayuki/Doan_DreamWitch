using DreamKnight.Enemy;
using DreamKnight.Player;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0010 : MvEnemyBase
    {
        public enum As : byte
        {
            Attack = (byte)AsCommon.Max,
            Max
        }

        [Header("BoneDog Attack")]
        [SerializeField] private float attackLeapForwardSpeed = 4.2f;
        [SerializeField] private float attackLeapUpSpeed = 2.2f;
        [SerializeField] private float attackLeapDuration = 0.3f;

        [Header("Transform Unlock")]
        [SerializeField] private bool spawnTransformUnlockOnDeath = true;
        [SerializeField] private PlayerFormDataSO unlockFormData;
        [SerializeField] private EnemyTransformUnlockPickup transformUnlockZone;

        protected override string IdleStateName => "Idle";
        protected override string RunStateName => "Run";
        protected override string AttackSignStateName => "AtkSign";
        protected override string AttackStateName => "Atk1";
        protected override string HitStateName => "Hit";
        protected override string DeadStateName => "Death";

        public float AttackLeapForwardSpeed => Mathf.Max(0f, attackLeapForwardSpeed);
        public float AttackLeapUpSpeed => Mathf.Max(0f, attackLeapUpSpeed);
        public float AttackLeapDuration => Mathf.Max(0.05f, attackLeapDuration);

        private Rigidbody2D cachedRb;
        private bool leapDashActive;
        private float leapDashDir;
        private float leapDashRemaining;

        protected override EnemyState CreateAttackState(EnemyContext context)
        {
            return new Em0010AttackState(context);
        }

        protected override EnemyState CreateIdleState(EnemyContext context)
        {
            return new Em0010IdleState(context);
        }

        protected override EnemyState CreateRunState(EnemyContext context)
        {
            return new Em0010RunState(context);
        }

        protected override void Awake()
        {
            base.Awake();
            cachedRb = GetComponent<Rigidbody2D>();
        }

        protected override void FixedUpdate()
        {
            if (leapDashActive && cachedRb != null)
            {
                float dx = leapDashDir * AttackLeapForwardSpeed * Time.fixedDeltaTime;
                Vector2 next = cachedRb.position + new Vector2(dx, 0f);
                cachedRb.MovePosition(next);

                leapDashRemaining -= Time.fixedDeltaTime;
                if (leapDashRemaining <= 0f)
                    StopLeapDash();
            }

            base.FixedUpdate();
        }

        public void StartLeapDash(float deltaX, float upSpeed)
        {
            leapDashActive = true;
            leapDashDir = Mathf.Abs(deltaX) > 0.01f ? Mathf.Sign(deltaX) : 1f;
            leapDashRemaining = AttackLeapDuration;

            if (cachedRb != null)
                cachedRb.linearVelocity = new Vector2(cachedRb.linearVelocity.x, upSpeed);
        }

        public void StopLeapDash()
        {
            leapDashActive = false;
            leapDashRemaining = 0f;
            if (cachedRb != null)
                cachedRb.linearVelocity = new Vector2(0f, cachedRb.linearVelocity.y);
        }


        protected override void Die()
        {
            base.Die();

            if (spawnTransformUnlockOnDeath)
                ActivateTransformUnlockZone();
                cachedRb.bodyType = RigidbodyType2D.Static;
        }

        private void ActivateTransformUnlockZone()
        {
            PlayerFormDataSO resolvedForm = ResolveUnlockFormData();
            if (resolvedForm == null)
                return;

            EnemyTransformUnlockPickup zone = ResolveTransformUnlockZone();
            if (zone == null)
            {
                Debug.LogWarning("[MvEm0010] Transform unlock zone not found.", this);
                return;
            }

            zone.Initialize(resolvedForm, gameObject);
            zone.gameObject.SetActive(true);
            EnableTransformUnlockZoneColliders(zone);
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

                if (entry.enemySourcePrefab.GetComponent<MvEm0010>() != null)
                    return entry;
            }

            return null;
        }
    }
}
