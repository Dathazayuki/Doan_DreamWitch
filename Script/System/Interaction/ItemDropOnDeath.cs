using Mv;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    [DisallowMultipleComponent]
    public class ItemDropOnDeath : MonoBehaviour
    {
        [System.Serializable]
        private class DropEntry
        {
            public ItemPickup itemPickupPrefab;
            [Range(0f, 100f)] public float dropChancePercent = 100f;
            public Vector3 spawnOffset;
        }

        [Header("References")]
        [SerializeField] private MvEnemyBase enemy;

        [Header("Drops")]
        [SerializeField] private DropEntry[] drops = new DropEntry[0];

        [Header("Spawn")]
        [SerializeField] private Vector3 defaultSpawnOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private float scatterRadius = 0.2f;

        private bool dropped;

        private void Awake()
        {
            if (enemy == null)
                enemy = GetComponent<MvEnemyBase>();
        }

        private void OnEnable()
        {
            dropped = false;
            if (enemy != null)
                enemy.OnDeath += HandleEnemyDeath;
        }

        private void OnDisable()
        {
            if (enemy != null)
                enemy.OnDeath -= HandleEnemyDeath;
        }

        private void HandleEnemyDeath()
        {
            if (dropped)
                return;

            dropped = true;

            if (drops == null || drops.Length == 0)
                return;

            for (int i = 0; i < drops.Length; i++)
            {
                DropEntry entry = drops[i];
                if (entry == null || entry.itemPickupPrefab == null)
                    continue;

                float chance = Mathf.Clamp01(entry.dropChancePercent / 100f);
                if (chance <= 0f || Random.value > chance)
                    continue;

                SpawnDrop(entry);
            }
        }

        private void SpawnDrop(DropEntry entry)
        {
            Vector3 offset = defaultSpawnOffset + entry.spawnOffset;
            Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, scatterRadius);
            Vector3 spawnPosition = transform.position + offset + new Vector3(scatter.x, scatter.y, 0f);
            Instantiate(entry.itemPickupPrefab, spawnPosition, Quaternion.identity);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            scatterRadius = Mathf.Max(0f, scatterRadius);

            if (drops == null)
                return;

            for (int i = 0; i < drops.Length; i++)
            {
                if (drops[i] != null)
                    drops[i].dropChancePercent = Mathf.Clamp(drops[i].dropChancePercent, 0f, 100f);
            }
        }
#endif
    }
}
