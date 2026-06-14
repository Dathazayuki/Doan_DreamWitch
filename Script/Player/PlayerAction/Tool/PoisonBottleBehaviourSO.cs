using System.Collections.Generic;
using DreamKnight.Systems.Inventory;
using UnityEngine;

namespace DreamKnight.Player
{
    [CreateAssetMenu(
        fileName = "PoisonBottleBehaviour",
        menuName = "DreamKnight/Tool Behaviours/Poison Bottle")]
    public class PoisonBottleBehaviourSO : ToolBehaviourSO
    {
        [Header("Projectile")]
        [SerializeField] private PoisonBottleProjectile projectilePrefab;
        [SerializeField] private float projectileSpeed = 7f;
        [SerializeField] private float arcUpVelocity = 5f;
        [SerializeField] private float projectileLifetime = 2f;
        [SerializeField] private Vector2 throwOffset = new Vector2(0.65f, 0.35f);

        [Header("Poison")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float impactRadius = 1.8f;
        [SerializeField] private int poisonTickCount = 5;
        [SerializeField] private float poisonDamagePerTick = 5f;
        [SerializeField] private float poisonTickInterval = 0.5f;
        [SerializeField] private float releaseDelayAfterImpact = 1f;

        [Header("Pool")]
        [SerializeField] private int maxPoolSize = 16;

        private readonly Queue<PoisonBottleProjectile> pooledInactive = new Queue<PoisonBottleProjectile>();
        private readonly Dictionary<PoisonBottleProjectile, bool> activeProjectiles = new Dictionary<PoisonBottleProjectile, bool>();
        private Transform poolRoot;

        public float ProjectileSpeed => projectileSpeed;
        public float ArcUpVelocity => arcUpVelocity;
        public float ProjectileLifetime => projectileLifetime;
        public LayerMask EnemyLayer => enemyLayer;
        public LayerMask GroundLayer => groundLayer;
        public float ImpactRadius => Mathf.Max(0.01f, impactRadius);
        public int PoisonTickCount => Mathf.Max(0, poisonTickCount);
        public float PoisonDamagePerTick => Mathf.Max(0f, poisonDamagePerTick);
        public float PoisonTickInterval => Mathf.Max(0.01f, poisonTickInterval);
        public float ReleaseDelayAfterImpact => Mathf.Max(0.01f, releaseDelayAfterImpact);

        public override bool Use(ItemUseContext context, PlayerToolAction toolAction)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[PoisonBottleBehaviour] projectilePrefab chua gan!");
                return false;
            }

            PlayerController player = toolAction != null ? toolAction.PlayerController : null;
            Vector2 direction = ResolveDirection(player, context);
            Vector3 spawnPos = ResolveSpawnPosition(player, context, direction);

            PoisonBottleProjectile projectile = SpawnProjectile(spawnPos);
            if (projectile == null)
                return false;

            GameObject owner = context?.User ?? toolAction?.gameObject;
            projectile.Launch(this, owner, direction, ReleaseProjectile);
            return true;
        }

        private Vector2 ResolveDirection(PlayerController player, ItemUseContext context)
        {
            if (player?.Movement != null)
                return player.Movement.FacingRight ? Vector2.right : Vector2.left;

            if (context?.User != null)
                return context.User.transform.localScale.x < 0f ? Vector2.left : Vector2.right;

            return Vector2.right;
        }

        private Vector3 ResolveSpawnPosition(PlayerController player, ItemUseContext context, Vector2 direction)
        {
            Vector3 basePos = player != null
                ? player.transform.position
                : (context?.User != null ? context.User.transform.position : Vector3.zero);

            Vector2 offset = throwOffset;
            offset.x *= direction.x >= 0f ? 1f : -1f;
            return basePos + (Vector3)offset;
        }

        private PoisonBottleProjectile SpawnProjectile(Vector3 position)
        {
            PoisonBottleProjectile projectile = null;

            while (pooledInactive.Count > 0 && projectile == null)
                projectile = pooledInactive.Dequeue();

            if (projectile == null)
                projectile = Instantiate(projectilePrefab);

            projectile.enabled = true;
            projectile.transform.SetParent(null, false);
            projectile.transform.position = position;
            projectile.gameObject.SetActive(true);
            activeProjectiles[projectile] = true;
            return projectile;
        }

        private void ReleaseProjectile(PoisonBottleProjectile projectile)
        {
            if (projectile == null)
                return;

            activeProjectiles.Remove(projectile);

            if (pooledInactive.Count >= Mathf.Max(1, maxPoolSize))
            {
                Destroy(projectile.gameObject);
                return;
            }

            EnsurePoolRoot(projectile);
            projectile.transform.SetParent(poolRoot, false);
            projectile.gameObject.SetActive(false);
            pooledInactive.Enqueue(projectile);
        }

        private void EnsurePoolRoot(PoisonBottleProjectile hint)
        {
            if (poolRoot != null)
                return;

            GameObject go = new GameObject($"[Pool] PoisonBottle_{name}");
            poolRoot = go.transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, hint.gameObject.scene);
        }

        private void OnDisable()
        {
            pooledInactive.Clear();
            activeProjectiles.Clear();
            if (poolRoot != null)
                Destroy(poolRoot.gameObject);
            poolRoot = null;
        }
    }
}
