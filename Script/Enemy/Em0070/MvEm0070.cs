using DreamKnight.Enemy;
using DreamKnight.Player;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0070 : MvEnemyBase, IMvAnimEventLiteListener
    {
        [Header("Em0070 – Attack Prefabs")]
        [Tooltip("Prefab FlyPile dùng khi Em0070 tấn công Player (Target Layer nên là Player).")]
        [SerializeField] private GameObject flyPilePrefab;
        [Tooltip("Prefab FireBall dùng khi Em0070 tấn công Player.")]
        [SerializeField] private GameObject fireBallPrefab;
        [Tooltip("Điểm spawn đạn.")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float attackDamage = 12f;

        [Header("Transform Unlock")]
        [SerializeField] private bool spawnTransformUnlockOnDeath = true;
        [SerializeField] private PlayerFormDataSO unlockFormData;
        [SerializeField] private EnemyTransformUnlockPickup transformUnlockZone;

        // Base class animator state names override
        protected override string IdleStateName       => "Idle";
        protected override string RunStateName        => "Run";
        protected override string AttackSignStateName => "AtkSign";
        protected override string AttackStateName
        {
            get
            {
                if (CurrentState is Em0070AtkState atkState)
                {
                    return atkState.SelectedAttackAnim;
                }
                return "Atk";
            }
        }
        protected override string HitStateName        => "Hit";
        protected override string DeadStateName       => "Death";

        private Animator cachedAnimator;

        public Animator CachedAnimator => cachedAnimator;

        protected override void Awake()
        {
            cachedAnimator = GetComponentInChildren<Animator>();
            base.Awake();
        }

        public string ChooseRandomAttack()
        {
            return Random.value < 0.5f ? "Atk" : "ShotL";
        }

        protected override EnemyState CreateAttackState(EnemyContext context)
        {
            return new Em0070AtkState(context);
        }

        public void OnMvAnimEvent(string eventName, MvAnimEventLite source)
        {
            if (!enabled || !IsAlive) return;

            if (eventName == "AtkS")
            {
                SpawnSelectedProjectile();
            }
        }

        private void SpawnSelectedProjectile()
        {
            if (cachedAnimator == null) return;

            AnimatorStateInfo stateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Atk"))
            {
                SpawnFlyPile();
            }
            else if (stateInfo.IsName("ShotL"))
            {
                SpawnFireBall();
            }
        }

        private void SpawnFlyPile()
        {
            if (flyPilePrefab == null) return;

            Transform sp = spawnPoint != null ? spawnPoint : transform;
            Vector2 dir = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
            if (CurrentTarget != null)
            {
                dir = (CurrentTarget.position - sp.position).normalized;
            }

            GameObject go = Instantiate(flyPilePrefab, sp.position, Quaternion.identity);
            var flyPile = go.GetComponent<DreamKnight.Systems.Skill.FlyPile>();
            if (flyPile != null)
            {
                flyPile.Initialize(gameObject, attackDamage, dir);
            }
        }

        private void SpawnFireBall()
        {
            if (fireBallPrefab == null) return;

            Transform sp = spawnPoint != null ? spawnPoint : transform;
            Vector2 dir = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

            var fireBallComp = fireBallPrefab.GetComponent<FireBall>();
            if (fireBallComp != null)
            {
                var fireBall = FireBallPoolManager.Instance.Spawn(fireBallComp, sp.position, Quaternion.identity);
                if (fireBall != null)
                {
                    fireBall.Initialize(gameObject, attackDamage, dir);
                }
            }
            else
            {
                GameObject go = Instantiate(fireBallPrefab, sp.position, Quaternion.identity);
                var fb = go.GetComponent<FireBall>();
                if (fb != null)
                {
                    fb.Initialize(gameObject, attackDamage, dir);
                }
            }
        }

        // Death & Transform Unlock logic
        protected override void Die()
        {
            base.Die();

            if (spawnTransformUnlockOnDeath)
                ActivateTransformUnlockZone();
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }

        private void ActivateTransformUnlockZone()
        {
            PlayerFormDataSO resolvedForm = ResolveUnlockFormData();
            if (resolvedForm == null)
                return;

            EnemyTransformUnlockPickup zone = ResolveTransformUnlockZone();
            if (zone == null)
            {
                Debug.LogWarning("[MvEm0070] Transform unlock zone not found.", this);
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
                if (col == null) continue;
                col.enabled   = true;
                col.isTrigger = true;
            }

            int layer = LayerMask.NameToLayer("EnemyCanTranform");
            if (layer >= 0)
                SetLayerRecursively(zone.transform, layer);
        }

        private void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null) return;
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
            if (layered == null) return null;

            transformUnlockZone = layered.GetComponent<EnemyTransformUnlockPickup>();
            if (transformUnlockZone == null)
                transformUnlockZone = layered.gameObject.AddComponent<EnemyTransformUnlockPickup>();

            return transformUnlockZone;
        }

        private Transform FindChildByLayer(string layerName)
        {
            int targetLayer = LayerMask.NameToLayer(layerName);
            if (targetLayer < 0) return null;

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

                if (entry.enemySourcePrefab.GetComponent<MvEm0070>() != null)
                    return entry;
            }

            return null;
        }
    }
}
