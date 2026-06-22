using System.Collections.Generic;
using DreamKnight.Systems.Inventory;
using UnityEngine;

namespace DreamKnight.Systems.Facility
{
    public enum FacilityStatType
    {
        MaxHealth,
        MaxMana,
        BasicAttackDamage,
        ToolCapacity
    }

    [System.Serializable]
    public class FacilityUpgradeLevelData
    {
        [Header("Cost")]
        public ItemDefinitionSO requiredItem;
        [Min(1)] public int requiredItemQuantity = 1;
        [HideInInspector] public int price;
        [Header("Bonus")]
        [Tooltip("Tong bonus cua stat tai level nay. Vi du Lv2 HP = 40 nghia la tong +40 HP.")]
        public float bonusValue;
    }

    [CreateAssetMenu(fileName = "FacilityUpgrade", menuName = "DreamKnight/Facility/Upgrade")]
    public class FacilityUpgradeSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string upgradeId;
        [SerializeField] private string displayName;
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        [Header("Stat")]
        [SerializeField] private FacilityStatType statType;
        [SerializeField] private List<FacilityUpgradeLevelData> levels = new List<FacilityUpgradeLevelData>();

        public string UpgradeId => string.IsNullOrWhiteSpace(upgradeId) ? name : upgradeId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public FacilityStatType StatType => statType;
        public int MaxLevel => levels != null ? levels.Count : 0;
        public IReadOnlyList<FacilityUpgradeLevelData> Levels => levels;

        public int GetNextUpgradePrice(int currentLevel)
        {
            if (levels == null || currentLevel < 0 || currentLevel >= levels.Count)
                return 0;

            FacilityUpgradeLevelData nextLevel = levels[currentLevel];
            return nextLevel != null ? Mathf.Max(0, nextLevel.price) : 0;
        }

        public ItemDefinitionSO GetRequiredItem(int currentLevel)
        {
            FacilityUpgradeLevelData nextLevel = GetLevelDataForNextUpgrade(currentLevel);
            return nextLevel != null ? nextLevel.requiredItem : null;
        }

        public int GetRequiredItemQuantity(int currentLevel)
        {
            FacilityUpgradeLevelData nextLevel = GetLevelDataForNextUpgrade(currentLevel);
            return nextLevel != null && nextLevel.requiredItem != null ? Mathf.Max(1, nextLevel.requiredItemQuantity) : 0;
        }

        public float GetBonusValue(int level)
        {
            if (levels == null || levels.Count == 0 || level <= 0)
                return 0f;

            int index = Mathf.Clamp(level - 1, 0, levels.Count - 1);
            FacilityUpgradeLevelData levelData = levels[index];
            return levelData != null ? levelData.bonusValue : 0f;
        }

        private FacilityUpgradeLevelData GetLevelDataForNextUpgrade(int currentLevel)
        {
            if (levels == null || currentLevel < 0 || currentLevel >= levels.Count)
                return null;

            return levels[currentLevel];
        }
    }
}
