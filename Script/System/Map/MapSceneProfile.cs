using UnityEngine;

namespace DreamKnight.Systems.Map
{
    [DisallowMultipleComponent]
    public class MapSceneProfile : MonoBehaviour
    {
        [Header("Override Ortho Size")]
        [SerializeField] private bool overrideMiniOrthoSize;
        [SerializeField] private float miniOrthoSize = 12f;
        [SerializeField] private bool overrideFullOrthoSize;
        [SerializeField] private float fullOrthoSize = 35f;

        [Header("Override Camera Offset")]
        [SerializeField] private bool overrideCameraOffset;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

        [Header("Clamp Bounds")]
        [SerializeField] private bool overrideClampBounds;
        [SerializeField] private bool disableClampBounds;
        [SerializeField] private Vector2 minBounds = new Vector2(-100f, -100f);
        [SerializeField] private Vector2 maxBounds = new Vector2(100f, 100f);

        public bool OverrideMiniOrthoSize => overrideMiniOrthoSize;
        public float MiniOrthoSize => miniOrthoSize;

        public bool OverrideFullOrthoSize => overrideFullOrthoSize;
        public float FullOrthoSize => fullOrthoSize;

        public bool OverrideCameraOffset => overrideCameraOffset;
        public Vector3 CameraOffset => cameraOffset;

        public bool OverrideClampBounds => overrideClampBounds;
        public bool DisableClampBounds => disableClampBounds;
        public Vector2 MinBounds => minBounds;
        public Vector2 MaxBounds => maxBounds;
    }
}
