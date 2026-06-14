using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    /// <summary>
    /// Hurtbox của Lever – nhận damage từ Player và chuyển tiếp lên ElevatorLever.
    ///
    /// Cách thiết lập:
    ///   1. Tạo child object "Hurtbox" dưới LeverRoot.
    ///   2. Thêm Collider2D (không trigger, layer "EnemyHitbox" hoặc layer Player đánh vào).
    ///   3. Thêm component LeverHurtbox.
    ///   4. Gán ElevatorLever vào field leverOwner (hoặc để trống để tự tìm).
    ///
    /// IDamageable.TakeDamage() → ElevatorLever.OnHitByPlayer()
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class LeverHurtbox : MonoBehaviour, IDamageable
    {
        [SerializeField] private ElevatorLever leverOwner;

        // IDamageable: Lever "không chết", luôn alive
        public bool IsAlive => true;
        public float CurrentHealth => float.MaxValue;

        private void Awake()
        {
            if (leverOwner == null)
                leverOwner = GetComponentInParent<ElevatorLever>();

            if (leverOwner == null)
                Debug.Log($"[LeverHurtbox] {name}: Không tìm thấy ElevatorLever!");
        }

        public void TakeDamage(float damage, GameObject damageSource = null)
        {
            if (leverOwner == null) return;
            leverOwner.OnHitByPlayer();
        }
    }
}
