using UnityEngine;
using DreamKnight.Player;

namespace DreamKnight.Systems.Inventory
{
    [CreateAssetMenu(fileName = "SpecialPotion", menuName = "DreamKnight/Inventory/Items/Special Potion")]
    public class SpecialPotionItemSO : ItemDefinitionSO
    {
        [Header("Special Effect")]
        [SerializeField] private float healthRestore = 0f;
        [SerializeField] private float staminaRestore = 0f;
        [SerializeField] private float durationSeconds = 10f;
        [SerializeField] private float moveSpeedMultiplier = 1f;

        public float HealthRestore => healthRestore;
        public float StaminaRestore => staminaRestore;
        public float DurationSeconds => durationSeconds;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;

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
