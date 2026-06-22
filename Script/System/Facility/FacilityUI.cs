using System.Collections.Generic;
using DreamKnight.Systems.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DreamKnight.Systems.Facility
{
    public class FacilityUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private FacilityManager facilityManager;
        [SerializeField] private InventoryStateSO inventoryState;

        [Header("View")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private FacilityUpgradeView upgradeViewPrefab;
        [FormerlySerializedAs("goldText")]
        [SerializeField] private TextMeshProUGUI materialText;
        [SerializeField] private Image materialIconImage;
        [SerializeField] private GameObject emptyStateObject;

        [Header("Detail Panel")]
        [SerializeField] private Image selectedIconImage;
        [SerializeField] private Image selectedCostIconImage;
        [SerializeField] private TextMeshProUGUI selectedNameText;
        [SerializeField] private TextMeshProUGUI selectedLevelText;
        [FormerlySerializedAs("selectedPriceText")]
        [SerializeField] private TextMeshProUGUI selectedMaterialText;
        [SerializeField] private TextMeshProUGUI selectedCurrentBonusText;
        [SerializeField] private TextMeshProUGUI selectedNextBonusText;
        [SerializeField] private TextMeshProUGUI selectedDescriptionText;
        [SerializeField] private Button upgradeSelectedButton;
        [SerializeField] private TextMeshProUGUI upgradeButtonLabelText;

        private readonly List<FacilityUpgradeView> spawnedViews = new List<FacilityUpgradeView>();
        private FacilityUpgradeSO selectedUpgrade;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearViews();
        }

        private void Subscribe()
        {
            if (facilityManager != null)
                facilityManager.OnUpgradeChanged += HandleUpgradeChanged;

            InventoryStateSO inventory = GetInventory();
            if (inventory != null)
                inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        private void Unsubscribe()
        {
            if (facilityManager != null)
                facilityManager.OnUpgradeChanged -= HandleUpgradeChanged;

            InventoryStateSO inventory = GetInventory();
            if (inventory != null)
                inventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        private void HandleUpgradeChanged(FacilityUpgradeSO upgrade, int level)
        {
            Refresh();
        }

        private void HandleInventoryChanged()
        {
            Refresh();
        }

        public void Refresh()
        {
            ClearViews();

            FacilityUpgradeDatabaseSO database = facilityManager != null ? facilityManager.UpgradeDatabase : null;
            IReadOnlyList<FacilityUpgradeSO> upgrades = database != null ? database.Upgrades : null;

            if (contentRoot == null || upgradeViewPrefab == null || upgrades == null || upgrades.Count == 0)
            {
                selectedUpgrade = null;
                if (emptyStateObject != null)
                    emptyStateObject.SetActive(true);

                RefreshDetailPanel();
                RefreshMaterialSummary();
                return;
            }

            bool selectedStillExists = false;
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null && upgrades[i] == selectedUpgrade)
                {
                    selectedStillExists = true;
                    break;
                }
            }

            if (!selectedStillExists)
                selectedUpgrade = FindFirstUpgrade(upgrades);

            bool hasVisibleUpgrade = false;
            for (int i = 0; i < upgrades.Count; i++)
            {
                FacilityUpgradeSO upgrade = upgrades[i];
                if (upgrade == null)
                    continue;

                hasVisibleUpgrade = true;
                FacilityUpgradeView view = Instantiate(upgradeViewPrefab, contentRoot);
                spawnedViews.Add(view);

                int level = facilityManager != null ? facilityManager.GetLevel(upgrade) : 0;
                int maxLevel = facilityManager != null ? facilityManager.GetMaxLevel(upgrade) : upgrade.MaxLevel;
                ItemDefinitionSO requiredItem = facilityManager != null ? facilityManager.GetRequiredItem(upgrade) : upgrade.GetRequiredItem(level);
                int requiredQuantity = facilityManager != null ? facilityManager.GetRequiredItemQuantity(upgrade) : upgrade.GetRequiredItemQuantity(level);
                int currentQuantity = facilityManager != null ? facilityManager.GetCurrentRequiredItemQuantity(upgrade) : GetItemQuantity(requiredItem);
                bool canUpgrade = facilityManager != null && facilityManager.CanUpgrade(upgrade);
                FacilityUpgradeSO upgradeForClick = upgrade;

                view.Bind(
                    upgrade,
                    level,
                    maxLevel,
                    requiredItem != null ? requiredItem.Icon : null,
                    requiredQuantity,
                    currentQuantity,
                    upgrade == selectedUpgrade,
                    () => SelectUpgrade(upgradeForClick),
                    canUpgrade,
                    () => TryUpgrade(upgradeForClick));
            }

            if (emptyStateObject != null)
                emptyStateObject.SetActive(!hasVisibleUpgrade);

            RefreshDetailPanel();
            RefreshMaterialSummary();
        }

        private FacilityUpgradeSO FindFirstUpgrade(IReadOnlyList<FacilityUpgradeSO> upgrades)
        {
            if (upgrades == null)
                return null;

            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null)
                    return upgrades[i];
            }

            return null;
        }

        private void SelectUpgrade(FacilityUpgradeSO upgrade)
        {
            selectedUpgrade = upgrade;
            Refresh();
        }

        private void TryUpgrade(FacilityUpgradeSO upgrade)
        {
            if (facilityManager == null || upgrade == null)
                return;

            facilityManager.TryUpgrade(upgrade);
        }

        private void RefreshMaterialSummary()
        {
            ItemDefinitionSO item = selectedUpgrade != null && facilityManager != null ? facilityManager.GetRequiredItem(selectedUpgrade) : null;
            int requiredQuantity = selectedUpgrade != null && facilityManager != null ? facilityManager.GetRequiredItemQuantity(selectedUpgrade) : 0;
            int currentQuantity = selectedUpgrade != null && facilityManager != null ? facilityManager.GetCurrentRequiredItemQuantity(selectedUpgrade) : 0;

            if (materialText != null)
                materialText.text = item != null && requiredQuantity > 0 ? $"{requiredQuantity}/{currentQuantity}" : string.Empty;

            if (materialIconImage != null)
            {
                materialIconImage.sprite = item != null ? item.Icon : null;
                materialIconImage.enabled = item != null && item.Icon != null;
            }
        }

        private InventoryStateSO GetInventory()
        {
            if (inventoryState != null)
                return inventoryState;

            return facilityManager != null ? facilityManager.InventoryState : null;
        }

        private int GetItemQuantity(ItemDefinitionSO item)
        {
            InventoryStateSO inventory = GetInventory();
            return item != null && inventory != null ? inventory.GetQuantity(item) : 0;
        }

        private void RefreshDetailPanel()
        {
            if (selectedIconImage != null)
                selectedIconImage.sprite = selectedUpgrade != null ? selectedUpgrade.Icon : null;

            if (selectedNameText != null)
                selectedNameText.text = selectedUpgrade != null ? selectedUpgrade.DisplayName : string.Empty;

            if (selectedDescriptionText != null)
                selectedDescriptionText.text = selectedUpgrade != null ? selectedUpgrade.Description : string.Empty;

            int level = selectedUpgrade != null && facilityManager != null ? facilityManager.GetLevel(selectedUpgrade) : 0;
            int maxLevel = selectedUpgrade != null && facilityManager != null ? facilityManager.GetMaxLevel(selectedUpgrade) : 0;
            ItemDefinitionSO requiredItem = selectedUpgrade != null && facilityManager != null ? facilityManager.GetRequiredItem(selectedUpgrade) : null;
            int requiredQuantity = selectedUpgrade != null && facilityManager != null ? facilityManager.GetRequiredItemQuantity(selectedUpgrade) : 0;
            int currentQuantity = selectedUpgrade != null && facilityManager != null ? facilityManager.GetCurrentRequiredItemQuantity(selectedUpgrade) : 0;
            bool canUpgrade = selectedUpgrade != null && facilityManager != null && facilityManager.CanUpgrade(selectedUpgrade);
            bool isMaxLevel = selectedUpgrade != null && maxLevel > 0 && level >= maxLevel;

            if (selectedLevelText != null)
                selectedLevelText.text = selectedUpgrade != null ? $"Lv {level}/{maxLevel}" : string.Empty;

            if (selectedMaterialText != null)
                selectedMaterialText.text = selectedUpgrade != null && !isMaxLevel && requiredQuantity > 0 ? $"{requiredQuantity}/{currentQuantity}" : string.Empty;

            if (selectedCostIconImage != null)
            {
                selectedCostIconImage.sprite = requiredItem != null ? requiredItem.Icon : null;
                selectedCostIconImage.enabled = selectedUpgrade != null && !isMaxLevel && requiredItem != null && requiredItem.Icon != null;
            }

            if (selectedCurrentBonusText != null)
                selectedCurrentBonusText.text = selectedUpgrade != null ? FormatBonus(selectedUpgrade.StatType, selectedUpgrade.GetBonusValue(level)) : string.Empty;

            if (selectedNextBonusText != null)
                selectedNextBonusText.text = selectedUpgrade != null && !isMaxLevel ? FormatBonus(selectedUpgrade.StatType, selectedUpgrade.GetBonusValue(level + 1)) : string.Empty;

            if (upgradeSelectedButton != null)
            {
                upgradeSelectedButton.onClick.RemoveAllListeners();
                upgradeSelectedButton.interactable = canUpgrade;
                if (selectedUpgrade != null)
                    upgradeSelectedButton.onClick.AddListener(() => TryUpgrade(selectedUpgrade));
            }

            if (upgradeButtonLabelText != null)
            {
                if (selectedUpgrade == null)
                    upgradeButtonLabelText.text = string.Empty;
                else
                    upgradeButtonLabelText.text = isMaxLevel ? "Max" : "Upgrade";
            }
        }

        private static string FormatBonus(FacilityStatType statType, float value)
        {
            switch (statType)
            {
                case FacilityStatType.ToolCapacity:
                    return $"+{Mathf.RoundToInt(value)}";
                default:
                    return $"+{value:0.##}";
            }
        }

        private void ClearViews()
        {
            for (int i = 0; i < spawnedViews.Count; i++)
            {
                if (spawnedViews[i] != null)
                    Destroy(spawnedViews[i].gameObject);
            }

            spawnedViews.Clear();
        }
    }
}
