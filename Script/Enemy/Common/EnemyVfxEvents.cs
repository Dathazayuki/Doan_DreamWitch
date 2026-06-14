using UnityEngine;
using DreamKnight.Player;

namespace Mv
{
    [DisallowMultipleComponent]
    public class EnemyVfxEvents : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MvAnimEventLite animEventRelay;
        [SerializeField] private VfxPoolManager vfxPoolManager;
        [SerializeField] private Transform defaultSpawnPoint;
        [SerializeField] private bool parentToSpawner = false;

        [Header("Attack / Trigger Prefabs")]
        [SerializeField] private GameObject atkSPrefab;
        [SerializeField] private GameObject atkEPrefab;
        [SerializeField] private GameObject trg0Prefab;
        [SerializeField] private GameObject trg1Prefab;
        [SerializeField] private GameObject trg2Prefab;
        [SerializeField] private GameObject trg3Prefab;
        [SerializeField] private GameObject trg4Prefab;
        [SerializeField] private GameObject trg5Prefab;
        [SerializeField] private GameObject trg6Prefab;

        [Header("SE Prefabs")]
        [SerializeField] private GameObject se0Prefab;
        [SerializeField] private GameObject se1Prefab;
        [SerializeField] private GameObject se2Prefab;

        [Header("FX Prefabs")]
        [SerializeField] private GameObject fx0Prefab;
        [SerializeField] private GameObject fx1Prefab;
        [SerializeField] private GameObject fx2Prefab;
        [SerializeField] private GameObject fx3Prefab;
        [SerializeField] private GameObject fx4Prefab;
        [SerializeField] private GameObject fx5Prefab;
        [SerializeField] private GameObject fx6Prefab;
        [SerializeField] private GameObject fx7Prefab;

        private void Awake()
        {
            if (animEventRelay == null)
            {
                animEventRelay = GetComponent<MvAnimEventLite>();
                if (animEventRelay == null)
                    animEventRelay = GetComponentInParent<MvAnimEventLite>();
                if (animEventRelay == null)
                    animEventRelay = GetComponentInChildren<MvAnimEventLite>(true);
            }

            if (animEventRelay == null)
                Debug.LogWarning($"[{nameof(EnemyVfxEvents)}] Missing {nameof(MvAnimEventLite)} relay on '{name}'. Animation events will spawn VFX but won't notify gameplay listeners.", this);

            if (vfxPoolManager == null)
                vfxPoolManager = VfxPoolManager.Instance;
        }

        private void SpawnVfx(GameObject prefab)
        {
            if (prefab == null) return;

            Transform spawnPoint = defaultSpawnPoint != null ? defaultSpawnPoint : transform;
            Transform parent = parentToSpawner ? spawnPoint : null;
            vfxPoolManager?.Spawn(prefab, spawnPoint.position, spawnPoint.rotation, parent);
        }

        private void RaiseEventAndSpawn(string eventName, GameObject prefab = null)
        {
            if (!string.IsNullOrEmpty(eventName))
                animEventRelay?.RaiseEvent(eventName);

            SpawnVfx(prefab);
        }

        public void AtkS() => RaiseEventAndSpawn("AtkS", atkSPrefab);
        public void AtkE() => RaiseEventAndSpawn("AtkE", atkEPrefab);

        public void Trg0() => RaiseEventAndSpawn("Trg0", trg0Prefab);
        public void Trg1() => RaiseEventAndSpawn("Trg1", trg1Prefab);
        public void Trg2() => RaiseEventAndSpawn("Trg2", trg2Prefab);
        public void Trg3() => RaiseEventAndSpawn("Trg3", trg3Prefab);
        public void Trg4() => RaiseEventAndSpawn("Trg4", trg4Prefab);
        public void Trg5() => RaiseEventAndSpawn("Trg5", trg5Prefab);
        public void Trg6() => RaiseEventAndSpawn("Trg6", trg6Prefab);

        public void SE0() => RaiseEventAndSpawn("SE0", se0Prefab);
        public void SE1() => RaiseEventAndSpawn("SE1", se1Prefab);
        public void SE2() => RaiseEventAndSpawn("SE2", se2Prefab);

        public void Fx0() => RaiseEventAndSpawn("Fx0", fx0Prefab);
        public void Fx1() => RaiseEventAndSpawn("Fx1", fx1Prefab);
        public void Fx2() => RaiseEventAndSpawn("Fx2", fx2Prefab);
        public void Fx3() => RaiseEventAndSpawn("Fx3", fx3Prefab);
        public void Fx4() => RaiseEventAndSpawn("Fx4", fx4Prefab);
        public void Fx5() => RaiseEventAndSpawn("Fx5", fx5Prefab);
        public void Fx6() => RaiseEventAndSpawn("Fx6", fx6Prefab);
        public void Fx7() => RaiseEventAndSpawn("Fx7", fx7Prefab);

        public void Col0() => RaiseEventAndSpawn("Col0");
        public void Col1() => RaiseEventAndSpawn("Col1");
        public void Col2() => RaiseEventAndSpawn("Col2");
        public void Col3() => RaiseEventAndSpawn("Col3");
        public void Col4() => RaiseEventAndSpawn("Col4");

        public void Shake0() => RaiseEventAndSpawn("Shake0");
        public void Shake1() => RaiseEventAndSpawn("Shake1");
        public void Shake2() => RaiseEventAndSpawn("Shake2");
        public void Shake3() => RaiseEventAndSpawn("Shake3");
        public void Shake4() => RaiseEventAndSpawn("Shake4");

        public void Dust0() => RaiseEventAndSpawn("Dust0");
        public void Dust1() => RaiseEventAndSpawn("Dust1");
        public void Dust2() => RaiseEventAndSpawn("Dust2");
        public void Dust3() => RaiseEventAndSpawn("Dust3");
        public void Dust4() => RaiseEventAndSpawn("Dust4");

        public void Wing0() => RaiseEventAndSpawn("Wing0");
        public void Wing1() => RaiseEventAndSpawn("Wing1");

        public void SA_S() => RaiseEventAndSpawn("SA_S");
        public void SA_E() => RaiseEventAndSpawn("SA_E");

        public void CancelR() => RaiseEventAndSpawn("CancelR");
        public void CancelS() => RaiseEventAndSpawn("CancelS");
        public void CancelE() => RaiseEventAndSpawn("CancelE");
        public void CancelMove() => RaiseEventAndSpawn("CancelMove");

        public void EventByName(string eventName) => RaiseEventAndSpawn(eventName);
    }
}