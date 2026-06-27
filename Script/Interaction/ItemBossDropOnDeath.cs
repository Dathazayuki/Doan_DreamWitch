using Mv;
using DreamKnight.Systems.SaveLoad;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    [DisallowMultipleComponent]
    public class ItemBossDropOnDeath : MonoBehaviour
    {
        [System.Serializable]
        private class BossDropEntry
        {
            public string uniqueDropId;
            public ItemPickup itemPickupPrefab;
            public Vector3 spawnOffset;
        }

        [Header("References")]
        [SerializeField] private MvEnemyBase boss;

        [Header("Drop Identity")]
        [SerializeField] private string bossDropId;

        [Header("Drops")]
        [SerializeField] private BossDropEntry[] drops = new BossDropEntry[0];

        [Header("Spawn")]
        [SerializeField] private Vector3 defaultSpawnOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private float scatterRadius = 0.25f;

        private bool dropped;

        private void Awake()
        {
            if (boss == null)
                boss = GetComponent<MvEnemyBase>();

            EnsureIds();
        }

        private void OnEnable()
        {
            dropped = false;
            if (boss != null)
                boss.OnDeath += HandleBossDeath;
        }

        private void OnDisable()
        {
            if (boss != null)
                boss.OnDeath -= HandleBossDeath;
        }

        private void HandleBossDeath()
        {
            if (dropped)
                return;

            dropped = true;

            if (drops == null || drops.Length == 0)
                return;

            EnsureIds();

            for (int i = 0; i < drops.Length; i++)
            {
                BossDropEntry entry = drops[i];
                if (entry == null || entry.itemPickupPrefab == null)
                    continue;

                string dropId = ResolveDropId(entry, i);
                if (WorldPickupSaveService.IsCollected(dropId))
                    continue;

                SpawnDrop(entry, dropId);
            }
        }

        private void SpawnDrop(BossDropEntry entry, string dropId)
        {
            Vector3 offset = defaultSpawnOffset + entry.spawnOffset;
            Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, scatterRadius);
            Vector3 spawnPosition = transform.position + offset + new Vector3(scatter.x, scatter.y, 0f);

            ItemPickup pickup = Instantiate(entry.itemPickupPrefab, spawnPosition, Quaternion.identity);
            if (pickup != null)
                pickup.ConfigureRuntimePickupId(dropId, true);
        }

        private string ResolveDropId(BossDropEntry entry, int index)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.uniqueDropId))
                return entry.uniqueDropId;

            string rootId = !string.IsNullOrWhiteSpace(bossDropId) ? bossDropId : gameObject.scene.name + "/" + name;
            return $"{rootId}/drop_{index}";
        }

        private void EnsureIds()
        {
            if (string.IsNullOrWhiteSpace(bossDropId))
                bossDropId = $"{gameObject.scene.name}/{name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";

            if (drops == null)
                return;

            for (int i = 0; i < drops.Length; i++)
            {
                BossDropEntry entry = drops[i];
                if (entry == null || !string.IsNullOrWhiteSpace(entry.uniqueDropId))
                    continue;

                entry.uniqueDropId = $"{bossDropId}/drop_{i}";
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureIds();
        }
#endif
    }
}
