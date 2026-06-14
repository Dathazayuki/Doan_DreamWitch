using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Systems.Culling
{
    /// <summary>
    /// Gắn lên Enemy / Projectile / VFX để đăng ký vào RoomController cha.
    /// Implements ICullable – nhận lệnh Cull/UnCull từ RoomController khi Room sleep/wake.
    ///
    /// Ưu tiên: Room Culling > Distance Culling.
    /// Khi Room sleep, nếu GameObject có cả DistanceCullingTarget thì DistanceCullingTarget
    /// sẽ không UnCull được (cờ isRoomSleeping chặn lại).
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomCullingMember : MonoBehaviour, ICullable
    {
        // ─── Inspector ─────────────────────────────────────────────────────────
        [Header("Components bị disable khi Room sleep (để trống = tự detect)")]
        [Tooltip("Danh sách MonoBehaviour bị disable khi Room sleep. Để trống để tự detect.")]
        [SerializeField] private MonoBehaviour[] componentsToCull;

        // ─── Runtime ───────────────────────────────────────────────────────────
        private bool isCulled;
        private RoomController parentRoom;
        private Rigidbody2D rb;
        private RigidbodyConstraints2D originalConstraints;
        private ParticleSystem[] cachedParticleSystems;
        private DistanceCullingTarget distanceCullingTarget;

        public bool IsCulled => isCulled;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null) originalConstraints = rb.constraints;

            cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
            distanceCullingTarget = GetComponent<DistanceCullingTarget>();

            // Tự detect components nếu chưa khai báo
            if (componentsToCull == null || componentsToCull.Length == 0)
                AutoDetectComponents();

            // Đăng ký vào RoomController cha gần nhất
            parentRoom = GetComponentInParent<RoomController>();
            if (parentRoom != null)
                parentRoom.RegisterMember(this);
            else
                Debug.LogWarning($"[RoomCullingMember] {gameObject.name}: Không tìm thấy RoomController cha. Hãy đặt object này vào trong Room hoặc đặt room reference thủ công.", this);
        }

        private void OnDestroy()
        {
            if (parentRoom != null)
                parentRoom.UnregisterMember(this);
        }

        // ─── ICullable ─────────────────────────────────────────────────────────
        public void Cull()
        {
            if (isCulled) return;
            isCulled = true;

            ApplyCull();

            // Thông báo cho DistanceCullingTarget (nếu có) để không override sleep
            if (distanceCullingTarget != null)
                distanceCullingTarget.SetRoomSleep(true);
        }

        public void UnCull()
        {
            if (!isCulled) return;
            isCulled = false;

            ApplyUnCull();

            // Giải phóng Room sleep flag cho DistanceCullingTarget
            if (distanceCullingTarget != null)
                distanceCullingTarget.SetRoomSleep(false);
        }

        // ─── Apply Helpers ─────────────────────────────────────────────────────
        private void ApplyCull()
        {
            if (componentsToCull != null)
            {
                for (int i = 0; i < componentsToCull.Length; i++)
                {
                    if (componentsToCull[i] != null)
                        componentsToCull[i].enabled = false;
                }
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.constraints   = RigidbodyConstraints2D.FreezeAll;
            }

            if (cachedParticleSystems != null)
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
            if (componentsToCull != null)
            {
                for (int i = 0; i < componentsToCull.Length; i++)
                {
                    if (componentsToCull[i] != null)
                        componentsToCull[i].enabled = true;
                }
            }

            if (rb != null)
                rb.constraints = originalConstraints;

            if (cachedParticleSystems != null)
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
            // Ưu tiên detect MvEnemyBase
            Mv.MvEnemyBase enemy = GetComponent<Mv.MvEnemyBase>();
            if (enemy != null)
            {
                componentsToCull = new MonoBehaviour[] { enemy };
                return;
            }

            // Fallback: lấy tất cả MonoBehaviour trừ chính nó và DistanceCullingTarget
            MonoBehaviour[] all = GetComponents<MonoBehaviour>();
            System.Collections.Generic.List<MonoBehaviour> list =
                new System.Collections.Generic.List<MonoBehaviour>();
            foreach (MonoBehaviour mb in all)
            {
                if (mb == null || mb == this || mb is DistanceCullingTarget) continue;
                list.Add(mb);
            }
            componentsToCull = list.ToArray();
        }
    }
}
