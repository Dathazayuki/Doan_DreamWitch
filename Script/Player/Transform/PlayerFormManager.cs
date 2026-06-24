using UnityEngine;

namespace DreamKnight.Player
{
    /// <summary>
    /// Quản lý việc tắt/bật form prefab và cung cấp tham chiếu
    /// đến body collider + hitbox collider đang active của Player.
    ///
    /// Đặt trên root Player object.
    /// Các hệ thống khác (ZoneBase, MvAttack, ...) query component này
    /// thay vì dùng GetComponent&lt;Collider2D&gt;() trực tiếp trên root.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerFormManager : MonoBehaviour
    {
        [Header("Human Form")]
        [Tooltip("Root GameObject của Human Form (chứa Sword, ColliderCollision, ColliderHitbox...).")]
        [SerializeField] private GameObject humanFormRoot;

        // ── Runtime state ──────────────────────────────────────────────────────────
        private GameObject activeFormInstance;
        private Collider2D activeBodyCollider;
        private PlayerFormBodyRef activeBodyRef;

        // ── Properties ─────────────────────────────────────────────────────────────
        /// <summary>Body collider (solid, non-trigger) của form đang active.</summary>
        public Collider2D ActiveBodyCollider => activeBodyCollider;

        /// <summary>GameObject chứa body collider đang active.</summary>
        public GameObject ActiveBodyColliderObject => activeBodyCollider != null ? activeBodyCollider.gameObject : null;

        /// <summary>Guard collider của form đang active (nếu có).</summary>
        public Collider2D ActiveGuardCollider => activeBodyRef != null ? activeBodyRef.GuardCollider : null;

        /// <summary>Trả về toàn bộ struct tham chiếu của form hiện tại (để lấy GroundCheck, WallCheck...)</summary>
        public PlayerFormBodyRef ActiveBodyRef => activeBodyRef;

        private void Awake()
        {
            // Khởi tạo với Human form
            RefreshBodyColliderFromHuman();
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Biến hình sang dạng Enemy form. Instantiate prefab (nếu chưa có), bật nó và tắt HumanForm.
        /// </summary>
        public void ActivateForm(PlayerFormDataSO entry)
        {
            if (entry == null) return;

            // Nếu prefab đã được instantiate trước đó và vẫn còn, dùng lại
            if (activeFormInstance != null && activeFormInstance.activeSelf)
                DeactivateCurrentForm();

            // Tắt Human form
            if (humanFormRoot != null)
                humanFormRoot.SetActive(false);

            // Instantiate prefab nếu chưa có trong cache
            if (entry.formPrefab != null)
            {
                activeFormInstance = Instantiate(entry.formPrefab, transform);
                activeFormInstance.name = $"{entry.formPrefab.name}_Instance";

                // Đặt lại local transform (không inherit scale kỳ lạ từ root khi flip)
                activeFormInstance.transform.localPosition = Vector3.zero;
                activeFormInstance.transform.localRotation = Quaternion.identity;
                activeFormInstance.transform.localScale = Vector3.one;

                // Lấy body + hitbox ref từ form instance
                activeBodyRef = activeFormInstance.GetComponentInChildren<PlayerFormBodyRef>(true);
                activeBodyCollider = activeBodyRef != null ? activeBodyRef.BodyCollider : null;
                SetGuardColliderActive(false);
            }
            else
            {
                activeBodyCollider = null;
                activeBodyRef = null;
            }
        }

        /// <summary>
        /// Tắt form hiện tại, bật lại HumanForm.
        /// </summary>
        public void DeactivateCurrentForm()
        {
            if (activeFormInstance != null)
            {
                Destroy(activeFormInstance);
                activeFormInstance = null;
            }

            activeBodyRef = null;

            // Bật lại Human form
            if (humanFormRoot != null)
                humanFormRoot.SetActive(true);

            RefreshBodyColliderFromHuman();
        }

        /// <summary>
        /// Lấy các hitbox collider của form đang active.
        /// </summary>
        public bool TryGetActiveHitboxes(out Collider2D normal, out Collider2D upper, out Collider2D heavy)
        {
            if (activeBodyRef != null)
            {
                normal = activeBodyRef.NormalHitbox;
                upper  = activeBodyRef.UpperHitbox;
                heavy  = activeBodyRef.HeavyHitbox;
                return true;
            }

            normal = upper = heavy = null;
            return false;
        }

        // ── Private helpers ────────────────────────────────────────────────────────

        private void RefreshBodyColliderFromHuman()
        {
            if (humanFormRoot == null)
            {
                activeBodyCollider = null;
                return;
            }

            var humanBodyRef = humanFormRoot.GetComponentInChildren<PlayerFormBodyRef>(true);
            activeBodyRef = humanBodyRef;
            activeBodyCollider = humanBodyRef != null
                ? humanBodyRef.BodyCollider
                : humanFormRoot.GetComponentInChildren<Collider2D>(true);
            SetGuardColliderActive(false);
        }

        private void SetGuardColliderActive(bool isActive)
        {
            Collider2D guard = activeBodyRef != null ? activeBodyRef.GuardCollider : null;
            if (guard == null)
                return;

            guard.gameObject.SetActive(isActive);
        }
    }
}
