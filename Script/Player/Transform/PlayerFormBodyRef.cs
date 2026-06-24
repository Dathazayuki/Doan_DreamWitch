using UnityEngine;

namespace DreamKnight.Player
{
    /// <summary>
    /// Đặt trên root của form prefab (HumanForm hoặc EnemyForm).
    /// Khai báo tường minh các collider thuộc form đó để PlayerFormManager lấy.
    ///
    /// Cấu trúc prefab khuyến nghị:
    ///   EnemyFormRoot  ← PlayerFormBodyRef nằm ở đây
    ///     ├── ColliderCollision (BoxCollider2D/CapsuleCollider2D, isTrigger=false) ← BodyCollider
    ///     │     ├── WallCheck
    ///     │     └── GroundCheck
    ///     └── ColliderHitbox
    ///           ├── NormalAttack  (isTrigger=true, layer=PlayerHitbox) ← NormalHitbox
    ///           ├── UpAttack      (isTrigger=true, layer=PlayerHitbox) ← UpperHitbox
    ///           └── HeavyStrike   (isTrigger=true, layer=PlayerHitbox) ← HeavyHitbox
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerFormBodyRef : MonoBehaviour
    {
        [Header("Body Collider (va chạm với môi trường, solid)")]
        [SerializeField] private Collider2D bodyCollider;

        [Header("Guard Collider (chặn đòn tấn công)")]
        [SerializeField] private Collider2D guardCollider;

        [Header("Attack Hitboxes (isTrigger = true, layer = PlayerHitbox)")]
        [SerializeField] private Collider2D normalHitbox;
        [SerializeField] private Collider2D upperHitbox;
        [SerializeField] private Collider2D heavyHitbox;

        [Header("Movement Refs (optional - dùng cho PlayerMovement)")]
        [Tooltip("Transform GroundCheck của form này.")]
        [SerializeField] private Transform groundCheck;
        [Tooltip("Transform WallCheck của form này.")]
        [SerializeField] private Transform wallCheck;

        [Header("Em0070 References")]
        [Tooltip("Prefab FlyPile dùng khi tấn công.")]
        [SerializeField] private GameObject flyPilePrefab;
        [Tooltip("Điểm spawn FlyPile (nếu gán, sẽ dùng vị trí này; nếu không, mặc định dùng vị trí Player).")]
        [SerializeField] private Transform flyPileSpawnPoint;

        public Collider2D BodyCollider => bodyCollider;
        public Collider2D GuardCollider => guardCollider;
        public Collider2D NormalHitbox => normalHitbox;
        public Collider2D UpperHitbox  => upperHitbox;
        public Collider2D HeavyHitbox  => heavyHitbox;
        public Transform  GroundCheck  => groundCheck;
        public Transform  WallCheck    => wallCheck;
        public GameObject FlyPilePrefab => flyPilePrefab;
        public Transform FlyPileSpawnPoint => flyPileSpawnPoint;

        private void Reset()
        {
            // Tự động điền khi Add Component nếu có thể
            bodyCollider = GetComponentInChildren<Collider2D>();
        }
    }
}
