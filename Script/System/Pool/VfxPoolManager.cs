using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DreamKnight.Player
{
    public class VfxPoolManager : MonoBehaviour
    {
        [SerializeField] private int defaultMaxPoolSizePerPrefab = 24;

        private readonly Dictionary<int, Queue<GameObject>> pooledInactive = new Dictionary<int, Queue<GameObject>>();
        private readonly Dictionary<int, int> pooledTotalCount = new Dictionary<int, int>();
        private readonly Dictionary<int, Transform> poolRoots = new Dictionary<int, Transform>();
        private readonly List<(GameObject obj, int id)> deferredReleaseQueue = new List<(GameObject, int)>();

        private static VfxPoolManager instance;
        public static VfxPoolManager Instance
        {
            get
            {
                if (instance != null) return instance;

                instance = FindAnyObjectByType<VfxPoolManager>();
                if (instance != null) return instance;

                GameObject go = new GameObject("VfxPoolManager");
                instance = go.AddComponent<VfxPoolManager>();
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

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            int prefabId = prefab.GetInstanceID();
            Queue<GameObject> queue = GetOrCreateQueue(prefabId);

            GameObject instanceObject = null;
            while (queue.Count > 0 && instanceObject == null)
            {
                instanceObject = queue.Dequeue();
            }

            if (instanceObject == null)
            {
                instanceObject = Instantiate(prefab);
                if (!pooledTotalCount.ContainsKey(prefabId))
                    pooledTotalCount[prefabId] = 0;
                pooledTotalCount[prefabId]++;
            }

            if (parent != null)
                instanceObject.transform.SetParent(parent, false);
            else
                instanceObject.transform.SetParent(null, false);

            instanceObject.transform.position = position;
            instanceObject.transform.rotation = rotation;
            instanceObject.SetActive(true);

            PooledVfxAutoRelease autoRelease = instanceObject.GetComponent<PooledVfxAutoRelease>();
            if (autoRelease == null)
                autoRelease = instanceObject.AddComponent<PooledVfxAutoRelease>();
            autoRelease.Initialize(this, prefabId);

            PlayVfx(instanceObject);
            return instanceObject;
        }

        /// <summary>
        /// Gọi từ OnDisable – defer sang frame tiếp để tránh SetParent trong callback deactivation.
        /// </summary>
        public void DeferRelease(GameObject instanceObject, int prefabId)
        {
            if (instanceObject == null) return;
            deferredReleaseQueue.Add((instanceObject, prefabId));
        }

        private void Update()
        {
            if (deferredReleaseQueue.Count == 0) return;
            for (int i = 0; i < deferredReleaseQueue.Count; i++)
            {
                var (obj, id) = deferredReleaseQueue[i];
                if (obj != null)
                    Release(obj, id);
            }
            deferredReleaseQueue.Clear();
        }

        public void Release(GameObject instanceObject, int prefabId)
        {
            if (instanceObject == null) return;

            Queue<GameObject> queue = GetOrCreateQueue(prefabId);
            int maxPoolSize = Mathf.Max(1, defaultMaxPoolSizePerPrefab);

            if (queue.Count >= maxPoolSize)
            {
                Destroy(instanceObject);
                if (pooledTotalCount.ContainsKey(prefabId))
                    pooledTotalCount[prefabId] = Mathf.Max(0, pooledTotalCount[prefabId] - 1);
                return;
            }

            Transform root = GetOrCreatePoolRoot(prefabId);
            instanceObject.transform.SetParent(root, false);
            instanceObject.SetActive(false);
            queue.Enqueue(instanceObject);
        }

        private Queue<GameObject> GetOrCreateQueue(int prefabId)
        {
            if (!pooledInactive.TryGetValue(prefabId, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pooledInactive[prefabId] = queue;
            }
            return queue;
        }

        private Transform GetOrCreatePoolRoot(int prefabId)
        {
            if (!poolRoots.TryGetValue(prefabId, out Transform root) || root == null)
            {
                GameObject go = new GameObject($"Pool_{prefabId}");
                root = go.transform;
                root.SetParent(transform, false);
                poolRoots[prefabId] = root;
            }
            return root;
        }

		private void PlayVfx(GameObject instanceObject)
		{
			if (instanceObject.GetComponent<Mv.MvFx>() == null && instanceObject.GetComponentInChildren<Mv.MvFx>(true) == null)
			{
				ParticleSystem[] particleSystems = instanceObject.GetComponentsInChildren<ParticleSystem>(true);
				foreach (ParticleSystem ps in particleSystems)
				{
					ps.gameObject.SetActive(true);
					ps.Clear(true);
					ps.Play(true);
				}
			}

            Type vfxType = Type.GetType("UnityEngine.VFX.VisualEffect, Unity.VisualEffectGraph.Runtime");
            if (vfxType == null) return;

            Component[] vfxComponents = instanceObject.GetComponentsInChildren(vfxType, true);
            MethodInfo reinitMethod = vfxType.GetMethod("Reinit", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo playMethod = vfxType.GetMethod("Play", BindingFlags.Instance | BindingFlags.Public);

            foreach (Component vfxComponent in vfxComponents)
            {
                if (vfxComponent == null) continue;
                vfxComponent.gameObject.SetActive(true);
                reinitMethod?.Invoke(vfxComponent, null);
                playMethod?.Invoke(vfxComponent, null);
            }
        }
    }
}
