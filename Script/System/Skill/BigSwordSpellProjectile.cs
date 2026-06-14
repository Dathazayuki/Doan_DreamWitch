using System;
using System.Collections.Generic;
using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    [DisallowMultipleComponent]
    public class BigSwordSpellProjectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D hitCollider;
        [SerializeField] private bool rotateAlongDirection = true;
        [SerializeField] private LayerMask targetLayer = ~0;

        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        private GameObject caster;
        private Action<BigSwordSpellProjectile> releaseToPool;
        private Vector2 direction;
        private float damage;
        private float speed;
        private float lifetime;
        private float aliveTime;
        private bool activeFlight;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            activeFlight = false;
            aliveTime = 0f;
            caster = null;
            releaseToPool = null;
            damagedTargets.Clear();

            if (hitCollider != null)
                hitCollider.enabled = true;

            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        public void Launch(
            GameObject casterObject,
            SpellLevelData levelData,
            Vector2 travelDirection,
            SpellData spellData,
            Action<BigSwordSpellProjectile> releaseCallback)
        {
            ResolveReferences();
            enabled = true;

            caster = casterObject;
            releaseToPool = releaseCallback;
            direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector2.right;
            damage = levelData != null ? Mathf.Max(0f, levelData.damage) : 0f;
            speed = spellData != null ? Mathf.Max(0.1f, spellData.bigSwordSpeed) : 10f;
            lifetime = spellData != null ? Mathf.Max(0.1f, spellData.bigSwordLifetime) : 1f;
            aliveTime = 0f;
            activeFlight = true;
            damagedTargets.Clear();

            if (rotateAlongDirection)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (hitCollider != null)
            {
                hitCollider.enabled = true;
                hitCollider.isTrigger = true;
            }

            if (rb != null)
                rb.linearVelocity = direction * speed;
        }

        private void Update()
        {
            if (!activeFlight)
                return;

            aliveTime += Time.deltaTime;
            if (aliveTime >= lifetime)
            {
                Release();
                return;
            }

            if (rb == null)
                transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider2D other)
        {
            if (!activeFlight || other == null)
                return;

            if (caster != null && other.transform.root == caster.transform.root)
                return;

            if ((targetLayer.value & (1 << other.gameObject.layer)) == 0)
                return;

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive || damagedTargets.Contains(damageable))
                return;

            damagedTargets.Add(damageable);
            damageable.TakeDamage(damage, caster);
        }

        private void Release()
        {
            activeFlight = false;

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            Action<BigSwordSpellProjectile> callback = releaseToPool;
            if (callback != null)
                callback.Invoke(this);
            else
                gameObject.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();
        }
    }
}
