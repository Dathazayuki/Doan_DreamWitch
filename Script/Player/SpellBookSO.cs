using UnityEngine;

namespace DreamKnight.Player
{
    [CreateAssetMenu(fileName = "SpellBook_New", menuName = "DreamKnight/Items/Spell Book")]
    public class SpellBookSO : ScriptableObject
    {
        [Header("Identity")]
        public string bookId;
        public string displayName;
        public Sprite icon;
        [TextArea(3, 10)]
        public string description;

        [Header("Stats Modifiers (Human Form Only)")]
        [Tooltip("Max Health increase (flat)")]
        public float healthBonus = 0f;
        [Tooltip("Max Health multiplier (e.g. 0.15 for +15%)")]
        public float healthPercentBonus = 0f;

        [Tooltip("Max Mana (MP) increase (flat)")]
        public float manaBonus = 0f;

        [Tooltip("Basic attack damage multiplier (e.g. 1.25 for x125% basic damage)")]
        public float basicAttackDamageMultiplier = 1f;

        [Tooltip("Mana (MP) restored per basic attack hit")]
        public float manaRegenPerHitBonus = 0f;

        [Tooltip("Critical Strike chance (0 to 1)")]
        public float critChance = 0f;
        [Tooltip("Critical Strike damage bonus multiplier (e.g. 0.5 for +50% critical damage)")]
        public float critDamageMultiplierBonus = 0f;

        [Tooltip("Skill/Spell damage multiplier (e.g. 1.25 for x125% skill damage)")]
        public float skillDamageMultiplier = 1f;

        [Tooltip("Skill/Spell cooldown reduction (e.g. 0.15 for 15% reduction)")]
        public float skillCooldownReduction = 0f;

        [Header("New Buffs")]
        [Tooltip("Health (HP) regenerated per second")]
        public float healthRegenBonus = 0f;

        [Tooltip("Mana (MP) regenerated per second")]
        public float manaRegenBonus = 0f;

        [Tooltip("Movement speed multiplier bonus (e.g. 0.15 for +15%)")]
        public float moveSpeedPercentBonus = 0f;

        [Tooltip("Flat damage reduction (defense)")]
        public float defenseBonus = 0f;
    }
}
