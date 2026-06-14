using System;
using UnityEngine;
using DreamKnight.Systems.Currency;
using DreamKnight.Systems.SaveLoad;

namespace DreamKnight.Systems.Skill
{
    public class SpellManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SpellDatabaseSO spellDatabase;
        [SerializeField] private SkillProgressSO skillProgress;
        [SerializeField] private CurrencyWalletSO currencyWallet;

        public event Action<string, int> OnSpellProgressChanged;

        public SpellDatabaseSO SpellDatabase => spellDatabase;
        public SkillProgressSO SkillProgress => skillProgress;

        private void OnEnable()
        {
            if (skillProgress != null)
                skillProgress.OnSpellLevelChanged += HandleSpellLevelChanged;
        }

        private void OnDisable()
        {
            if (skillProgress != null)
                skillProgress.OnSpellLevelChanged -= HandleSpellLevelChanged;
        }

        private void HandleSpellLevelChanged(string spellId, int level)
        {
            OnSpellProgressChanged?.Invoke(spellId, level);
        }

        public int GetLevel(SpellData spell)
        {
            if (spell == null || skillProgress == null)
                return 0;

            return skillProgress.GetLevel(spell.GetStableId());
        }

        public bool IsUnlocked(SpellData spell)
        {
            return GetLevel(spell) > 0;
        }

        public int GetMaxLevel(SpellData spell)
        {
            return spell != null ? Mathf.Max(0, spell.MaxLevel) : 0;
        }

        public int GetNextPrice(SpellData spell)
        {
            if (spell == null)
                return 0;

            int level = GetLevel(spell);
            return spell.GetNextUpgradePrice(level);
        }

        public bool CanUpgrade(SpellData spell)
        {
            if (spell == null)
                return false;

            int level = GetLevel(spell);
            int maxLevel = GetMaxLevel(spell);
            if (level >= maxLevel)
                return false;

            int price = GetNextPrice(spell);
            return currencyWallet != null && currencyWallet.Balance >= price;
        }

        public bool TryUnlockOrUpgrade(SpellData spell)
        {
            if (spell == null || skillProgress == null || currencyWallet == null)
                return false;

            int level = GetLevel(spell);
            int maxLevel = GetMaxLevel(spell);
            if (level >= maxLevel && maxLevel > 0)
                return false;

            int price = GetNextPrice(spell);
            if (price > 0 && !currencyWallet.Spend(price))
                return false;

            int nextLevel = Mathf.Clamp(level + 1, 0, maxLevel);
            if (maxLevel == 0)
                nextLevel = level + 1;

            skillProgress.SetLevel(spell.GetStableId(), nextLevel);
            GameAutoSave.Request("spell_upgrade");
            return true;
        }
    }
}
