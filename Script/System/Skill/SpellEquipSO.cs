using System;
using UnityEngine;
using DreamKnight.Systems.SaveLoad;

namespace DreamKnight.Systems.Skill
{
    [CreateAssetMenu(fileName = "SpellEquip", menuName = "DreamKnight/Skill/Spell Equip")]
    public class SpellEquipSO : ScriptableObject
    {
        // Runtime only: do not serialize the equipped spell into the asset file.
        private SpellData equippedSpell;

        public event Action OnEquipmentChanged;

        public SpellData EquippedSpell => equippedSpell;

        public void Equip(SpellData spell)
        {
            if (spell == null)
                return;

            // Prevent equipping spells that are not unlocked.
            SpellManager manager = FindAnyObjectByType<SpellManager>();
            if (manager != null && !manager.IsUnlocked(spell))
            {
                Debug.LogWarning($"[SpellEquipSO] Attempt to equip locked spell '{spell.GetStableId()}'. Equip prevented.");
                return;
            }

            equippedSpell = spell;
            OnEquipmentChanged?.Invoke();
            GameAutoSave.Request("spell_equip");
        }

        public void Unequip()
        {
            if (equippedSpell == null)
                return;

            equippedSpell = null;
            OnEquipmentChanged?.Invoke();
            GameAutoSave.Request("spell_unequip");
        }

        public bool HasSpell()
        {
            return equippedSpell != null;
        }

        // Save/Load helpers
        public string CaptureSaveData()
        {
            return equippedSpell != null ? equippedSpell.GetStableId() : string.Empty;
        }

        public void LoadFromSaveData(string spellId, SpellDatabaseSO database = null)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                equippedSpell = null;
                OnEquipmentChanged?.Invoke();
                return;
            }

            SpellData found = null;
            if (database != null)
                found = database.FindById(spellId);

            if (found == null)
                found = Resources.Load<SpellData>(spellId);

            if (found != null)
            {
                equippedSpell = found;
                OnEquipmentChanged?.Invoke();
            }
            else
            {
                equippedSpell = null;
                OnEquipmentChanged?.Invoke();
            }
        }
    }
}
