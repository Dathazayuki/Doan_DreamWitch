using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    /// <summary>
    /// Elevator di chuyển giữa các waypoint theo kiểu Metroidvania sử dụng Rigidbody2D Kinematic.
    ///
    /// Cấu trúc Prefab:
    ///   ElevatorRoot  (ElevatorController + Rigidbody2D Kinematic + Interpolate)
    ///     ├── Platform      (BoxCollider2D solid)
    ///     └── PassengerZone (ElevatorPassengerZone + Collider2D isTrigger)
    ///
    /// Waypoints: Transform rỗng KHÔNG phải con của Elevator.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class ElevatorController : MonoBehaviour
    {
        public enum ElevatorMode { Idle, Moving }
        public enum LoopMode     { PingPong, OneWay, Loop }

        [Header("Waypoints")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private LoopMode loopMode = LoopMode.PingPong;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [Tooltip("Thời gian dừng tại mỗi waypoint (0 = không chờ).")]
        [SerializeField] private float waitTimeAtWaypoint = 0f;
        [Tooltip("Tự động di chuyển khi bắt đầu (không cần Lever).")]
        [SerializeField] private bool autoStart = false;

        [Header("Easing")]
        [SerializeField] private float slowDownDistance = 0.8f;
        [SerializeField] private float minSpeedFactor   = 0.25f;

        [Header("Sound")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip   movingClip;
        [SerializeField] private AudioClip   arrivedClip;

        // ── Runtime ───────────────────────────────────────────────────────────
        private Rigidbody2D  rb;
        private ElevatorMode mode                = ElevatorMode.Idle;
        private int          currentWaypointIndex = 0;
        private int          direction            = 1;    // +1 tới, -1 lùi
        private float        waitTimer            = 0f;
        private bool         isWaiting            = false;
        private bool         stopAfterWaypoint    = false;
        private Vector2      currentVelocity;
        private Vector2      nextPosition;

        public Vector2 CurrentVelocity => currentVelocity;

        // ── Properties ────────────────────────────────────────────────────────
        public ElevatorMode Mode                => mode;
        public bool         IsMoving            => mode == ElevatorMode.Moving;
        public int          CurrentWaypointIndex => currentWaypointIndex;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType      = RigidbodyType2D.Kinematic;
            rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Start()
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                Debug.LogWarning($"[ElevatorController] {name}: Cần ít nhất 2 waypoints!");
                return;
            }
            rb.position = waypoints[0].position;
            if (autoStart) StartMoving();
        }

        private void FixedUpdate()
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                currentVelocity = Vector2.zero;
                return;
            }

            Vector2 positionBefore = rb.position;
            nextPosition = positionBefore;

            if (mode != ElevatorMode.Idle)
            {
                HandleMovement();
            }

            currentVelocity = (nextPosition - positionBefore) / Time.fixedDeltaTime;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Bắt đầu / tiếp tục di chuyển.</summary>
        public void StartMoving()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            mode      = ElevatorMode.Moving;
            isWaiting = false;
            PlayMovingSound();
        }

        /// <summary>Dừng tại vị trí hiện tại.</summary>
        public void StopMoving()
        {
            mode = ElevatorMode.Idle;
            StopMovingSound();
        }

        /// <summary>Toggle giữa Moving và Idle.</summary>
        public void Toggle()
        {
            if (mode == ElevatorMode.Moving) StopMoving();
            else                             StartMoving();
        }

        /// <summary>Đặt tốc độ từ bên ngoài.</summary>
        public void SetSpeed(float speed) => moveSpeed = Mathf.Max(0.1f, speed);

        /// <summary>
        /// Đổi hướng và di chuyển đúng 1 waypoint rồi dừng.
        /// Gọi mỗi lần Player đánh Lever.
        /// </summary>
        public void ToggleDirectionAndMoveOne()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            direction *= -1;
            int next = currentWaypointIndex + direction;

            if (next < 0 || next >= waypoints.Length)
            {
                direction *= -1;
                next = currentWaypointIndex + direction;
            }

            if (next < 0 || next >= waypoints.Length) return;

            currentWaypointIndex = next;
            stopAfterWaypoint    = true;
            isWaiting            = false;
            mode                 = ElevatorMode.Moving;
            PlayMovingSound();
        }

        // ── Movement ──────────────────────────────────────────────────────────
        private void HandleMovement()
        {
            if (isWaiting)
            {
                nextPosition = rb.position;
                waitTimer -= Time.fixedDeltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    AdvanceWaypoint();
                }
                return;
            }

            Vector2 targetPos    = waypoints[currentWaypointIndex].position;
            Vector2 currentPos   = rb.position;
            float   distToTarget = Vector2.Distance(currentPos, targetPos);

            if (distToTarget <= 0.05f)
            {
                nextPosition = targetPos;
                rb.MovePosition(targetPos);
                OnReachedWaypoint();
                return;
            }

            float speedFactor = (distToTarget < slowDownDistance)
                ? Mathf.Lerp(minSpeedFactor, 1f, distToTarget / slowDownDistance)
                : 1f;

            float step = moveSpeed * speedFactor * Time.fixedDeltaTime;
            nextPosition = Vector2.MoveTowards(currentPos, targetPos, step);
            rb.MovePosition(nextPosition);
        }

        private void OnReachedWaypoint()
        {
            PlayArrivedSound();

            if (stopAfterWaypoint)
            {
                stopAfterWaypoint = false;
                StopMoving();
                return;
            }

            if (waitTimeAtWaypoint > 0f)
            {
                isWaiting = true;
                waitTimer = waitTimeAtWaypoint;
            }
            else
            {
                AdvanceWaypoint();
            }
        }

        private void AdvanceWaypoint()
        {
            int next = currentWaypointIndex + direction;

            switch (loopMode)
            {
                case LoopMode.PingPong:
                    if (next >= waypoints.Length || next < 0)
                    {
                        direction *= -1;
                        next = currentWaypointIndex + direction;
                    }
                    break;

                case LoopMode.OneWay:
                    if (next >= waypoints.Length)
                    {
                        StopMoving();
                        return;
                    }
                    break;

                case LoopMode.Loop:
                    next = (next + waypoints.Length) % waypoints.Length;
                    break;
            }

            currentWaypointIndex = next;
        }

        // ── Audio ─────────────────────────────────────────────────────────────
        private void PlayMovingSound()
        {
            if (audioSource == null || movingClip == null) return;
            audioSource.clip = movingClip;
            audioSource.loop = true;
            if (!audioSource.isPlaying) audioSource.Play();
        }

        private void StopMovingSound()
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        private void PlayArrivedSound()
        {
            if (audioSource == null || arrivedClip == null) return;
            audioSource.PlayOneShot(arrivedClip);
        }

        // ── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.18f);
                if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 0.3f, 0f));
        }
#endif
    }
}
