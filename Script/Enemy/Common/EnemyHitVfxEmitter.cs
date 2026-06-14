using DreamKnight.Player;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class EnemyHitVfxEmitter : MonoBehaviour
    {
        [Header("Hit Blood VFX")]
        [SerializeField] private GameObject hitBloodPrefab;
        [SerializeField] private bool usePoolManager = true;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;
        [SerializeField] private float fallbackDestroyDelay = 4f;

        public void PlayHitBlood(Vector3 worldPosition)
        {
            if (hitBloodPrefab == null) return;

            Vector3 spawnPos = worldPosition + spawnOffset;
            Quaternion spawnRot = Quaternion.identity;

            if (usePoolManager)
            {
                VfxPoolManager.Instance?.Spawn(hitBloodPrefab, spawnPos, spawnRot, spawnParent);
                return;
            }

            Transform parent = spawnParent != null ? spawnParent : null;
            GameObject instance = Instantiate(hitBloodPrefab, spawnPos, spawnRot, parent);
            if (instance == null) return;

			Destroy(instance, Mathf.Max(0.1f, fallbackDestroyDelay));
        }
    }
}
