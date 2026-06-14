using DreamKnight.Interfaces;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvDamage : MonoBehaviour, IDamageable
    {
        [Header("Hurtbox")]
        [SerializeField] private MvEnemyBase owner;
        [SerializeField] private float partsDamageRate = 1f;
        [SerializeField] private EnemyHitVfxEmitter hitVfxEmitter;

        private Collider2D hurtboxCollider;

        public bool IsAlive => owner != null && owner.IsAlive;
        public float CurrentHealth => owner != null ? owner.CurrentHealth : 0f;

        private void Awake()
        {
            if (owner == null)
                owner = GetComponentInParent<MvEnemyBase>();

            if (hitVfxEmitter == null)
                hitVfxEmitter = GetComponentInParent<EnemyHitVfxEmitter>();

            hurtboxCollider = GetComponent<Collider2D>();
            if (hurtboxCollider == null)
                hurtboxCollider = GetComponentInChildren<Collider2D>();
        }

        public void TakeDamage(float damage, GameObject damageSource = null)
        {
            if (owner == null) return;

            float finalDamage = Mathf.Max(0f, damage) * Mathf.Max(0f, partsDamageRate);
            if (!owner.CanReceiveDamage(finalDamage, damageSource, hurtboxCollider))
            {
                owner.OnDamageBlocked(finalDamage, damageSource, hurtboxCollider);
                return;
            }

            // Quay mặt về phía nguồn damage (hướng Player đánh tới)
            // Chỉ áp dụng nếu enemy cho phép (IsFaceOnHitEnabled = true)
            if (damageSource != null && owner.IsFaceOnHitEnabled)
            {
                float deltaX = damageSource.transform.position.x - owner.transform.position.x;
                owner.FaceByDeltaX(deltaX);
            }

            Vector3 hitPos = ResolveHitPosition(damageSource);
            hitVfxEmitter?.PlayHitBlood(hitPos);
            owner.TakeDamage(finalDamage, damageSource, hitPos);
        }

        private Vector3 ResolveHitPosition(GameObject damageSource)
        {
            if (hurtboxCollider == null)
                return transform.position;

            if (damageSource != null)
                return hurtboxCollider.ClosestPoint(damageSource.transform.position);

            return hurtboxCollider.bounds.center;
        }
    }
}
