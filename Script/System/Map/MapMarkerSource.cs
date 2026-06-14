using UnityEngine;

namespace DreamKnight.Systems.Map
{
    public enum MapMarkerType
    {
        Player = 1,
        Portal = 2,
        Chest = 3,
        Enemy = 4,
        Custom = 99
    }

    [DisallowMultipleComponent]
    public class MapMarkerSource : MonoBehaviour
    {
        [SerializeField] private MapMarkerType markerType = MapMarkerType.Custom;
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private Vector3 worldOffset;
        [SerializeField] private Vector3 markerScale = Vector3.one;
        [SerializeField] private Color markerColor = Color.white;

        public MapMarkerType MarkerType => markerType;
        public GameObject MarkerPrefab => markerPrefab;
        public Vector3 WorldOffset => worldOffset;
        public Vector3 MarkerScale => markerScale;
        public Color MarkerColor => markerColor;

        public void Configure(MapMarkerType type, GameObject prefab, Vector3 offset, Vector3 scale, Color color)
        {
            markerType = type;
            markerPrefab = prefab;
            worldOffset = offset;
            markerScale = scale;
            markerColor = color;
        }
    }
}
