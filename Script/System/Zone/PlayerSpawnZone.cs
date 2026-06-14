using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Zone
{
    [DisallowMultipleComponent]
    public class PlayerSpawnZone : ZoneBase
    {
        [SerializeField] private Transform spawnPoint;

        public override ZoneType Type => ZoneType.Spawn;
        protected override ZoneHitTarget HitTarget => ZoneHitTarget.Player;

        protected override void OnPlayerEnterZone(PlayerController player)
        {
            if (player == null)
                return;

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            player.SetRespawnPosition(position);
        }
    }
}
