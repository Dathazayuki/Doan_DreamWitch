using System;
using System.Collections.Generic;
using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.SaveLoad;
using UnityEngine;

namespace DreamKnight.Systems.SkillTree
{
    public class SkillTreeManager : MonoBehaviour
    {
        private static SkillTreeManager instance;

        [Header("Data")]
        [SerializeField] private SkillTreeDatabaseSO database;
        [SerializeField] private SkillTreeProgressSO progress;
        [SerializeField] private InventoryStateSO inventoryState;

        public static SkillTreeManager Instance => instance;
        public SkillTreeDatabaseSO Database => database;
        public SkillTreeProgressSO Progress => progress;
        public InventoryStateSO InventoryState => inventoryState;

        public event Action<SkillTreeNodeSO> OnNodeUnlocked;
        public event Action OnProgressChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnEnable()
        {
            if (progress != null)
                progress.OnNodeUnlocked += HandleNodeUnlocked;
        }

        private void OnDisable()
        {
            if (progress != null)
                progress.OnNodeUnlocked -= HandleNodeUnlocked;

            if (instance == this)
                instance = null;
        }

        public bool IsUnlocked(SkillTreeNodeSO node)
        {
            return node != null && IsUnlocked(node.NodeId);
        }

        public bool IsUnlocked(string nodeId)
        {
            return progress != null && progress.IsUnlocked(nodeId);
        }

        public bool CanUnlock(SkillTreeNodeSO node)
        {
            if (node == null || progress == null)
                return false;

            if (IsUnlocked(node))
                return false;

            if (!ArePrerequisitesUnlocked(node))
                return false;

            return HasRequiredUnlockItem(node);
        }

        public bool HasUnlockedPrerequisites(SkillTreeNodeSO node)
        {
            return node != null && ArePrerequisitesUnlocked(node);
        }

        public bool TryUnlock(SkillTreeNodeSO node)
        {
            if (!CanUnlock(node))
                return false;

            if (!SpendRequiredUnlockItem(node))
                return false;

            bool unlocked = progress.Unlock(node.NodeId);
            if (unlocked)
                GameAutoSave.Request("skill_tree_unlock");

            return unlocked;
        }

        public int GetRequiredItemQuantity(SkillTreeNodeSO node)
        {
            return node != null && node.RequiredItem != null ? node.RequiredItemQuantity : 0;
        }

        public int GetCurrentRequiredItemQuantity(SkillTreeNodeSO node)
        {
            if (node == null || node.RequiredItem == null || inventoryState == null)
                return 0;

            return inventoryState.GetQuantity(node.RequiredItem);
        }

        public bool HasRequiredUnlockItem(SkillTreeNodeSO node)
        {
            if (node == null)
                return false;

            if (node.RequiredItem == null)
                return false;

            return inventoryState != null && inventoryState.GetQuantity(node.RequiredItem) >= node.RequiredItemQuantity;
        }

        private bool SpendRequiredUnlockItem(SkillTreeNodeSO node)
        {
            if (node == null)
                return false;

            if (node.RequiredItem == null)
                return false;

            return inventoryState != null && inventoryState.RemoveItem(node.RequiredItem, node.RequiredItemQuantity);
        }

        public float GetUnlockedEffectValue(SkillTreeEffectType effectType)
        {
            IReadOnlyList<SkillTreeNodeSO> nodes = database != null ? database.Nodes : null;
            if (nodes == null)
                return 0f;

            float total = 0f;
            for (int i = 0; i < nodes.Count; i++)
            {
                SkillTreeNodeSO node = nodes[i];
                if (node != null && node.EffectType == effectType && IsUnlocked(node))
                    total += node.EffectValue;
            }

            return total;
        }

        public float GetComboThirdHitManaRestoreBonus()
        {
            return GetUnlockedEffectValue(SkillTreeEffectType.ComboThirdHitRestoreMana);
        }

        public float GetComboThirdHitExtraDamage()
        {
            return GetUnlockedEffectValue(SkillTreeEffectType.ComboThirdHitExtraDamage);
        }

        public bool IsTransformUnlocked()
        {
            return GetUnlockedEffectValue(SkillTreeEffectType.UnlockTransform) > 0f;
        }

        public bool IsDashInvincibleUnlocked()
        {
            return GetUnlockedEffectValue(SkillTreeEffectType.DashInvincible) > 0f;
        }

        public float GetCriticalDamageBonus()
        {
            return NormalizePercent(GetUnlockedEffectValue(SkillTreeEffectType.CriticalDamagePercent));
        }

        public float GetCriticalRateBonus()
        {
            return NormalizePercent(GetUnlockedEffectValue(SkillTreeEffectType.CriticalRatePercent));
        }

        public bool IsSpellBookSpellDamageUnlocked()
        {
            return GetUnlockedEffectValue(SkillTreeEffectType.SpellBookSpellDamagePercent) > 0f;
        }

        public float GetSpellBookSpellDamageBonus()
        {
            return NormalizePercent(GetUnlockedEffectValue(SkillTreeEffectType.SpellBookSpellDamagePercent));
        }

        public SkillTreeProgressSaveData CaptureSaveData()
        {
            return progress != null ? progress.CaptureSaveData() : new SkillTreeProgressSaveData();
        }

        public void LoadFromSaveData(SkillTreeProgressSaveData saveData)
        {
            if (progress == null)
                return;

            progress.LoadFromSaveData(saveData);
            OnProgressChanged?.Invoke();
        }

        private bool ArePrerequisitesUnlocked(SkillTreeNodeSO node)
        {
            IReadOnlyList<string> prerequisiteIds = node.PrerequisiteNodeIds;
            if (prerequisiteIds == null)
                return true;

            for (int i = 0; i < prerequisiteIds.Count; i++)
            {
                string prerequisiteId = prerequisiteIds[i];
                if (!string.IsNullOrWhiteSpace(prerequisiteId) && !IsUnlocked(prerequisiteId))
                    return false;
            }

            return true;
        }

        private void HandleNodeUnlocked(string nodeId)
        {
            SkillTreeNodeSO node = database != null ? database.FindById(nodeId) : null;
            if (node != null)
                OnNodeUnlocked?.Invoke(node);

            OnProgressChanged?.Invoke();
        }

        private static float NormalizePercent(float value)
        {
            return value > 1f ? value / 100f : Mathf.Max(0f, value);
        }
    }
}
