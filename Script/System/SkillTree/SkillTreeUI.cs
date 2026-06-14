using System;
using System.Collections.Generic;
using DreamKnight.Systems.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.Systems.SkillTree
{
    public class SkillTreeUI : MonoBehaviour
    {
        [Serializable]
        private class SkillTreeNodeBinding
        {
            public SkillTreeNodeSO node;
            public Button button;
            public Image iconImage;
            public TextMeshProUGUI nameText;
            public GameObject selectedObject;
            public GameObject lockedObject;
            public GameObject unlockableObject;
            public GameObject unlockedObject;
        }

        [Header("Data")]
        [SerializeField] private SkillTreeManager skillTreeManager;
        [SerializeField] private InventoryStateSO inventoryState;

        [Header("Placed Nodes")]
        [SerializeField] private List<SkillTreeNodeBinding> nodeBindings = new List<SkillTreeNodeBinding>();

        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI unlockItemText;
        [SerializeField] private GameObject emptyStateObject;

        [Header("Detail Panel")]
        [SerializeField] private Image selectedIconImage;
        [SerializeField] private TextMeshProUGUI selectedNameText;
        [SerializeField] private TextMeshProUGUI selectedDescriptionText;
        [SerializeField] private TextMeshProUGUI selectedPriceText;
        [SerializeField] private TextMeshProUGUI selectedStatusText;
        [SerializeField] private TextMeshProUGUI selectedEffectText;
        [SerializeField] private Button unlockSelectedButton;
        [SerializeField] private TextMeshProUGUI unlockButtonLabelText;

        private SkillTreeNodeSO selectedNode;

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            BindNodeButtons();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearNodeButtons();
        }

        public void Refresh()
        {
            ResolveReferences();
            EnsureSelectedNode();
            RefreshUnlockItemText();
            RefreshNodes();
            RefreshDetailPanel();
        }

        private void Subscribe()
        {
            if (skillTreeManager != null)
            {
                skillTreeManager.OnNodeUnlocked += HandleNodeUnlocked;
                skillTreeManager.OnProgressChanged += HandleProgressChanged;
            }

            InventoryStateSO inventory = GetInventory();
            if (inventory != null)
                inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        private void Unsubscribe()
        {
            if (skillTreeManager != null)
            {
                skillTreeManager.OnNodeUnlocked -= HandleNodeUnlocked;
                skillTreeManager.OnProgressChanged -= HandleProgressChanged;
            }

            InventoryStateSO inventory = GetInventory();
            if (inventory != null)
                inventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        private void BindNodeButtons()
        {
            for (int i = 0; i < nodeBindings.Count; i++)
            {
                SkillTreeNodeBinding binding = nodeBindings[i];
                if (binding == null || binding.button == null)
                    continue;

                SkillTreeNodeSO node = binding.node;
                binding.button.onClick.RemoveAllListeners();
                binding.button.onClick.AddListener(() => SelectNode(node));
            }
        }

        private void ClearNodeButtons()
        {
            for (int i = 0; i < nodeBindings.Count; i++)
            {
                SkillTreeNodeBinding binding = nodeBindings[i];
                if (binding != null && binding.button != null)
                    binding.button.onClick.RemoveAllListeners();
            }
        }

        private void SelectNode(SkillTreeNodeSO node)
        {
            if (node == null)
                return;

            selectedNode = node;
            Refresh();
        }

        private void TryUnlockSelectedNode()
        {
            if (skillTreeManager == null || selectedNode == null)
                return;

            skillTreeManager.TryUnlock(selectedNode);
        }

        private void RefreshNodes()
        {
            bool hasNode = false;

            for (int i = 0; i < nodeBindings.Count; i++)
            {
                SkillTreeNodeBinding binding = nodeBindings[i];
                if (binding == null)
                    continue;

                SkillTreeNodeSO node = binding.node;
                bool valid = node != null;
                hasNode |= valid;

                bool unlocked = valid && skillTreeManager != null && skillTreeManager.IsUnlocked(node);
                bool prereqUnlocked = valid && skillTreeManager != null && skillTreeManager.HasUnlockedPrerequisites(node);
                bool unlockable = valid && !unlocked && prereqUnlocked;
                bool selected = valid && node == selectedNode;

                if (binding.iconImage != null)
                {
                    binding.iconImage.sprite = valid ? node.Icon : null;
                    binding.iconImage.enabled = valid && node.Icon != null;
                }

                if (binding.nameText != null)
                    binding.nameText.text = valid ? node.DisplayName : string.Empty;

                SetActive(binding.selectedObject, selected);
                SetActive(binding.lockedObject, valid && !unlocked && !prereqUnlocked);
                SetActive(binding.unlockableObject, unlockable);
                SetActive(binding.unlockedObject, unlocked);

                if (binding.button != null)
                    binding.button.interactable = valid;
            }

            SetActive(emptyStateObject, !hasNode);
        }

        private void RefreshDetailPanel()
        {
            bool hasSelection = selectedNode != null;
            bool unlocked = hasSelection && skillTreeManager != null && skillTreeManager.IsUnlocked(selectedNode);
            bool prereqUnlocked = hasSelection && skillTreeManager != null && skillTreeManager.HasUnlockedPrerequisites(selectedNode);
            bool canUnlock = hasSelection && skillTreeManager != null && skillTreeManager.CanUnlock(selectedNode);

            if (selectedIconImage != null)
            {
                selectedIconImage.sprite = hasSelection ? selectedNode.Icon : null;
                selectedIconImage.enabled = hasSelection && selectedNode.Icon != null;
            }

            if (selectedNameText != null)
                selectedNameText.text = hasSelection ? selectedNode.DisplayName : string.Empty;

            if (selectedDescriptionText != null)
                selectedDescriptionText.text = hasSelection ? selectedNode.Description : string.Empty;

            if (selectedPriceText != null)
                selectedPriceText.text = hasSelection && !unlocked ? FormatRequiredItem(selectedNode) : string.Empty;

            if (selectedStatusText != null)
                selectedStatusText.text = FormatStatus(hasSelection, unlocked, prereqUnlocked, canUnlock);

            if (selectedEffectText != null)
                selectedEffectText.text = hasSelection ? FormatEffect(selectedNode) : string.Empty;

            if (unlockSelectedButton != null)
            {
                unlockSelectedButton.onClick.RemoveAllListeners();
                unlockSelectedButton.interactable = canUnlock;
                if (hasSelection)
                    unlockSelectedButton.onClick.AddListener(TryUnlockSelectedNode);
            }

            if (unlockButtonLabelText != null)
                unlockButtonLabelText.text = FormatUnlockButtonLabel(hasSelection, unlocked, prereqUnlocked);
        }

        private void RefreshUnlockItemText()
        {
            if (unlockItemText == null)
                return;

            if (selectedNode == null || selectedNode.RequiredItem == null)
            {
                unlockItemText.text = string.Empty;
                return;
            }

            int current = skillTreeManager != null ? skillTreeManager.GetCurrentRequiredItemQuantity(selectedNode) : 0;
            unlockItemText.text = $"{selectedNode.RequiredItem.DisplayName}: {current}";
        }

        private void EnsureSelectedNode()
        {
            if (selectedNode != null && ContainsNode(selectedNode))
                return;

            selectedNode = FindFirstNode();
        }

        private bool ContainsNode(SkillTreeNodeSO node)
        {
            if (node == null)
                return false;

            for (int i = 0; i < nodeBindings.Count; i++)
            {
                if (nodeBindings[i] != null && nodeBindings[i].node == node)
                    return true;
            }

            return false;
        }

        private SkillTreeNodeSO FindFirstNode()
        {
            for (int i = 0; i < nodeBindings.Count; i++)
            {
                if (nodeBindings[i] != null && nodeBindings[i].node != null)
                    return nodeBindings[i].node;
            }

            return null;
        }

        private InventoryStateSO GetInventory()
        {
            if (inventoryState != null)
                return inventoryState;

            return skillTreeManager != null ? skillTreeManager.InventoryState : null;
        }

        private void ResolveReferences()
        {
            if (skillTreeManager == null)
                skillTreeManager = SkillTreeManager.Instance;
        }

        private void HandleNodeUnlocked(SkillTreeNodeSO node)
        {
            Refresh();
        }

        private void HandleProgressChanged()
        {
            Refresh();
        }

        private void HandleInventoryChanged()
        {
            Refresh();
        }

        private static string FormatStatus(bool hasSelection, bool unlocked, bool prereqUnlocked, bool canUnlock)
        {
            if (!hasSelection)
                return string.Empty;

            if (unlocked)
                return "Unlocked";

            if (!prereqUnlocked)
                return "Locked";

            return canUnlock ? "Ready" : "Need Item";
        }

        private static string FormatUnlockButtonLabel(bool hasSelection, bool unlocked, bool prereqUnlocked)
        {
            if (!hasSelection)
                return string.Empty;

            if (unlocked)
                return "Unlocked";

            return prereqUnlocked ? "Unlock" : "Locked";
        }

        private static string FormatEffect(SkillTreeNodeSO node)
        {
            if (node == null)
                return string.Empty;

            switch (node.EffectType)
            {
                case SkillTreeEffectType.ComboThirdHitRestoreMana:
                    return $"+{node.EffectValue:0.##} mana on combo 3";
                case SkillTreeEffectType.ComboThirdHitExtraDamage:
                    return $"+{node.EffectValue:0.##} damage on combo 3";
                case SkillTreeEffectType.DashInvincible:
                    return "Invincible while dashing";
                case SkillTreeEffectType.CriticalDamagePercent:
                    return $"+{FormatPercent(node.EffectValue)} critical damage";
                case SkillTreeEffectType.CriticalRatePercent:
                    return $"+{FormatPercent(node.EffectValue)} critical rate";
                case SkillTreeEffectType.SpellBookSpellDamagePercent:
                    return $"+{FormatPercent(node.EffectValue)} spell damage";
                case SkillTreeEffectType.UnlockTransform:
                    return "Unlock transform";
                default:
                    return string.Empty;
            }
        }

        private string FormatRequiredItem(SkillTreeNodeSO node)
        {
            if (node == null || node.RequiredItem == null)
                return "No unlock item";

            int current = skillTreeManager != null ? skillTreeManager.GetCurrentRequiredItemQuantity(node) : 0;
            int required = skillTreeManager != null ? skillTreeManager.GetRequiredItemQuantity(node) : node.RequiredItemQuantity;
            return $"{node.RequiredItem.DisplayName} {required}/{current}";
        }

        private static string FormatPercent(float value)
        {
            float normalized = value > 1f ? value : value * 100f;
            return $"{normalized:0.##}%";
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
