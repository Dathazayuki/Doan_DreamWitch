using System.Collections.Generic;
using DreamKnight.Interfaces;
using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Culling
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class RoomController : MonoBehaviour
    {
        [Header("Room Identity")]
        [SerializeField] private string roomId;

        [Header("Adjacent Rooms")]
        [SerializeField] private RoomController[] adjacentRooms;

        [Header("Renderer Options")]
        [SerializeField] private bool cullTilemapRenderer = true;
        [SerializeField, Min(0f)] private float cameraVisibilityPadding = 1f;
        [SerializeField] private bool ignoreMapOnlyTilemaps = true;
        [SerializeField] private string mapOnlyLayerName = "TileMapOnly";

        private readonly List<ICullable> members = new List<ICullable>();
        private UnityEngine.Tilemaps.TilemapRenderer[] tilemapRenderers;
        private readonly Plane[] cameraFrustumPlanes = new Plane[6];
        private Camera gameplayCamera;
        private bool isActive = true;

        public bool IsActive => isActive;
        public RoomController[] AdjacentRooms => adjacentRooms;
        public string RoomId => string.IsNullOrEmpty(roomId) ? gameObject.name : roomId;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;

            CacheCullableTilemapRenderers();

            if (string.IsNullOrEmpty(roomId))
                roomId = gameObject.name;

            CullingManager.Instance?.RegisterRoom(this);
        }

        private void OnDestroy()
        {
            CullingManager.Current?.UnregisterRoom(this);
        }

        private void LateUpdate()
        {
            if (!cullTilemapRenderer)
                return;

            RefreshTilemapRendererCameraVisibility();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (string.IsNullOrEmpty(roomId))
            {
                roomId = gameObject.name;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerBodyCollider(other))
                return;

            CullingManager.Instance?.SetActiveRoom(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerBodyCollider(other))
                return;

            CullingManager.Instance?.OnPlayerExitRoom(this);
        }

        private bool IsPlayerBodyCollider(Collider2D col)
        {
            PlayerController player = col.GetComponentInParent<PlayerController>();
            return player != null && player.IsBodyCollider(col);
        }

        private void CacheCullableTilemapRenderers()
        {
            UnityEngine.Tilemaps.TilemapRenderer[] renderers =
                GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(true);

            if (!ignoreMapOnlyTilemaps || string.IsNullOrWhiteSpace(mapOnlyLayerName))
            {
                tilemapRenderers = renderers;
                return;
            }

            int mapOnlyLayer = LayerMask.NameToLayer(mapOnlyLayerName);
            if (mapOnlyLayer < 0)
            {
                tilemapRenderers = renderers;
                return;
            }

            List<UnityEngine.Tilemaps.TilemapRenderer> cullableRenderers =
                new List<UnityEngine.Tilemaps.TilemapRenderer>(renderers.Length);

            for (int i = 0; i < renderers.Length; i++)
            {
                UnityEngine.Tilemaps.TilemapRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                if (renderer.gameObject.layer == mapOnlyLayer)
                    continue;

                cullableRenderers.Add(renderer);
            }

            tilemapRenderers = cullableRenderers.ToArray();
        }

        private void RefreshTilemapRendererCameraVisibility()
        {
            if (tilemapRenderers == null || tilemapRenderers.Length == 0)
                return;

            Camera targetCamera = ResolveGameplayCamera();
            if (targetCamera == null)
            {
                SetAllTilemapRenderersEnabled(true);
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(targetCamera, cameraFrustumPlanes);

            for (int i = 0; i < tilemapRenderers.Length; i++)
            {
                UnityEngine.Tilemaps.TilemapRenderer renderer = tilemapRenderers[i];
                if (renderer == null)
                    continue;

                Bounds bounds = renderer.bounds;
                if (cameraVisibilityPadding > 0f)
                    bounds.Expand(cameraVisibilityPadding);

                bool visibleByCamera = GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, bounds);
                if (renderer.enabled != visibleByCamera)
                    renderer.enabled = visibleByCamera;
            }
        }

        private Camera ResolveGameplayCamera()
        {
            if (gameplayCamera != null && gameplayCamera.isActiveAndEnabled)
                return gameplayCamera;

            gameplayCamera = Camera.main;
            return gameplayCamera;
        }

        private void SetAllTilemapRenderersEnabled(bool enabled)
        {
            if (tilemapRenderers == null)
                return;

            for (int i = 0; i < tilemapRenderers.Length; i++)
            {
                UnityEngine.Tilemaps.TilemapRenderer renderer = tilemapRenderers[i];
                if (renderer != null && renderer.enabled != enabled)
                    renderer.enabled = enabled;
            }
        }

        public void SetRoomActive(bool active)
        {
            if (isActive == active)
                return;

            isActive = active;

            for (int i = members.Count - 1; i >= 0; i--)
            {
                ICullable member = members[i];
                if (member == null)
                {
                    members.RemoveAt(i);
                    continue;
                }

                if (active)
                    member.UnCull();
                else
                    member.Cull();
            }
        }

        public void RegisterMember(ICullable member)
        {
            if (member != null && !members.Contains(member))
                members.Add(member);
        }

        public void UnregisterMember(ICullable member)
        {
            members.Remove(member);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (adjacentRooms == null)
                return;

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
            foreach (RoomController adjacent in adjacentRooms)
            {
                if (adjacent != null)
                    Gizmos.DrawLine(transform.position, adjacent.transform.position);
            }
        }
#endif
    }
}
