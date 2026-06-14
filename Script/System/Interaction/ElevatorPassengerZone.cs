using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    /// <summary>
    /// Zone phát hiện Player đứng trên sàn Elevator.
    ///
    /// Đăng ký active platform cho Player khi bước vào vùng kích hoạt để Player cộng dồn vận tốc di chuyển.
    ///
    /// Đặt component này trên cùng GameObject với Collider2D (isTrigger).
    /// Chỉ phản hồi BodyCollider của Player (không phản hồi AttackCollider).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class ElevatorPassengerZone : MonoBehaviour
    {
        [Header("Auto-Start")]
        [Tooltip("Elevator tự chạy khi Player bước lên, dừng khi Player rời đi.\n" +
                 "Tắt nếu dùng Lever để điều khiển.")]
        [SerializeField] private bool startWhenBoarded = false;

        [Tooltip("Chỉ hoạt động khi startWhenBoarded = true.\n" +
                 "Elevator dừng lại khi Player bước xuống.")]
        [SerializeField] private bool stopWhenExited = true;

        private ElevatorController elevator;
        private int boardedCount = 0;

        private void Awake()
        {
            elevator = GetComponentInParent<ElevatorController>();

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            if (elevator == null)
            {
                Debug.LogWarning($"[ElevatorPassengerZone] {name}: Không tìm thấy ElevatorController trên Parent!");
                return;
            }


        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (elevator == null) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            if (!player.IsBodyCollider(other)) return;

            // Gán player làm con trực tiếp của Elevator root (có Rigidbody2D) để tránh double-movement và giật vật lý
            player.transform.SetParent(elevator.transform);

            boardedCount++;
            if (startWhenBoarded && boardedCount == 1)
                elevator.StartMoving();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (elevator == null) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other)) return;

            // Đưa player trở lại world space và khôi phục DontDestroyOnLoad
            if (player.transform.parent == elevator.transform)
            {
                player.transform.SetParent(null);
                DontDestroyOnLoad(player.gameObject);
            }

            boardedCount = Mathf.Max(0, boardedCount - 1);
            if (startWhenBoarded && boardedCount == 0 && stopWhenExited)
                elevator.StopMoving();
        }

        private void OnDisable()
        {
            boardedCount = 0;
        }
    }
}
