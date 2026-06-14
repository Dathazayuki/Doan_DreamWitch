using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Zone
{
    [DisallowMultipleComponent]
    public class PlayerSlowZone : ZoneBase
    {
        [SerializeField, Range(0.1f, 1f)] private float speedMultiplier = 0.5f;

        public override ZoneType Type => ZoneType.Slow;
        protected override ZoneHitTarget HitTarget => ZoneHitTarget.Player;

        protected override void OnPlayerStayZone(PlayerController player)
        {
            if (player == null || player.Movement == null)
                return;

            Vector2 v = player.Movement.Velocity;
            float clampedMultiplier = Mathf.Clamp(speedMultiplier, 0.1f, 1f);
            player.Movement.SetVelocity(new Vector2(v.x * clampedMultiplier, v.y));
        }
    }
}
