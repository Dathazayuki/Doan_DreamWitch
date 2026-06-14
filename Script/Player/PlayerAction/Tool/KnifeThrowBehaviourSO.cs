using System;
using System.Collections.Generic;
using DreamKnight.Systems.Inventory;
using UnityEngine;

namespace DreamKnight.Player
{
    /// <summary>
    /// Concrete ToolBehaviour xử lý việc ném dao (KnifeProjectile).
    ///
    /// Chứa toàn bộ tham số ném (prefab, speed, lifetime, offset, pool size)
    /// và logic pool — mọi logic throw đều ở đây, không phụ thuộc MonoBehaviour.
    ///
    /// Cách tạo: Create → DreamKnight/Tool Behaviours/Knife Throw
    /// Gán vào: ToolItemSO.behaviour
    /// </summary>
    [CreateAssetMenu(
        fileName = "KnifeThrowBehaviour",
        menuName  = "DreamKnight/Tool Behaviours/Knife Throw")]
    public class KnifeThrowBehaviourSO : ToolBehaviourSO
    {
        [Header("Projectile")]
        [Tooltip("Prefab KnifeProjectile.")]
        [SerializeField] private KnifeProjectile projectilePrefab;

        [Header("Throw Parameters")]
        [SerializeField] private float damage           = 10f;
        [SerializeField] private float projectileSpeed  = 12f;
        [SerializeField] private float projectileLifetime = 2f;
        [SerializeField] private Vector2 throwOffset    = new Vector2(0.65f, 0.35f);

        [Header("Pool")]
        [SerializeField] private int maxPoolSize = 24;

        // Expose để KnifeProjectile đọc
        public float Damage            => damage;
        public float ProjectileSpeed   => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;

        // ── Object Pool (per ScriptableObject instance) ──────────────────
        private readonly Queue<KnifeProjectile>           pooledInactive       = new Queue<KnifeProjectile>();
        private readonly Dictionary<KnifeProjectile, bool> activeProjectiles   = new Dictionary<KnifeProjectile, bool>();
        private Transform poolRoot;

        // ─────────────────────────────────────────────────────────────────
        public override bool Use(ItemUseContext context, PlayerToolAction toolAction)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[KnifeThrowBehaviour] projectilePrefab chưa gán!");
                return false;
            }

            PlayerController player = toolAction != null ? toolAction.PlayerController : null;

            Vector2 direction    = ResolveDirection(player, context);
            Vector3 spawnPos     = ResolveSpawnPosition(player, context, direction);

            KnifeProjectile projectile = SpawnProjectile(spawnPos);
            if (projectile == null)
                return false;

            GameObject owner = context?.User ?? toolAction?.gameObject;
            projectile.Launch(this, owner, direction, ReleaseProjectile);
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
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

        // ── Pool ──────────────────────────────────────────────────────────
        private KnifeProjectile SpawnProjectile(Vector3 position)
        {
            KnifeProjectile projectile = null;

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

        private void ReleaseProjectile(KnifeProjectile projectile)
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

        private void EnsurePoolRoot(KnifeProjectile hint)
        {
            if (poolRoot != null)
                return;

            GameObject go = new GameObject($"[Pool] KnifeThrow_{name}");
            poolRoot = go.transform;
            // Bám vào scene root, không bị destroy theo player
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                go, hint.gameObject.scene);
        }

        // Khi ScriptableObject bị unload (domain reload), dọn pool
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
