using System.Collections.Generic;
using DreamKnight.Systems.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.Systems.Facility
{
    public class FacilityUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private FacilityManager facilityManager;
        [SerializeField] private CurrencyWalletSO currencyWallet;

        [Header("View")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private FacilityUpgradeView upgradeViewPrefab;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private GameObject emptyStateObject;

        [Header("Detail Panel")]
        [SerializeField] private Image selectedIconImage;
        [SerializeField] private TextMeshProUGUI selectedNameText;
        [SerializeField] private TextMeshProUGUI selectedLevelText;
        [SerializeField] private TextMeshProUGUI selectedPriceText;
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

            CurrencyWalletSO wallet = GetWallet();
            if (wallet != null)
                wallet.OnBalanceChanged += HandleGoldChanged;
        }

        private void Unsubscribe()
        {
            if (facilityManager != null)
                facilityManager.OnUpgradeChanged -= HandleUpgradeChanged;

            CurrencyWalletSO wallet = GetWallet();
            if (wallet != null)
                wallet.OnBalanceChanged -= HandleGoldChanged;
        }

        private void HandleUpgradeChanged(FacilityUpgradeSO upgrade, int level)
        {
            Refresh();
        }

        private void HandleGoldChanged(int balance)
        {
            Refresh();
        }

        public void Refresh()
        {
            ClearViews();
            RefreshGold();

            FacilityUpgradeDatabaseSO database = facilityManager != null ? facilityManager.UpgradeDatabase : null;
            IReadOnlyList<FacilityUpgradeSO> upgrades = database != null ? database.Upgrades : null;

            if (contentRoot == null || upgradeViewPrefab == null || upgrades == null || upgrades.Count == 0)
            {
                selectedUpgrade = null;
                if (emptyStateObject != null)
                    emptyStateObject.SetActive(true);

                RefreshDetailPanel();
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
                int price = facilityManager != null ? facilityManager.GetNextPrice(upgrade) : upgrade.GetNextUpgradePrice(level);
                bool canUpgrade = facilityManager != null && facilityManager.CanUpgrade(upgrade);
                FacilityUpgradeSO upgradeForClick = upgrade;

                view.Bind(upgrade, level, maxLevel, price, upgrade == selectedUpgrade, () => SelectUpgrade(upgradeForClick), canUpgrade, () => TryUpgrade(upgradeForClick));
            }

            if (emptyStateObject != null)
                emptyStateObject.SetActive(!hasVisibleUpgrade);

            RefreshDetailPanel();
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

        private void RefreshGold()
        {
            if (goldText != null)
            {
                CurrencyWalletSO wallet = GetWallet();
                goldText.text = wallet != null ? wallet.Balance.ToString() : "0";
            }
        }

        private CurrencyWalletSO GetWallet()
        {
            if (currencyWallet != null)
                return currencyWallet;

            return facilityManager != null ? facilityManager.CurrencyWallet : null;
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
            int price = selectedUpgrade != null && facilityManager != null ? facilityManager.GetNextPrice(selectedUpgrade) : 0;
            bool canUpgrade = selectedUpgrade != null && facilityManager != null && facilityManager.CanUpgrade(selectedUpgrade);
            bool isMaxLevel = selectedUpgrade != null && maxLevel > 0 && level >= maxLevel;

            if (selectedLevelText != null)
                selectedLevelText.text = selectedUpgrade != null ? $"Lv {level}/{maxLevel}" : string.Empty;

            if (selectedPriceText != null)
                selectedPriceText.text = selectedUpgrade != null && !isMaxLevel ? price.ToString() : string.Empty;

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
