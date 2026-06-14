using System;
using System.Collections.Generic;
using DreamKnight.Player;
using DreamKnight.Systems.Currency;
using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.SaveLoad;
using UnityEngine;

namespace DreamKnight.Systems.Facility
{
    [Serializable]
    public struct FacilityAppliedStats
    {
        public float maxHealthBonus;
        public float maxManaBonus;
        public float basicAttackDamageBonus;
        public int toolCapacityBonus;
    }

    public class FacilityManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private FacilityUpgradeDatabaseSO upgradeDatabase;
        [SerializeField] private FacilityProgressSO progress;
        [SerializeField] private CurrencyWalletSO currencyWallet;

        [Header("Apply Targets")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private ToolEquipSO toolEquip;
        [SerializeField] private bool autoFindPlayerTargets = true;

        private FacilityAppliedStats currentStats;

        public event Action<FacilityAppliedStats> OnAppliedStatsChanged;
        public event Action<FacilityUpgradeSO, int> OnUpgradeChanged;

        public FacilityUpgradeDatabaseSO UpgradeDatabase => upgradeDatabase;
        public FacilityProgressSO Progress => progress;
        public CurrencyWalletSO CurrencyWallet => currencyWallet;
        public ToolEquipSO ToolEquip => toolEquip;
        public FacilityAppliedStats CurrentStats => currentStats;

        private void OnEnable()
        {
            if (progress != null)
                progress.OnUpgradeLevelChanged += HandleProgressChanged;

            ApplyUpgrades();
        }

        private void OnDisable()
        {
            if (progress != null)
                progress.OnUpgradeLevelChanged -= HandleProgressChanged;
        }

        public int GetLevel(FacilityUpgradeSO upgrade)
        {
            if (upgrade == null || progress == null)
                return 0;

            return progress.GetLevel(upgrade.UpgradeId);
        }

        public int GetMaxLevel(FacilityUpgradeSO upgrade)
        {
            return upgrade != null ? Mathf.Max(0, upgrade.MaxLevel) : 0;
        }

        public int GetNextPrice(FacilityUpgradeSO upgrade)
        {
            if (upgrade == null)
                return 0;

            return upgrade.GetNextUpgradePrice(GetLevel(upgrade));
        }

        public bool CanUpgrade(FacilityUpgradeSO upgrade)
        {
            if (upgrade == null || progress == null || currencyWallet == null)
                return false;

            int level = GetLevel(upgrade);
            int maxLevel = GetMaxLevel(upgrade);
            if (maxLevel <= 0 || level >= maxLevel)
                return false;

            int price = GetNextPrice(upgrade);
            return currencyWallet.Balance >= price;
        }

        public bool TryUpgrade(FacilityUpgradeSO upgrade)
        {
            if (!CanUpgrade(upgrade))
                return false;

            int price = GetNextPrice(upgrade);
            if (price > 0 && !currencyWallet.Spend(price))
                return false;

            int nextLevel = Mathf.Clamp(GetLevel(upgrade) + 1, 0, GetMaxLevel(upgrade));
            progress.SetLevel(upgrade.UpgradeId, nextLevel);
            GameAutoSave.Request("facility_upgrade");
            return true;
        }

        public float GetCurrentBonus(FacilityUpgradeSO upgrade)
        {
            if (upgrade == null)
                return 0f;

            return upgrade.GetBonusValue(GetLevel(upgrade));
        }

        public FacilityProgressSaveData CaptureSaveData()
        {
            return progress != null ? progress.CaptureSaveData() : new FacilityProgressSaveData();
        }

        public void LoadFromSaveData(FacilityProgressSaveData saveData)
        {
            if (progress == null)
                return;

            progress.LoadFromSaveData(saveData);
        }

        public FacilityAppliedStats CalculateAppliedStats()
        {
            FacilityAppliedStats stats = new FacilityAppliedStats();
            IReadOnlyList<FacilityUpgradeSO> upgrades = upgradeDatabase != null ? upgradeDatabase.Upgrades : null;
            if (upgrades == null)
                return stats;

            for (int i = 0; i < upgrades.Count; i++)
            {
                FacilityUpgradeSO upgrade = upgrades[i];
                if (upgrade == null)
                    continue;

                float value = GetCurrentBonus(upgrade);
                switch (upgrade.StatType)
                {
                    case FacilityStatType.MaxHealth:
                        stats.maxHealthBonus += value;
                        break;
                    case FacilityStatType.MaxMana:
                        stats.maxManaBonus += value;
                        break;
                    case FacilityStatType.BasicAttackDamage:
                        stats.basicAttackDamageBonus += value;
                        break;
                    case FacilityStatType.ToolCapacity:
                        stats.toolCapacityBonus += Mathf.RoundToInt(value);
                        break;
                }
            }

            return stats;
        }

        public void ApplyUpgrades()
        {
            currentStats = CalculateAppliedStats();
            ApplyStatsToTargets(currentStats);
            OnAppliedStatsChanged?.Invoke(currentStats);
        }

        private void ApplyStatsToTargets(FacilityAppliedStats stats)
        {
            ResolveApplyTargets();

            if (playerStats != null)
                playerStats.SetFacilityMaxStatBonuses(stats.maxHealthBonus, stats.maxManaBonus);

            if (playerCombat != null)
                playerCombat.SetFacilityBasicAttackDamageBonus(stats.basicAttackDamageBonus);

            if (toolEquip != null)
                toolEquip.SetFacilitySlotBonus(stats.toolCapacityBonus);
        }

        private void ResolveApplyTargets()
        {
            if (!autoFindPlayerTargets)
                return;

            if (playerStats == null)
                playerStats = FindAnyObjectByType<PlayerStats>();

            if (playerCombat == null)
                playerCombat = FindAnyObjectByType<PlayerCombat>();
        }

        private void HandleProgressChanged(string upgradeId, int level)
        {
            FacilityUpgradeSO upgrade = upgradeDatabase != null ? upgradeDatabase.FindById(upgradeId) : null;
            if (upgrade != null)
                OnUpgradeChanged?.Invoke(upgrade, level);

            ApplyUpgrades();
        }
    }
}
