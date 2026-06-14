using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    [DisallowMultipleComponent]
    public class SpellProjectilePoolManager : MonoBehaviour
    {
        private static SpellProjectilePoolManager instance;

        private readonly Dictionary<int, Queue<ForwardSpellProjectile>> inactiveByPrefab = new Dictionary<int, Queue<ForwardSpellProjectile>>();
        private readonly Dictionary<int, Transform> rootsByPrefab = new Dictionary<int, Transform>();
        private readonly Dictionary<ForwardSpellProjectile, int> activePrefabIds = new Dictionary<ForwardSpellProjectile, int>();

        public static SpellProjectilePoolManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindAnyObjectByType<SpellProjectilePoolManager>();
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("SpellProjectilePoolManager");
                instance = go.AddComponent<SpellProjectilePoolManager>();
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

        public ForwardSpellProjectile Spawn(
            ForwardSpellProjectile prefab,
            Vector3 position,
            Quaternion rotation,
            int maxPoolSize)
        {
            if (prefab == null)
                return null;

            int prefabId = prefab.GetInstanceID();
            Queue<ForwardSpellProjectile> queue = GetOrCreateQueue(prefabId);

            ForwardSpellProjectile projectile = null;
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

        public void Release(ForwardSpellProjectile projectile, int maxPoolSize)
        {
            if (projectile == null)
                return;

            if (!activePrefabIds.TryGetValue(projectile, out int prefabId))
            {
                projectile.gameObject.SetActive(false);
                return;
            }

            activePrefabIds.Remove(projectile);
            Queue<ForwardSpellProjectile> queue = GetOrCreateQueue(prefabId);
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

        private Queue<ForwardSpellProjectile> GetOrCreateQueue(int prefabId)
        {
            if (!inactiveByPrefab.TryGetValue(prefabId, out Queue<ForwardSpellProjectile> queue))
            {
                queue = new Queue<ForwardSpellProjectile>();
                inactiveByPrefab[prefabId] = queue;
            }

            return queue;
        }

        private Transform GetOrCreateRoot(int prefabId)
        {
            if (!rootsByPrefab.TryGetValue(prefabId, out Transform root) || root == null)
            {
                GameObject go = new GameObject($"[Pool] SpellProjectile_{prefabId}");
                root = go.transform;
                root.SetParent(transform, false);
                rootsByPrefab[prefabId] = root;
            }

            return root;
        }
    }
}
