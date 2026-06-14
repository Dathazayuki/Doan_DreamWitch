using System.Collections;
using System.Collections.Generic;
using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Culling
{
    /// <summary>
    /// Singleton điều phối toàn bộ Distance Culling và Room Culling.
    /// Cập nhật định kỳ (updateInterval giây) thay vì mỗi frame.
    /// Dùng hysteresis: đối tượng chỉ bị Cull khi vượt disableDistance,
    /// chỉ được UnCull khi giảm xuống dưới enableDistance.
    /// </summary>
    [DisallowMultipleComponent]
    public class CullingManager : MonoBehaviour
    {
        // ─── Singleton ─────────────────────────────────────────────────────────
        private static CullingManager instance;
        public static CullingManager Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindAnyObjectByType<CullingManager>();
                if (instance != null) return instance;

                GameObject go = new GameObject("[CullingManager]");
                instance = go.AddComponent<CullingManager>();
                return instance;
            }
        }

        // ─── Inspector ─────────────────────────────────────────────────────────
        [Header("Update Interval")]
        [Tooltip("Số giây giữa mỗi lần tính toán culling. Tăng để tiết kiệm CPU.")]
        [SerializeField] private float updateInterval = 0.2f;

        [Header("Default Hysteresis Distances")]
        [Tooltip("Ngưỡng UnCull (kích hoạt lại) — phải nhỏ hơn disableDistance.")]
        [SerializeField] private float defaultEnableDistance  = 18f;
        [Tooltip("Ngưỡng Cull (vô hiệu hóa) — phải lớn hơn enableDistance.")]
        [SerializeField] private float defaultDisableDistance = 22f;

        // ─── Runtime ───────────────────────────────────────────────────────────
        private readonly List<DistanceCullingTarget> distanceTargets = new List<DistanceCullingTarget>();
        private readonly List<RoomController>        rooms           = new List<RoomController>();
        private RoomController                       activeRoom;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()  => StartCoroutine(CullingLoop());

        private void OnDisable() => StopAllCoroutines();

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // ─── Loop ──────────────────────────────────────────────────────────────
        private IEnumerator CullingLoop()
        {
            var wait = new WaitForSeconds(Mathf.Max(0.05f, updateInterval));
            while (true)
            {
                TickDistanceCulling();
                yield return wait;
            }
        }

        private void TickDistanceCulling()
        {
            Vector3 playerPos = GetPlayerPosition();
            if (playerPos == Vector3.zero && !HasPlayer()) return;

            for (int i = distanceTargets.Count - 1; i >= 0; i--)
            {
                DistanceCullingTarget target = distanceTargets[i];
                if (target == null)
                {
                    distanceTargets.RemoveAt(i);
                    continue;
                }

                // Room sleep ưu tiên cao hơn Distance culling – không override
                if (target.IsRoomSleeping) continue;

                float dist = Vector3.Distance(target.transform.position, playerPos);
                float enableDist  = target.OverrideEnableDistance  > 0f ? target.OverrideEnableDistance  : defaultEnableDistance;
                float disableDist = target.OverrideDisableDistance > 0f ? target.OverrideDisableDistance : defaultDisableDistance;

                // Hysteresis
                if (!target.IsCulled && dist > disableDist)
                {
                    target.Cull();
                }
                else if (target.IsCulled && dist < enableDist)
                {
                    target.UnCull();
                }
                // Trong vùng [enable, disable] → giữ nguyên
            }
        }

        // ─── Room Management ───────────────────────────────────────────────────

        /// <summary>
        /// Được gọi bởi RoomController khi Player bước vào Room.
        /// </summary>
        public void SetActiveRoom(RoomController newRoom)
        {
            if (newRoom == activeRoom) return;

            RoomController prevRoom = activeRoom;
            activeRoom = newRoom;

            // Sleep tất cả rooms trước
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null)
                    rooms[i].SetRoomActive(false);
            }

            // Wake room mới và adjacent rooms của nó
            if (activeRoom != null)
            {
                activeRoom.SetRoomActive(true);
                RoomController[] adjacent = activeRoom.AdjacentRooms;
                if (adjacent != null)
                {
                    for (int i = 0; i < adjacent.Length; i++)
                    {
                        if (adjacent[i] != null)
                            adjacent[i].SetRoomActive(true);
                    }
                }
            }

            // Adjacent rooms của room cũ cũng wake thêm 1 chu kỳ (tránh pop-in)
            if (prevRoom != null && prevRoom != activeRoom)
            {
                RoomController[] prevAdjacent = prevRoom.AdjacentRooms;
                if (prevAdjacent != null)
                {
                    for (int i = 0; i < prevAdjacent.Length; i++)
                    {
                        if (prevAdjacent[i] != null && prevAdjacent[i] != activeRoom)
                            prevAdjacent[i].SetRoomActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Được gọi bởi RoomController khi Player rời Room (không còn ở room nào).
        /// </summary>
        public void OnPlayerExitRoom(RoomController exitedRoom)
        {
            if (activeRoom == exitedRoom)
            {
                // Giữ nguyên – không sleep ngay khi Player đứng giữa ranh giới 2 room
                // Room mới sẽ gọi SetActiveRoom khi Player bước vào
            }
        }

        // ─── Registration ──────────────────────────────────────────────────────
        public void Register(DistanceCullingTarget target)
        {
            if (target != null && !distanceTargets.Contains(target))
                distanceTargets.Add(target);
        }

        public void Unregister(DistanceCullingTarget target)
        {
            distanceTargets.Remove(target);
        }

        public void RegisterRoom(RoomController room)
        {
            if (room != null && !rooms.Contains(room))
                rooms.Add(room);
        }

        public void UnregisterRoom(RoomController room)
        {
            rooms.Remove(room);
        }

        // ─── Helpers ───────────────────────────────────────────────────────────
        private Vector3 GetPlayerPosition()
        {
            if (PersistentPlayerRoot.Instance != null)
                return PersistentPlayerRoot.Instance.transform.position;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) return player.transform.position;

            return Vector3.zero;
        }

        private bool HasPlayer()
        {
            if (PersistentPlayerRoot.Instance != null) return true;
            return FindAnyObjectByType<PlayerController>() != null;
        }
    }
}
