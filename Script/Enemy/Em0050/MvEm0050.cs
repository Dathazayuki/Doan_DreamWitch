using UnityEngine;

namespace Mv
{
    /// <summary>
    /// Em0050 – Waypoint Patrol với góc xoay per-waypoint.
    ///
    /// Mỗi waypoint (Transform) có Rotation Z riêng → đó là góc xoay Enemy
    /// khi đến điểm đó.  Gizmo hiển thị mũi tên vàng chỉ hướng xoay.
    ///
    /// Setup:
    ///   1. Tạo N child GameObject rỗng làm waypoint.
    ///   2. Kéo thả vào mảng Waypoints.
    ///   3. Trong Scene View, xoay từng waypoint để chỉnh góc mũi tên vàng.
    ///   4. Rigidbody2D → Kinematic, Gravity Scale = 0.
    /// </summary>
    [DisallowMultipleComponent]
    public class MvEm0050 : MvEnemyBase
    {
        [Header("Em0050 – Waypoint Patrol")]
        [SerializeField] private float patrolSpeed      = 2f;
        [SerializeField] private float waypointReachDist = 0.15f;
        [Tooltip("N điểm. Rotation Z của mỗi điểm = góc xoay Enemy khi đến đó.")]
        [SerializeField] private Transform[] waypoints;

        [Header("Visual")]
        [Tooltip("Bật xoay Enemy theo Rotation Z của waypoint")]
        [SerializeField] private bool  rotateSpriteZ = true;
        [Tooltip("Tốc độ xoay (độ/giây). 0 = snap ngay lập tức (khuyến nghị)")]
        [SerializeField] private float rotationSpeed  = 0f;

        // ── Runtime ──
        private Rigidbody2D cachedRb;
        private int         currentWpIndex  = 0;
        private Vector2     desiredVelocity = Vector2.zero;
        private float       currentAngleZ   = 0f;
        private float       targetAngleZ    = 0f;

        // ── Factory ──
        protected override EnemyState CreateIdleState(EnemyContext ctx)
            => new Em0050IdleState(ctx);
        protected override EnemyState CreateRunState(EnemyContext ctx)
            => new Em0050RunState(ctx);

        // ── Lifecycle ──
        protected override void Awake()
        {
            base.Awake();
            cachedRb = GetComponent<Rigidbody2D>();
            if (cachedRb != null)
                cachedRb.gravityScale = 0f;

            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        // Em0050 không lật mặt khi bị Player đánh (dùng rotation Z để xoay, không dùng scale flip)
        protected override bool FaceOnHitEnabled => false;
        // Bỏ qua ApplyKinematicVerticalMotion của base
        protected override void FixedUpdate()
        {
            if (cachedRb != null)
                cachedRb.linearVelocity = desiredVelocity;
        }

        // ── Public: gọi từ Em0050RunState.Tick() ──
        public void TickWaypointPatrol()
        {
            if (!IsAlive) return;
            if (waypoints == null || waypoints.Length == 0)
            {
                desiredVelocity = Vector2.zero;
                return;
            }

            SetRunAnimation(true, false);

            // Xoay dần nếu có tốc độ
            if (rotateSpriteZ && rotationSpeed > 0f)
                UpdateSmoothRotation();

            Vector2 myPos  = transform.position;
            Vector2 tgtPos = GetWaypointPos(currentWpIndex);
            Vector2 toNext = tgtPos - myPos;

            // ── Đến waypoint → cập nhật góc xoay → sang điểm tiếp theo ──
            if (toNext.magnitude <= waypointReachDist)
            {
                if (rotateSpriteZ)
                {
                    if (rotationSpeed <= 0f)
                        SnapToWaypointRotation(currentWpIndex);
                    else
                        SetTargetRotation(currentWpIndex);
                }

                currentWpIndex = (currentWpIndex + 1) % waypoints.Length;
                tgtPos = GetWaypointPos(currentWpIndex);
                toNext = tgtPos - myPos;
            }

            float dist = toNext.magnitude;
            if (dist < 0.001f)
            {
                desiredVelocity = Vector2.zero;
                return;
            }

            desiredVelocity = (toNext / dist) * Mathf.Max(0f, patrolSpeed);
        }

        // ── Rotation helpers ──

        /// <summary>Lấy Rotation Z của waypoint[i] (chuẩn hóa về [-180,180]).</summary>
        private float GetWaypointAngle(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length) return 0f;
            Transform wp = waypoints[index];
            if (wp == null) return 0f;
            float z = wp.eulerAngles.z;
            if (z > 180f) z -= 360f;  // chuẩn hóa về [-180, 180]
            return z;
        }

        /// <summary>Snap ngay lập tức về góc của waypoint[i].</summary>
        private void SnapToWaypointRotation(int index)
        {
            float z = GetWaypointAngle(index);
            currentAngleZ = z;
            targetAngleZ  = z;
            transform.rotation = Quaternion.Euler(0f, 0f, z);
        }

        /// <summary>Đặt target để SmoothRotate tiến tới.</summary>
        private void SetTargetRotation(int index)
        {
            targetAngleZ = GetWaypointAngle(index);
        }

        /// <summary>Gọi mỗi frame để xoay dần về targetAngleZ.</summary>
        private void UpdateSmoothRotation()
        {
            if (Mathf.Approximately(currentAngleZ, targetAngleZ)) return;
            currentAngleZ = Mathf.MoveTowardsAngle(
                currentAngleZ, targetAngleZ, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngleZ);
        }

        // ── Position helper ──
        private Vector2 GetWaypointPos(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length)
                return transform.position;
            Transform wp = waypoints[index];
            return wp != null ? (Vector2)wp.position : (Vector2)transform.position;
        }

        // ── Gizmo: mũi tên xoay + đường đi ──
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform wp = waypoints[i];
                if (wp == null) continue;

                Vector3 pos    = wp.position;
                float   zDeg   = wp.eulerAngles.z;
                Vector3 fwdDir = Quaternion.Euler(0f, 0f, zDeg) * Vector3.right;

                // ── Đường đi đến waypoint tiếp theo ──
                int next = (i + 1) % waypoints.Length;
                if (waypoints[next] != null)
                {
                    Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.6f);
                    Gizmos.DrawLine(pos, waypoints[next].position);
                }

                // ── Vòng tròn tại waypoint ──
                Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.9f);
                Gizmos.DrawWireSphere(pos, 0.14f);

                // ── Mũi tên vàng chỉ hướng xoay ──
                float   arrowLen = 0.45f;
                Vector3 tip      = pos + fwdDir * arrowLen;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pos, tip);

                // Đầu mũi tên
                Vector3 perp = new Vector3(-fwdDir.y, fwdDir.x, 0f);
                float   hw   = 0.1f;
                float   hl   = 0.15f;
                Gizmos.DrawLine(tip, tip - fwdDir * hl + perp * hw);
                Gizmos.DrawLine(tip, tip - fwdDir * hl - perp * hw);

                // ── Label số thứ tự + góc ──
                UnityEditor.Handles.color = Color.cyan;
                UnityEditor.Handles.Label(
                    pos + Vector3.up * 0.28f,
                    $"WP{i}  Z={zDeg:F0}°");
            }
        }
#endif
    }
}
