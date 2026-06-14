using System.Collections.Generic;
using DreamKnight.Interfaces;
using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Culling
{
    /// <summary>
    /// Gắn lên Room GameObject (cần có Collider2D IsTrigger = true).
    /// Phát hiện Player bước vào/ra để thông báo CullingManager.
    /// Quản lý việc wake/sleep tất cả ICullable members trong Room.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class RoomController : MonoBehaviour
    {
        // ─── Inspector ─────────────────────────────────────────────────────────
        [Header("Room Identity")]
        [SerializeField] private string roomId;

        [Header("Adjacent Rooms (luôn active khi room này active)")]
        [Tooltip("Các Room kề – wake cùng khi Room này được active, tránh pop-in khi Player di chuyển giữa rooms.")]
        [SerializeField] private RoomController[] adjacentRooms;

        [Header("Renderer Options")]
        [Tooltip("Tắt TilemapRenderer khi Room sleep (tiết kiệm GPU). Bật lại khi Room active.")]
        [SerializeField] private bool cullTilemapRenderer = true;

        // ─── Runtime ───────────────────────────────────────────────────────────
        private readonly List<ICullable> members = new List<ICullable>();
        private UnityEngine.Tilemaps.TilemapRenderer[] tilemapRenderers;
        private bool isActive = true; // Mặc định active để tránh flash ở frame đầu

        public bool IsActive => isActive;
        public RoomController[] AdjacentRooms => adjacentRooms;
        public string RoomId => string.IsNullOrEmpty(roomId) ? gameObject.name : roomId;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            // Đảm bảo collider là trigger
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            // Cache tilemaps trong room này
            tilemapRenderers = GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(true);

            if (string.IsNullOrEmpty(roomId))
                roomId = gameObject.name;

            CullingManager.Instance?.RegisterRoom(this);
        }

        private void OnDestroy()
        {
            CullingManager.Instance?.UnregisterRoom(this);
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

        // ─── Trigger Detection ─────────────────────────────────────────────────
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerBodyCollider(other)) return;
            CullingManager.Instance?.SetActiveRoom(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerBodyCollider(other)) return;
            CullingManager.Instance?.OnPlayerExitRoom(this);
        }

        private bool IsPlayerBodyCollider(Collider2D col)
        {
            PlayerController player = col.GetComponentInParent<PlayerController>();
            return player != null && player.IsBodyCollider(col);
        }

        // ─── Room Active / Sleep ───────────────────────────────────────────────

        /// <summary>
        /// Bật (active=true) hoặc tắt (active=false) tất cả member và TilemapRenderer trong Room.
        /// Không bao giờ SetActive(false) bản thân GameObject – cần trigger để detect Player.
        /// </summary>
        public void SetRoomActive(bool active)
        {
            if (isActive == active) return;
            isActive = active;

            // Wake / Sleep tất cả members
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

            // Bật/tắt TilemapRenderer
            if (cullTilemapRenderer && tilemapRenderers != null)
            {
                for (int i = 0; i < tilemapRenderers.Length; i++)
                {
                    if (tilemapRenderers[i] != null)
                        tilemapRenderers[i].enabled = active;
                }
            }
        }

        // ─── Member Registration ───────────────────────────────────────────────
        public void RegisterMember(ICullable member)
        {
            if (member != null && !members.Contains(member))
                members.Add(member);
        }

        public void UnregisterMember(ICullable member)
        {
            members.Remove(member);
        }

        // ─── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (adjacentRooms == null) return;
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
