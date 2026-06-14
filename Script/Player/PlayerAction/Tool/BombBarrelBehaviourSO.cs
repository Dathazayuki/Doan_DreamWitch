using System.Collections.Generic;
using DreamKnight.Systems.Inventory;
using UnityEngine;

namespace DreamKnight.Player
{
    [CreateAssetMenu(
        fileName = "BombBarrelBehaviour",
        menuName = "DreamKnight/Tool Behaviours/Bomb Barrel")]
    public class BombBarrelBehaviourSO : ToolBehaviourSO
    {
        [Header("Bomb")]
        [SerializeField] private BombBarrelTool bombPrefab;
        [SerializeField] private Vector2 placeOffset = new Vector2(0.65f, 0f);
        [SerializeField] private float fuseDuration = 2f;

        [Header("Explosion")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float explosionDamage = 30f;
        [SerializeField] private float explosionDuration = 1f;
        [SerializeField] private int explosionOverlapBufferSize = 32;

        [Header("Pool")]
        [SerializeField] private int maxPoolSize = 8;

        private readonly Queue<BombBarrelTool> pooledInactive = new Queue<BombBarrelTool>();
        private readonly Dictionary<BombBarrelTool, bool> activeBombs = new Dictionary<BombBarrelTool, bool>();
        private Transform poolRoot;

        public LayerMask EnemyLayer => enemyLayer;
        public float FuseDuration => Mathf.Max(0.01f, fuseDuration);
        public float ExplosionDamage => Mathf.Max(0f, explosionDamage);
        public float ExplosionDuration => Mathf.Max(0.01f, explosionDuration);
        public int ExplosionOverlapBufferSize => Mathf.Max(8, explosionOverlapBufferSize);

        public override bool Use(ItemUseContext context, PlayerToolAction toolAction)
        {
            if (bombPrefab == null)
            {
                Debug.LogWarning("[BombBarrelBehaviour] bombPrefab chua gan!");
                return false;
            }

            PlayerController player = toolAction != null ? toolAction.PlayerController : null;
            Vector2 direction = ResolveDirection(player, context);
            Vector3 spawnPos = ResolveSpawnPosition(player, context, direction);

            BombBarrelTool bomb = SpawnBomb(spawnPos);
            if (bomb == null)
                return false;

            GameObject owner = context?.User ?? toolAction?.gameObject;
            bomb.Place(this, owner, ReleaseBomb);
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

            Vector2 offset = placeOffset;
            offset.x *= direction.x >= 0f ? 1f : -1f;
            return basePos + (Vector3)offset;
        }

        private BombBarrelTool SpawnBomb(Vector3 position)
        {
            BombBarrelTool bomb = null;

            while (pooledInactive.Count > 0 && bomb == null)
                bomb = pooledInactive.Dequeue();

            if (bomb == null)
                bomb = Instantiate(bombPrefab);

            bomb.enabled = true;
            bomb.transform.SetParent(null, false);
            bomb.transform.position = position;
            bomb.transform.rotation = Quaternion.identity;
            bomb.gameObject.SetActive(true);
            activeBombs[bomb] = true;
            return bomb;
        }

        private void ReleaseBomb(BombBarrelTool bomb)
        {
            if (bomb == null)
                return;

            activeBombs.Remove(bomb);

            if (pooledInactive.Count >= Mathf.Max(1, maxPoolSize))
            {
                Destroy(bomb.gameObject);
                return;
            }

            EnsurePoolRoot(bomb);
            bomb.transform.SetParent(poolRoot, false);
            bomb.gameObject.SetActive(false);
            pooledInactive.Enqueue(bomb);
        }

        private void EnsurePoolRoot(BombBarrelTool hint)
        {
            if (poolRoot != null)
                return;

            GameObject go = new GameObject($"[Pool] BombBarrel_{name}");
            poolRoot = go.transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, hint.gameObject.scene);
        }

        private void OnDisable()
        {
            pooledInactive.Clear();
            activeBombs.Clear();
            if (poolRoot != null)
                Destroy(poolRoot.gameObject);
            poolRoot = null;
        }
    }
}
