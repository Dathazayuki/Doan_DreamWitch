using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Zone
{
    /// <summary>
    /// Acid Water Zone: Deals periodic damage to the Player while they are inside this zone.
    /// Can be combined with InteractiveWater2D on the same GameObject for interactive acid pools.
    /// </summary>
    [DisallowMultipleComponent]
    public class AcidWaterZone : ZoneBase
    {
        [Header("Damage Settings")]
        [SerializeField] private float damageAmount = 1f;
        [SerializeField] private float damageInterval = 1f;

        [Header("Movement Settings")]
        [SerializeField] private bool applySlowEffect = true;
        [SerializeField, Range(0.1f, 1f)] private float speedMultiplier = 0.5f;

        private float nextDamageTime;

        public override ZoneType Type => ZoneType.Damage;
        protected override ZoneHitTarget HitTarget => ZoneHitTarget.Player;

        protected override void OnPlayerEnterZone(PlayerController player)
        {
            if (player == null || !player.IsAlive)
                return;

            // Gây sát thương lần đầu ngay khi chạm vào axit
            player.TakeDamage(damageAmount, gameObject);
            nextDamageTime = Time.time + damageInterval;
        }

        protected override void OnPlayerStayZone(PlayerController player)
        {
            if (player == null || !player.IsAlive)
                return;

            if (Time.time >= nextDamageTime)
            {
                player.TakeDamage(damageAmount, gameObject);
                nextDamageTime = Time.time + damageInterval;
            }

            // Làm chậm tốc độ của Player
            if (applySlowEffect && player.Movement != null)
            {
                Vector2 v = player.Movement.Velocity;
                float clampedMultiplier = Mathf.Clamp(speedMultiplier, 0.1f, 1f);
                player.Movement.SetVelocity(new Vector2(v.x * clampedMultiplier, v.y));
            }
        }
    }
}
