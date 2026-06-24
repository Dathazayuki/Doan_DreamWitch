using System.Collections.Generic;
using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Zone
{
    [DisallowMultipleComponent]
    public class PlayerTrapZone : ZoneBase
    {
        [Header("Trap")]
        [SerializeField] private float hitDamage = 15f;
        [SerializeField] private bool respawnAfterHit = true;
        [SerializeField] private float retriggerCooldown = 0.35f;

        private readonly Dictionary<int, float> lastTriggerTimeByPlayer = new Dictionary<int, float>();

        public override ZoneType Type => ZoneType.Trap;
        protected override ZoneHitTarget HitTarget => ZoneHitTarget.Player;

        protected override void OnPlayerEnterZone(PlayerController player)
        {
            TriggerTrap(player);
        }

        protected override void OnPlayerStayZone(PlayerController player)
        {
            TriggerTrap(player);
        }

        protected override void OnPlayerExitZone(PlayerController player)
        {
            if (player == null)
                return;

            lastTriggerTimeByPlayer.Remove(player.GetInstanceID());
        }

        private void TriggerTrap(PlayerController player)
        {
            if (player == null || !player.IsAlive || player.IsTrapRespawnInProgress)
                return;

            int id = player.GetInstanceID();
            if (lastTriggerTimeByPlayer.TryGetValue(id, out float lastTime))
            {
                if (Time.time < lastTime + Mathf.Max(0.01f, retriggerCooldown))
                    return;
            }

            lastTriggerTimeByPlayer[id] = Time.time;

            if (respawnAfterHit)
                player.TriggerTrapRespawn(hitDamage, gameObject);
            else
            {
                float damage = Mathf.Max(0f, hitDamage);
                if (damage > 0f)
                    player.TakeDamage(damage, gameObject);
            }
        }
    }
}
