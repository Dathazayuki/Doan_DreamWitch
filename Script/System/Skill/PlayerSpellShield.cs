using System;
using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    [DisallowMultipleComponent]
    public class PlayerSpellShield : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;

        private GameObject activeVisual;
        private GameObject activeVisualPrefab;
        private int remainingBlocks;

        public event Action<int> OnShieldChanged;

        public bool IsActive => remainingBlocks > 0;
        public int RemainingBlocks => remainingBlocks;

        public static PlayerSpellShield GetOrCreate(Transform owner)
        {
            if (owner == null)
                return null;

            PlayerSpellShield shield = owner.GetComponent<PlayerSpellShield>();
            if (shield == null)
                shield = owner.gameObject.AddComponent<PlayerSpellShield>();

            return shield;
        }

        public void Activate(int blockCount, GameObject visualPrefab)
        {
            if (IsActive)
                return;

            remainingBlocks = Mathf.Max(0, blockCount);
            RefreshVisual(visualPrefab);
            OnShieldChanged?.Invoke(remainingBlocks);
        }

        public bool TryBlockDamage(float damage, GameObject damageSource = null)
        {
            if (remainingBlocks <= 0)
                return false;

            remainingBlocks--;
            if (remainingBlocks <= 0)
                DeactivateVisual();

            OnShieldChanged?.Invoke(remainingBlocks);
            return true;
        }

        public void Clear()
        {
            remainingBlocks = 0;
            DeactivateVisual();
            OnShieldChanged?.Invoke(remainingBlocks);
        }

        private void RefreshVisual(GameObject visualPrefab)
        {
            if (remainingBlocks <= 0)
            {
                DeactivateVisual();
                return;
            }

            if (visualPrefab == null)
            {
                DeactivateVisual();
                return;
            }

            Transform parent = visualRoot != null ? visualRoot : transform;
            if (activeVisual == null || activeVisualPrefab != visualPrefab)
            {
                DeactivateVisual();
                activeVisual = Instantiate(visualPrefab, parent);
                activeVisualPrefab = visualPrefab;
            }
            else
            {
                activeVisual.transform.SetParent(parent, false);
            }

            activeVisual.transform.localPosition = Vector3.zero;
            activeVisual.transform.localRotation = Quaternion.identity;
            activeVisual.transform.localScale = Vector3.one;
            activeVisual.SetActive(true);
        }

        private void DeactivateVisual()
        {
            if (activeVisual != null)
                activeVisual.SetActive(false);
        }
    }
}
