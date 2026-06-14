using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    [DisallowMultipleComponent]
    public class BigSwordSpellPoolManager : MonoBehaviour
    {
        private static BigSwordSpellPoolManager instance;

        private readonly Dictionary<int, Queue<BigSwordSpellProjectile>> inactiveByPrefab = new Dictionary<int, Queue<BigSwordSpellProjectile>>();
        private readonly Dictionary<int, Transform> rootsByPrefab = new Dictionary<int, Transform>();
        private readonly Dictionary<BigSwordSpellProjectile, int> activePrefabIds = new Dictionary<BigSwordSpellProjectile, int>();

        public static BigSwordSpellPoolManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindAnyObjectByType<BigSwordSpellPoolManager>();
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("BigSwordSpellPoolManager");
                instance = go.AddComponent<BigSwordSpellPoolManager>();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public BigSwordSpellProjectile Spawn(
            BigSwordSpellProjectile prefab,
            Vector3 position,
            Quaternion rotation)
        {
            if (prefab == null)
                return null;

            int prefabId = prefab.GetInstanceID();
            Queue<BigSwordSpellProjectile> queue = GetOrCreateQueue(prefabId);

            BigSwordSpellProjectile projectile = null;
            while (queue.Count > 0 && projectile == null)
                projectile = queue.Dequeue();

            if (projectile == null)
                projectile = Instantiate(prefab);

            projectile.transform.SetParent(null, false);
            projectile.transform.position = position;
            projectile.transform.rotation = rotation;
            projectile.gameObject.SetActive(true);
            activePrefabIds[projectile] = prefabId;
            return projectile;
        }

        public void Release(BigSwordSpellProjectile projectile, int maxPoolSize)
        {
            if (projectile == null)
                return;

            if (!activePrefabIds.TryGetValue(projectile, out int prefabId))
            {
                projectile.gameObject.SetActive(false);
                return;
            }

            activePrefabIds.Remove(projectile);
            Queue<BigSwordSpellProjectile> queue = GetOrCreateQueue(prefabId);
            int limit = Mathf.Max(1, maxPoolSize);
            if (queue.Count >= limit)
            {
                Destroy(projectile.gameObject);
                return;
            }

            projectile.transform.SetParent(GetOrCreateRoot(prefabId), false);
            projectile.gameObject.SetActive(false);
            queue.Enqueue(projectile);
        }

        private Queue<BigSwordSpellProjectile> GetOrCreateQueue(int prefabId)
        {
            if (!inactiveByPrefab.TryGetValue(prefabId, out Queue<BigSwordSpellProjectile> queue))
            {
                queue = new Queue<BigSwordSpellProjectile>();
                inactiveByPrefab[prefabId] = queue;
            }

            return queue;
        }

        private Transform GetOrCreateRoot(int prefabId)
        {
            if (!rootsByPrefab.TryGetValue(prefabId, out Transform root) || root == null)
            {
                GameObject go = new GameObject($"[Pool] BigSwordSpell_{prefabId}");
                root = go.transform;
                root.SetParent(transform, false);
                rootsByPrefab[prefabId] = root;
            }

            return root;
        }
    }
}
