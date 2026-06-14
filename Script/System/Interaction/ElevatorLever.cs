using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    /// <summary>
    /// Lever điều khiển Elevator theo kiểu Metroidvania:
    ///   - Đánh lần 1 → Elevator chạy theo chiều A
    ///   - Đánh lần 2 → Elevator đổi chiều, chạy ngược lại
    ///   - Mỗi lần đánh chỉ di chuyển đúng 1 waypoint rồi dừng.
    ///   - Visual đổi theo hướng hiện tại qua switch-case.
    ///
    /// Cấu trúc Prefab:
    ///   LeverRoot
    ///     ├── [ElevatorLever]
    ///     ├── Hurtbox      ← [LeverHurtbox] + Collider2D solid
    ///     ├── Visual_Up    ← Sprite khi Elevator sẽ đi lên/tới
    ///     └── Visual_Down  ← Sprite khi Elevator sẽ đi xuống/về
    /// </summary>
    [DisallowMultipleComponent]
    public class ElevatorLever : MonoBehaviour
    {
        // ── Direction state ────────────────────────────────────────────────────
        public enum LeverDirection
        {
            Forward,    // Elevator đi tới (waypoint index tăng)
            Backward,   // Elevator đi lùi (waypoint index giảm)
        }

        [Header("Hurtbox")]
        [Tooltip("Child object chứa LeverHurtbox. Tự tìm nếu để trống.")]
        [SerializeField] private LeverHurtbox hurtbox;

        [Header("Target Elevators")]
        [Tooltip("Danh sách Elevator được điều khiển.")]
        [SerializeField] private ElevatorController[] targetElevators;

        [Header("Cooldown")]
        [Tooltip("Thời gian khóa sau mỗi lần kích hoạt (giây).")]
        [SerializeField] private float activationCooldown = 0.6f;

        [Header("Visuals")]
        [Tooltip("Hiện khi Elevator sắp đi theo chiều Forward (lên/tới).")]
        [SerializeField] private GameObject visualForward;
        [Tooltip("Hiện khi Elevator sắp đi theo chiều Backward (xuống/về).")]
        [SerializeField] private GameObject visualBackward;

        [Header("Sound")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hitClip;

        // ── Runtime ───────────────────────────────────────────────────────────
        private LeverDirection currentDirection = LeverDirection.Forward;
        private float nextActivationTime;

        public LeverDirection CurrentDirection => currentDirection;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (hurtbox == null)
                hurtbox = GetComponentInChildren<LeverHurtbox>();
        }

        private void Start()
        {
            RefreshVisuals();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Player đánh trúng → đổi hướng, di chuyển 1 bước rồi dừng.</summary>
        public void OnHitByPlayer()
        {
            if (Time.time < nextActivationTime) return;
            nextActivationTime = Time.time + activationCooldown;

            // Đổi hướng hiển thị
            switch (currentDirection)
            {
                case LeverDirection.Forward:
                    currentDirection = LeverDirection.Backward;
                    break;
                case LeverDirection.Backward:
                    currentDirection = LeverDirection.Forward;
                    break;
            }

            RefreshVisuals();
            PlayHitSound();

            if (targetElevators == null) return;
            foreach (ElevatorController elev in targetElevators)
            {
                if (elev == null) continue;
                elev.ToggleDirectionAndMoveOne();
            }
        }

        // ── Visuals ───────────────────────────────────────────────────────────
        private void RefreshVisuals()
        {
            switch (currentDirection)
            {
                case LeverDirection.Forward:
                    if (visualForward  != null) visualForward .SetActive(true);
                    if (visualBackward != null) visualBackward.SetActive(false);
                    break;
                case LeverDirection.Backward:
                    if (visualForward  != null) visualForward .SetActive(false);
                    if (visualBackward != null) visualBackward.SetActive(true);
                    break;
            }
        }

        // ── Audio ─────────────────────────────────────────────────────────────
        private void PlayHitSound()
        {
            if (audioSource == null || hitClip == null) return;
            audioSource.PlayOneShot(hitClip);
        }

        // ── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = currentDirection == LeverDirection.Forward
                ? new Color(0.2f, 0.8f, 1f)
                : new Color(1f, 0.6f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, 0.22f);

            if (targetElevators == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
            foreach (ElevatorController elev in targetElevators)
            {
                if (elev == null) continue;
                Gizmos.DrawLine(transform.position, elev.transform.position);
            }
        }
#endif
    }
}
