using UnityEngine;

namespace DreamKnight.Systems.Scene
{
    [DisallowMultipleComponent]
    public class SceneDoorSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string doorId;

        public string DoorId => string.IsNullOrWhiteSpace(doorId) ? gameObject.name : doorId;
    }
}
