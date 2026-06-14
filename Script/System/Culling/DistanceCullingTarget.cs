using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Systems.Culling
{
    /// <summary>
    /// Gắn lên Enemy / Projectile / VFX để tham gia Distance Culling.
    /// Dùng hysteresis: Cull khi dist > disableDistance, UnCull khi dist < enableDistance.
    /// Vùng [enableDistance, disableDistance] không thay đổi trạng thái.
    /// </summary>
    [DisallowMultipleComponent]
    public class DistanceCullingTarget : MonoBehaviour, ICullable
    {
        // ─── Inspector ─────────────────────────────────────────────────────────
        [Header("Culling Type")]
        [SerializeField] private CullingTargetType targetType = CullingTargetType.Enemy;

        [Header("Hysteresis Distance Override (0 = dùng giá trị mặc định từ CullingManager)")]
        [Tooltip("UnCull khi distance < giá trị này. 0 = dùng default của CullingManager.")]
        [SerializeField] private float overrideEnableDistance  = 0f;
        [Tooltip("Cull khi distance > giá trị này. 0 = dùng default của CullingManager.")]
        [SerializeField] private float overrideDisableDistance = 0f;

        [Header("Components bị disable khi Cull (để trống = tự detect)")]
        [Tooltip("Danh sách MonoBehaviour bị disable khi bị cull. Để trống để tự detect theo targetType.")]
        [SerializeField] private MonoBehaviour[] componentsToCull;

        // ─── Runtime ───────────────────────────────────────────────────────────
        private bool isCulled;
        private bool isRoomSleeping;
        private Rigidbody2D rb;
        private RigidbodyConstraints2D originalConstraints;
        private ParticleSystem[] cachedParticleSystems;

        // ─── ICullable Properties ──────────────────────────────────────────────
        public bool IsCulled        => isCulled;
        /// <summary>True khi RoomCullingMember đặt room sang sleep – Distance culling không override.</summary>
        public bool IsRoomSleeping  => isRoomSleeping;

        // ─── Override Properties ───────────────────────────────────────────────
        public float OverrideEnableDistance  => overrideEnableDistance;
        public float OverrideDisableDistance => overrideDisableDistance;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null) originalConstraints = rb.constraints;

            if (componentsToCull == null || componentsToCull.Length == 0)
                AutoDetectComponents();

            if (targetType == CullingTargetType.Vfx)
                cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void OnEnable()  => CullingManager.Instance?.Register(this);
        private void OnDisable() => CullingManager.Instance?.Unregister(this);

        // ─── ICullable ─────────────────────────────────────────────────────────
        public void Cull()
        {
            if (isCulled) return;
            isCulled = true;
            ApplyCull();
        }

        public void UnCull()
        {
            if (!isCulled) return;
            // Không UnCull nếu Room đang ngủ (Room Culling ưu tiên cao hơn)
            if (isRoomSleeping) return;
            isCulled = false;
            ApplyUnCull();
        }

        // ─── Room Sleep Override ───────────────────────────────────────────────

        /// <summary>
        /// Được gọi bởi RoomCullingMember khi Room bắt đầu sleep.
        /// Force Cull bất kể khoảng cách, và đặt cờ isRoomSleeping.
        /// </summary>
        public void SetRoomSleep(bool sleeping)
        {
            isRoomSleeping = sleeping;
            if (sleeping)
            {
                // Force cull, bỏ qua trạng thái isCulled hiện tại
                isCulled = true;
                ApplyCull();
            }
            else
            {
                // Wake – reset isCulled để CullingManager tính lại khoảng cách
                isCulled = false;
                ApplyUnCull();
            }
        }

        // ─── Apply Helpers ─────────────────────────────────────────────────────
        private void ApplyCull()
        {
            // Tắt components AI/logic
            if (componentsToCull != null)
            {
                for (int i = 0; i < componentsToCull.Length; i++)
                {
                    if (componentsToCull[i] != null)
                        componentsToCull[i].enabled = false;
                }
            }

            // Enemy: freeze Rigidbody
            if (targetType == CullingTargetType.Enemy && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.constraints   = RigidbodyConstraints2D.FreezeAll;
            }

            // VFX: pause particles
            if (targetType == CullingTargetType.Vfx && cachedParticleSystems != null)
            {
                for (int i = 0; i < cachedParticleSystems.Length; i++)
                {
                    if (cachedParticleSystems[i] != null && cachedParticleSystems[i].isPlaying)
                        cachedParticleSystems[i].Pause();
                }
            }
        }

        private void ApplyUnCull()
        {
            // Bật lại components
            if (componentsToCull != null)
            {
                for (int i = 0; i < componentsToCull.Length; i++)
                {
                    if (componentsToCull[i] != null)
                        componentsToCull[i].enabled = true;
                }
            }

            // Enemy: khôi phục Rigidbody constraints
            if (targetType == CullingTargetType.Enemy && rb != null)
            {
                rb.constraints = originalConstraints;
            }

            // VFX: resume particles
            if (targetType == CullingTargetType.Vfx && cachedParticleSystems != null)
            {
                for (int i = 0; i < cachedParticleSystems.Length; i++)
                {
                    if (cachedParticleSystems[i] != null && cachedParticleSystems[i].isPaused)
                        cachedParticleSystems[i].Play();
                }
            }
        }

        // ─── Auto Detect ───────────────────────────────────────────────────────
        private void AutoDetectComponents()
        {
            switch (targetType)
            {
                case CullingTargetType.Enemy:
                    // Tìm MvEnemyBase (hoặc base class)
                    MonoBehaviour enemy = GetComponent<Mv.MvEnemyBase>();
                    if (enemy != null)
                        componentsToCull = new MonoBehaviour[] { enemy };
                    break;

                case CullingTargetType.Projectile:
                    // Tìm tất cả MonoBehaviour trên cùng GameObject (ngoại trừ chính nó)
                    MonoBehaviour[] all = GetComponents<MonoBehaviour>();
                    System.Collections.Generic.List<MonoBehaviour> list =
                        new System.Collections.Generic.List<MonoBehaviour>();
                    foreach (var mb in all)
                    {
                        if (mb != null && mb != this)
                            list.Add(mb);
                    }
                    componentsToCull = list.ToArray();
                    break;

                case CullingTargetType.Vfx:
                    // VFX dùng particle pause, không cần disable component
                    componentsToCull = new MonoBehaviour[0];
                    break;
            }
        }

        // ─── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float enableDist  = overrideEnableDistance  > 0f ? overrideEnableDistance  : 18f;
            float disableDist = overrideDisableDistance > 0f ? overrideDisableDistance : 22f;

            // Màu xanh = ngưỡng UnCull
            UnityEditor.Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.4f);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, enableDist);

            // Màu đỏ = ngưỡng Cull
            UnityEditor.Handles.color = new Color(0.9f, 0.2f, 0.2f, 0.4f);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, disableDist);
        }
#endif
    }
}
