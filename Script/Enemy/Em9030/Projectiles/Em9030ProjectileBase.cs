using System.Collections.Generic;
using DreamKnight.Interfaces;
using UnityEngine;

namespace Mv
{
    public interface IEm9030Projectile
    {
        void Initialize(MvEm9030 owner, GameObject damageSource, float damage, Vector2 direction, Vector3 targetPosition);
    }

    [DisallowMultipleComponent]
    public abstract class Em9030ProjectileBase : MonoBehaviour, IEm9030Projectile
    {
        [Header("References")]
        [SerializeField] protected Rigidbody2D rb;
        [SerializeField] protected Collider2D hitCollider;

        [Header("Hit")]
        [SerializeField] protected LayerMask targetLayer = ~0;
        [SerializeField] protected bool releaseOnFirstHit = true;

        [Header("Lifetime")]
        [SerializeField] protected float lifetime = 4f;

        protected readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        protected MvEm9030 owner;
        protected GameObject damageSource;
        protected Vector2 direction;
        protected Vector3 targetPosition;
        protected float damage;
        protected float aliveTime;
        protected bool active;

        protected virtual void Awake()
        {
            ResolveReferences();
        }

        protected virtual void OnDisable()
        {
            active = false;
            aliveTime = 0f;
            owner = null;
            damageSource = null;
            damagedTargets.Clear();

            if (hitCollider != null)
                hitCollider.enabled = true;

            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        public virtual void Initialize(MvEm9030 poolOwner, GameObject source, float attackDamage, Vector2 travelDirection, Vector3 targetWorldPosition)
        {
            ResolveReferences();
            enabled = true;

            owner = poolOwner;
            damageSource = source;
            damage = Mathf.Max(0f, attackDamage);
            direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector2.right;
            targetPosition = targetWorldPosition;
            aliveTime = 0f;
            active = true;
            damagedTargets.Clear();

            if (hitCollider != null)
            {
                hitCollider.enabled = true;
                hitCollider.isTrigger = true;
            }

            OnInitialized();
        }

        protected virtual void Update()
        {
            if (!active)
                return;

            aliveTime += Time.deltaTime;
            if (aliveTime >= Mathf.Max(0.01f, lifetime))
                Release();
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        protected abstract void OnInitialized();

        protected virtual void TryDamage(Collider2D other)
        {
            if (!active || other == null)
                return;

            if (damageSource != null && other.transform.root == damageSource.transform.root)
                return;

            if ((targetLayer.value & (1 << other.gameObject.layer)) == 0)
                return;

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive || damagedTargets.Contains(damageable))
                return;

            damagedTargets.Add(damageable);
            damageable.TakeDamage(damage, damageSource);

            if (releaseOnFirstHit)
                Release();
        }

        protected void Release()
        {
            active = false;

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            if (owner != null)
                owner.ReleaseLocalShot(gameObject);
            else
                gameObject.SetActive(false);
        }

        protected void ResolveReferences()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();
        }
    }
}
