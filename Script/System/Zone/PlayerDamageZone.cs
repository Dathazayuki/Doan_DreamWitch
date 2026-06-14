using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Zone
{
    [DisallowMultipleComponent]
    public class PlayerDamageZone : ZoneBase
    {
        [SerializeField] private float damagePerSecond = 10f;

        public override ZoneType Type => ZoneType.Damage;
        protected override ZoneHitTarget HitTarget => ZoneHitTarget.Player;

        protected override void OnPlayerStayZone(PlayerController player)
        {
            if (player == null || !player.IsAlive)
                return;

            float damage = Mathf.Max(0f, damagePerSecond) * Time.deltaTime;
            if (damage > 0f)
                player.TakeDamage(damage, gameObject);
        }
    }
}
