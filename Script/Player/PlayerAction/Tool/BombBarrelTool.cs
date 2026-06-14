using System;
using System.Collections;
using System.Collections.Generic;
using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class BombBarrelTool : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Collider2D explosionCollider;
        [SerializeField] private GameObject imageObject;
        [SerializeField] private GameObject explosionVfxObject;

        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        private BombBarrelBehaviourSO data;
        private GameObject owner;
        private Action<BombBarrelTool> releaseToPool;
        private Coroutine routine;
        private bool exploding;
        private RigidbodyType2D originalBodyType;
        private bool hasOriginalBodyType;

        private void Awake()
        {
            ResolveReferences();
            CacheOriginalBodyType();
            ResetVisualState();
        }

        private void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            exploding = false;
            damagedTargets.Clear();
            data = null;
            owner = null;
            releaseToPool = null;

            if (rb != null)
            {
                rb.bodyType = originalBodyType;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            ResetVisualState();
        }

        public void Place(BombBarrelBehaviourSO behaviourData, GameObject ownerObject, Action<BombBarrelTool> releaseCallback)
        {
            ResolveReferences();
            CacheOriginalBodyType();

            data = behaviourData;
            owner = ownerObject;
            releaseToPool = releaseCallback;
            exploding = false;
            damagedTargets.Clear();

            if (rb != null)
            {
                rb.bodyType = originalBodyType;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            ResetVisualState();

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(BombRoutine());
        }

        private IEnumerator BombRoutine()
        {
            float fuse = data != null ? data.FuseDuration : 2f;
            yield return new WaitForSeconds(fuse);

            Explode();

            float duration = data != null ? data.ExplosionDuration : 1f;
            float timer = 0f;
            while (timer < duration)
            {
                DamageEnemiesInExplosion();
                timer += Time.deltaTime;
                yield return null;
            }

            routine = null;
            Release();
        }

        private void Explode()
        {
            exploding = true;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Static;
            }

            if (bodyCollider != null)
                bodyCollider.enabled = false;

            if (imageObject != null)
                imageObject.SetActive(false);

            if (explosionCollider != null)
            {
                explosionCollider.enabled = true;
                explosionCollider.isTrigger = true;
            }

            PlayExplosionVfx();
            DamageEnemiesInExplosion();
        }

        private void DamageEnemiesInExplosion()
        {
            if (!exploding || explosionCollider == null || data == null)
                return;

            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useLayerMask = true;
            filter.layerMask = data.EnemyLayer;

            Collider2D[] results = new Collider2D[Mathf.Max(8, data.ExplosionOverlapBufferSize)];
            int count = explosionCollider.Overlap(filter, results);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = results[i];
                if (hit == null)
                    continue;

                if (owner != null && hit.transform.root == owner.transform.root)
                    continue;

                IDamageable damageable = ResolveDamageable(hit, out _);
                if (damageable == null || !damageable.IsAlive || damagedTargets.Contains(damageable))
                    continue;

                damagedTargets.Add(damageable);
                damageable.TakeDamage(data.ExplosionDamage, owner);
            }
        }

        private IDamageable ResolveDamageable(Collider2D hit, out MonoBehaviour host)
        {
            host = null;

            Mv.MvEnemyBase enemy = hit.GetComponentInParent<Mv.MvEnemyBase>();
            if (enemy != null)
            {
                host = enemy;
                return enemy;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            host = damageable as MonoBehaviour;
            return damageable;
        }

        private void PlayExplosionVfx()
        {
            if (explosionVfxObject == null)
                return;

            explosionVfxObject.SetActive(false);
            explosionVfxObject.SetActive(true);

            ParticleSystem[] particleSystems = explosionVfxObject.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];
                if (ps == null)
                    continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }

        private void Release()
        {
            Action<BombBarrelTool> callback = releaseToPool;
            if (callback != null)
                callback.Invoke(this);
            else
                gameObject.SetActive(false);
        }

        private void ResetVisualState()
        {
            if (bodyCollider != null)
            {
                bodyCollider.enabled = true;
                bodyCollider.isTrigger = false;
            }

            if (explosionCollider != null)
            {
                explosionCollider.enabled = false;
                explosionCollider.isTrigger = true;
            }

            if (imageObject != null)
                imageObject.SetActive(true);

            if (explosionVfxObject != null)
                explosionVfxObject.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

            if (bodyCollider == null)
                bodyCollider = GetComponent<Collider2D>();
        }

        private void CacheOriginalBodyType()
        {
            if (hasOriginalBodyType || rb == null)
                return;

            originalBodyType = rb.bodyType;
            hasOriginalBodyType = true;
        }
    }
}
