using System.Collections;
using System.Collections.Generic;
using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Culling
{
    [DisallowMultipleComponent]
    public class CullingManager : MonoBehaviour
    {
        private static CullingManager instance;

        public static CullingManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindAnyObjectByType<CullingManager>();
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("[CullingManager]");
                instance = go.AddComponent<CullingManager>();
                return instance;
            }
        }

        public static CullingManager Current => instance;

        [Header("Update Interval")]
        [Tooltip("Seconds between culling checks. Increase this to save CPU.")]
        [SerializeField] private float updateInterval = 0.2f;

        [Header("Default Hysteresis Distances")]
        [Tooltip("Distance used to re-enable culled targets. Must be smaller than disable distance.")]
        [SerializeField] private float defaultEnableDistance = 18f;
        [Tooltip("Distance used to cull active targets. Must be larger than enable distance.")]
        [SerializeField] private float defaultDisableDistance = 22f;

        private readonly List<DistanceCullingTarget> distanceTargets = new List<DistanceCullingTarget>();
        private readonly List<RoomController> rooms = new List<RoomController>();
        private readonly HashSet<RoomController> activeRooms = new HashSet<RoomController>();

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

        private void OnEnable()
        {
            StartCoroutine(CullingLoop());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private IEnumerator CullingLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.05f, updateInterval));
            while (true)
            {
                TickDistanceCulling();
                yield return wait;
            }
        }

        private void TickDistanceCulling()
        {
            Vector3 playerPos = GetPlayerPosition();
            if (playerPos == Vector3.zero && !HasPlayer())
                return;

            for (int i = distanceTargets.Count - 1; i >= 0; i--)
            {
                DistanceCullingTarget target = distanceTargets[i];
                if (target == null)
                {
                    distanceTargets.RemoveAt(i);
                    continue;
                }

                if (target.IsRoomSleeping)
                    continue;

                float dist = Vector3.Distance(target.transform.position, playerPos);
                float enableDist = target.OverrideEnableDistance > 0f
                    ? target.OverrideEnableDistance
                    : defaultEnableDistance;
                float disableDist = target.OverrideDisableDistance > 0f
                    ? target.OverrideDisableDistance
                    : defaultDisableDistance;

                if (!target.IsCulled && dist > disableDist)
                    target.Cull();
                else if (target.IsCulled && dist < enableDist)
                    target.UnCull();
            }
        }

        public void SetActiveRoom(RoomController newRoom)
        {
            if (newRoom == null)
                return;

            if (activeRooms.Add(newRoom))
                RefreshRoomStates();
        }

        public void OnPlayerExitRoom(RoomController exitedRoom)
        {
            if (exitedRoom == null)
                return;

            if (activeRooms.Remove(exitedRoom))
                RefreshRoomStates();
        }

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
            if (room == null || rooms.Contains(room))
                return;

            rooms.Add(room);
        }

        public void UnregisterRoom(RoomController room)
        {
            rooms.Remove(room);
            activeRooms.Remove(room);
            RefreshRoomStates();
        }

        private void RefreshRoomStates()
        {
            HashSet<RoomController> roomsToWake = new HashSet<RoomController>();

            foreach (RoomController room in activeRooms)
            {
                if (room == null)
                    continue;

                roomsToWake.Add(room);

                RoomController[] adjacent = room.AdjacentRooms;
                if (adjacent == null)
                    continue;

                for (int i = 0; i < adjacent.Length; i++)
                {
                    if (adjacent[i] != null)
                        roomsToWake.Add(adjacent[i]);
                }
            }

            for (int i = rooms.Count - 1; i >= 0; i--)
            {
                RoomController room = rooms[i];
                if (room == null)
                {
                    rooms.RemoveAt(i);
                    continue;
                }

                room.SetRoomActive(roomsToWake.Contains(room));
            }
        }

        private Vector3 GetPlayerPosition()
        {
            if (PersistentPlayerRoot.Instance != null)
                return PersistentPlayerRoot.Instance.transform.position;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                return player.transform.position;

            return Vector3.zero;
        }

        private bool HasPlayer()
        {
            if (PersistentPlayerRoot.Instance != null)
                return true;

            return FindAnyObjectByType<PlayerController>() != null;
        }
    }
}
