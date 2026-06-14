using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Enemy
{
    [DisallowMultipleComponent]
    public class FireBallPoolManager : MonoBehaviour
    {
        [SerializeField] private int defaultMaxPoolSizePerPrefab = 64;

        private static FireBallPoolManager instance;
        private readonly Dictionary<int, Queue<FireBall>> pooledInactive = new Dictionary<int, Queue<FireBall>>();
        private readonly Dictionary<int, int> pooledTotalCount = new Dictionary<int, int>();
        private readonly Dictionary<int, Transform> poolRoots = new Dictionary<int, Transform>();

        public static FireBallPoolManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindAnyObjectByType<FireBallPoolManager>();
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("FireBallPoolManager");
                instance = go.AddComponent<FireBallPoolManager>();
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

        public FireBall Spawn(FireBall prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            int prefabId = prefab.gameObject.GetInstanceID();
            Queue<FireBall> queue = GetOrCreateQueue(prefabId);

            FireBall fireBall = null;
            while (queue.Count > 0 && fireBall == null)
                fireBall = queue.Dequeue();

            if (fireBall == null)
            {
                fireBall = Instantiate(prefab);
                if (!pooledTotalCount.ContainsKey(prefabId))
                    pooledTotalCount[prefabId] = 0;
                pooledTotalCount[prefabId]++;
            }

            fireBall.transform.SetParent(null, false);
            fireBall.transform.SetPositionAndRotation(position, rotation);
            fireBall.InitializePool(this, prefabId);
            fireBall.gameObject.SetActive(true);
            return fireBall;
        }

        public void Release(FireBall fireBall, int prefabId)
        {
            if (fireBall == null)
                return;

            Queue<FireBall> queue = GetOrCreateQueue(prefabId);
            int maxPoolSize = Mathf.Max(1, defaultMaxPoolSizePerPrefab);

            if (queue.Count >= maxPoolSize)
            {
                Destroy(fireBall.gameObject);
                if (pooledTotalCount.ContainsKey(prefabId))
                    pooledTotalCount[prefabId] = Mathf.Max(0, pooledTotalCount[prefabId] - 1);
                return;
            }

            Transform root = GetOrCreatePoolRoot(prefabId);
            fireBall.transform.SetParent(root, false);
            fireBall.gameObject.SetActive(false);
            queue.Enqueue(fireBall);
        }

        private Queue<FireBall> GetOrCreateQueue(int prefabId)
        {
            if (!pooledInactive.TryGetValue(prefabId, out Queue<FireBall> queue))
            {
                queue = new Queue<FireBall>();
                pooledInactive[prefabId] = queue;
            }

            return queue;
        }

        private Transform GetOrCreatePoolRoot(int prefabId)
        {
            if (!poolRoots.TryGetValue(prefabId, out Transform root) || root == null)
            {
                GameObject go = new GameObject($"FireBallPool_{prefabId}");
                root = go.transform;
                root.SetParent(transform, false);
                poolRoots[prefabId] = root;
            }

            return root;
        }
    }
}
