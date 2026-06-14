using UnityEngine;
using DreamKnight.Player;

namespace DreamKnight.Systems.Map
{
    [RequireComponent(typeof(Collider2D))]
    public class MapSubArea : MonoBehaviour
    {
        [SerializeField] private string areaId;
        [SerializeField] private GameObject mapVisualObject;

        public string AreaId
        {
            get
            {
                if (string.IsNullOrEmpty(areaId))
                {
                    areaId = gameObject.name;
                }
                return areaId;
            }
        }

        public GameObject MapVisualObject => mapVisualObject;

        private void Awake()
        {
            if (string.IsNullOrEmpty(areaId))
            {
                areaId = gameObject.name;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckAndUnlock(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckAndUnlock(other);
        }

        private void CheckAndUnlock(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.IsBodyCollider(other))
            {
                if (MapRenderTextureController.Instance != null)
                {
                    MapRenderTextureController.Instance.UnlockArea(AreaId);
                }
            }
        }

        public bool ContainsPoint(Vector2 point)
        {
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (var col in colliders)
            {
                if (col != null && col.OverlapPoint(point))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
