using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    public enum SpellCastType
    {
        PrefabCast = 0,
        ForwardProjectile = 1,
        BigSword = 2,
        Shield = 3
    }

    [CreateAssetMenu(fileName = "SpellData", menuName = "DreamKnight/Skill/Spell Data")]
    public class SpellData : ScriptableObject
    {
        [Header("Identity")]
        public string spellId;
        public string spellName;
        public Sprite icon;

        [Header("Prefab")]
        public GameObject prefab;

        [Header("Cast Type")]
        public SpellCastType castType = SpellCastType.PrefabCast;

        [Header("Forward Projectile")]
        public float projectileSpeed = 12f;
        public float projectileLifetime = 2f;
        public Vector2 projectileSpawnOffset = new Vector2(0.6f, 0.2f);
        public int projectileMaxPoolSize = 24;

        [Header("Big Sword")]
        public float bigSwordSpeed = 10f;
        public float bigSwordLifetime = 1f;
        public Vector2 bigSwordSpawnOffset = new Vector2(0.8f, 0.2f);
        public int bigSwordMaxPoolSize = 8;

        [Header("Shield")]
        public GameObject shieldVisualPrefab;

        [Header("Shop")]
        public int unlockPrice;

        [TextArea(2, 6)]
        public string description;

        public List<SpellLevelData> levels = new List<SpellLevelData>();

        public int MaxLevel => levels != null ? levels.Count : 0;

        public bool HasLevelData => levels != null && levels.Count > 0;

        public string GetStableId()
        {
            return string.IsNullOrWhiteSpace(spellId) ? name : spellId;
        }

        public bool TryGetLevelData(int unlockedLevel, out SpellLevelData levelData)
        {
            levelData = null;

            if (!HasLevelData)
                return false;

            int index = Mathf.Clamp(unlockedLevel - 1, 0, levels.Count - 1);
            if (unlockedLevel <= 0 || index < 0 || index >= levels.Count)
                return false;

            levelData = levels[index];
            return levelData != null;
        }

        public float GetManaCost(int unlockedLevel)
        {
            return TryGetLevelData(unlockedLevel, out SpellLevelData levelData) ? Mathf.Max(0f, levelData.manaCost) : 0f;
        }

        public float GetCooldown(int unlockedLevel)
        {
            return TryGetLevelData(unlockedLevel, out SpellLevelData levelData) ? Mathf.Max(0f, levelData.cooldown) : 0f;
        }

        public float GetDamage(int unlockedLevel)
        {
            return TryGetLevelData(unlockedLevel, out SpellLevelData levelData) ? Mathf.Max(0f, levelData.damage) : 0f;
        }

        public int GetNextUpgradePrice(int currentLevel)
        {
            if (!HasLevelData)
                return 0;

            if (currentLevel <= 0)
                return Mathf.Max(0, unlockPrice);

            if (currentLevel >= MaxLevel)
                return 0;

            SpellLevelData nextLevelData = levels[currentLevel];
            return nextLevelData != null ? Mathf.Max(0, nextLevelData.upgradePrice) : 0;
        }
    }

    [System.Serializable]
    public class SpellLevelData
    {
        public int upgradePrice;
        public float damage;
        public float cooldown;
        public float manaCost;
        public int shieldBlockCount;
    }
}
