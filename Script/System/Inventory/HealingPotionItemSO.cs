using UnityEngine;
using DreamKnight.Player;

namespace DreamKnight.Systems.Inventory
{
    [CreateAssetMenu(fileName = "HealingPotion", menuName = "DreamKnight/Inventory/Items/Healing Potion")]
    public class HealingPotionItemSO : ItemDefinitionSO
    {
        [Header("Healing")]
        [SerializeField] private float healthRestore = 25f;
        [SerializeField] private float staminaRestore = 0f;

        public float HealthRestore => healthRestore;
        public float StaminaRestore => staminaRestore;

        public override bool Use(ItemUseContext context)
        {
            PlayerStats playerStats = context != null ? context.PlayerStats : null;
            if (playerStats == null)
                return false;

            if (healthRestore > 0f)
                playerStats.Heal(healthRestore);

            if (staminaRestore > 0f)
                playerStats.RestoreStamina(staminaRestore);

            return true;
        }
    }
}
