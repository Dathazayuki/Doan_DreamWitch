using UnityEngine;
using UnityEngine.Serialization;
using DreamKnight.Player;

namespace DreamKnight.Systems.Inventory
{
    [CreateAssetMenu(fileName = "HealingPotion", menuName = "DreamKnight/Inventory/Items/Healing Potion")]
    public class HealingPotionItemSO : ItemDefinitionSO
    {
        public enum PotionEffectType
        {
            InstantHealth,
            HealthOverTime,
            InstantMana
        }

        [Header("Effect")]
        [SerializeField] private PotionEffectType effectType = PotionEffectType.InstantHealth;

        [Header("Instant Health")]
        [SerializeField] private float healthRestore = 25f;

        [Header("Health Over Time")]
        [FormerlySerializedAs("healthRestorePerTick")]
        [SerializeField] private float healthRegenBonus = 10f;
        [FormerlySerializedAs("healDuration")]
        [SerializeField] private float healthRegenDuration = 5f;

        [Header("Mana")]
        [SerializeField] private float manaRestore = 25f;

        [Header("Stamina")]
        [SerializeField] private float staminaRestore = 0f;

        public PotionEffectType EffectType => effectType;
        public float HealthRestore => healthRestore;
        public float HealthRegenBonus => healthRegenBonus;
        public float HealthRegenDuration => healthRegenDuration;
        public float ManaRestore => manaRestore;
        public float StaminaRestore => staminaRestore;

        public override bool Use(ItemUseContext context)
        {
            PlayerStats playerStats = context != null ? context.PlayerStats : null;
            if (playerStats == null)
                return false;

            switch (effectType)
            {
                case PotionEffectType.InstantHealth:
                    if (healthRestore > 0f)
                        playerStats.Heal(healthRestore);
                    break;

                case PotionEffectType.HealthOverTime:
                    playerStats.ApplyTemporaryHealthRegen(healthRegenBonus, healthRegenDuration);
                    break;

                case PotionEffectType.InstantMana:
                    if (manaRestore > 0f)
                        playerStats.RestoreMana(manaRestore);
                    break;
            }

            if (staminaRestore > 0f)
                playerStats.RestoreStamina(staminaRestore);

            return true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            healthRestore = Mathf.Max(0f, healthRestore);
            healthRegenBonus = Mathf.Max(0f, healthRegenBonus);
            healthRegenDuration = Mathf.Max(0f, healthRegenDuration);
            manaRestore = Mathf.Max(0f, manaRestore);
            staminaRestore = Mathf.Max(0f, staminaRestore);
        }
#endif
    }
}
