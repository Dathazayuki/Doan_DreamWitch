using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Currency
{
    [DisallowMultipleComponent]
    public class MoneyPickupPoolManager : MonoBehaviour
    {
        [SerializeField] private int defaultMaxPoolSizePerPrefab = 128;

        private static MoneyPickupPoolManager instance;
        private readonly Dictionary<int, Queue<MoneyPickup>> pooledInactive = new Dictionary<int, Queue<MoneyPickup>>();
        private readonly Dictionary<int, int> pooledTotalCount = new Dictionary<int, int>();
        private readonly Dictionary<int, Transform> poolRoots = new Dictionary<int, Transform>();

        public static MoneyPickupPoolManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindAnyObjectByType<MoneyPickupPoolManager>();
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("MoneyPickupPoolManager");
                instance = go.AddComponent<MoneyPickupPoolManager>();
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

        public MoneyPickup Spawn(MoneyPickup prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            int prefabId = prefab.gameObject.GetInstanceID();
            Queue<MoneyPickup> queue = GetOrCreateQueue(prefabId);

            MoneyPickup pickup = null;
            while (queue.Count > 0 && pickup == null)
                pickup = queue.Dequeue();

            if (pickup == null)
            {
                pickup = Instantiate(prefab);
                if (!pooledTotalCount.ContainsKey(prefabId))
                    pooledTotalCount[prefabId] = 0;
                pooledTotalCount[prefabId]++;
            }

            pickup.transform.SetParent(null, false);
            pickup.transform.SetPositionAndRotation(position, rotation);
            pickup.InitializePool(this, prefabId);
            pickup.gameObject.SetActive(true);
            return pickup;
        }

        public void Release(MoneyPickup pickup, int prefabId)
        {
            if (pickup == null)
                return;

            Queue<MoneyPickup> queue = GetOrCreateQueue(prefabId);
            int maxPoolSize = Mathf.Max(1, defaultMaxPoolSizePerPrefab);

            if (queue.Count >= maxPoolSize)
            {
                Destroy(pickup.gameObject);
                if (pooledTotalCount.ContainsKey(prefabId))
                    pooledTotalCount[prefabId] = Mathf.Max(0, pooledTotalCount[prefabId] - 1);
                return;
            }

            Transform root = GetOrCreatePoolRoot(prefabId);
            pickup.transform.SetParent(root, false);
            pickup.gameObject.SetActive(false);
            queue.Enqueue(pickup);
        }

        private Queue<MoneyPickup> GetOrCreateQueue(int prefabId)
        {
            if (!pooledInactive.TryGetValue(prefabId, out Queue<MoneyPickup> queue))
            {
                queue = new Queue<MoneyPickup>();
                pooledInactive[prefabId] = queue;
            }

            return queue;
        }

        private Transform GetOrCreatePoolRoot(int prefabId)
        {
            if (!poolRoots.TryGetValue(prefabId, out Transform root) || root == null)
            {
                GameObject go = new GameObject($"MoneyPool_{prefabId}");
                root = go.transform;
                root.SetParent(transform, false);
                poolRoots[prefabId] = root;
            }

            return root;
        }
    }
}
