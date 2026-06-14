using DreamKnight.Interfaces;
using DreamKnight.Player;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class Em0190Arrow : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D hitCollider;
        [SerializeField] private GameObject vfxFlying;
        [SerializeField] private GameObject vfxEnd;

        [Header("Lifetime")]
        [SerializeField] private float maxLifetime = 4f;
        [SerializeField] private float stuckLifetime = 1.5f;

        [Header("Impact Filter")]
        [SerializeField] private LayerMask groundImpactMask;

        private float speed;
        private float damage;
        private GameObject damageSource;
        private bool hasImpacted;

        private void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();
            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();

            if (groundImpactMask.value == 0)
            {
                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer >= 0)
                    groundImpactMask = 1 << groundLayer;
            }

            SetVfxState(isFlying: true);
        }

        private void OnEnable()
        {
            Destroy(gameObject, Mathf.Max(0.1f, maxLifetime));
        }

        public void Launch(Vector2 direction, float launchSpeed, float arrowDamage, GameObject source)
        {
            speed = Mathf.Max(0f, launchSpeed);
            damage = Mathf.Max(0f, arrowDamage);
            damageSource = source;

            Vector2 dir = direction.sqrMagnitude < 0.0001f ? Vector2.right : direction.normalized;
            ApplyDirectionRotation(dir);

            if (rb != null)
                rb.linearVelocity = dir * speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryImpact(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null)
                return;

            TryImpact(collision.collider);
        }

        private void TryImpact(Collider2D other)
        {
            if (hasImpacted || other == null)
                return;

            if (other.transform.root == transform.root)
                return;

            if (damageSource != null)
            {
                Transform sourceRoot = damageSource.transform.root;
                if (sourceRoot != null && other.transform.root == sourceRoot)
                    return;
            }

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();

            bool hitPlayer = damageable is PlayerController && damageable.IsAlive;
            bool hitGround = !other.isTrigger && IsInGroundImpactMask(other.gameObject.layer);
            if (!hitPlayer && !hitGround)
                return;

            hasImpacted = true;

            if (hitPlayer)
                damageable.TakeDamage(damage, damageSource != null ? damageSource : gameObject);

            if (hitCollider != null)
                hitCollider.enabled = false;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = false;
            }

            transform.SetParent(other.transform, true);
            SetVfxState(isFlying: false);
            Destroy(gameObject, Mathf.Max(0.05f, stuckLifetime));
        }

        private bool IsInGroundImpactMask(int layer)
        {
            return (groundImpactMask.value & (1 << layer)) != 0;
        }

        private void SetVfxState(bool isFlying)
        {
            if (vfxFlying != null)
                vfxFlying.SetActive(isFlying);
            if (vfxEnd != null)
                vfxEnd.SetActive(!isFlying);
        }

        private void ApplyDirectionRotation(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
