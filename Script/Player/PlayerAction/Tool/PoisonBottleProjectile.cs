using System;
using System.Collections.Generic;
using System.Collections;
using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PoisonBottleProjectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D hitCollider;
        [SerializeField] private GameObject bottleIconObject;
        [SerializeField] private GameObject explosionObject;
        [SerializeField] private bool rotateAlongDirection = true;

        private static readonly Collider2D[] overlapResults = new Collider2D[32];

        private PoisonBottleBehaviourSO data;
        private GameObject owner;
        private Action<PoisonBottleProjectile> releaseToPool;
        private Vector2 direction;
        private float aliveTime;
        private bool activeFlight;
        private bool impactProcessed;
        private Coroutine releaseRoutine;
        private RigidbodyType2D originalBodyType;
        private bool hasOriginalBodyType;

        private void Awake()
        {
            ResolveReferences();
            CacheOriginalBodyType();
        }

        private void OnDisable()
        {
            if (releaseRoutine != null)
            {
                StopCoroutine(releaseRoutine);
                releaseRoutine = null;
            }

            activeFlight = false;
            impactProcessed = false;
            aliveTime = 0f;
            data = null;
            owner = null;
            releaseToPool = null;

            if (hitCollider != null)
                hitCollider.enabled = true;

            if (bottleIconObject != null)
                bottleIconObject.SetActive(true);

            if (explosionObject != null)
                explosionObject.SetActive(false);

            if (rb != null)
            {
                rb.bodyType = originalBodyType;
                rb.linearVelocity = Vector2.zero;
            }
        }

        public void Launch(
            PoisonBottleBehaviourSO behaviourData,
            GameObject ownerObject,
            Vector2 travelDirection,
            Action<PoisonBottleProjectile> releaseCallback)
        {
            ResolveReferences();
            enabled = true;

            data = behaviourData;
            owner = ownerObject;
            releaseToPool = releaseCallback;
            direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector2.right;
            aliveTime = 0f;
            activeFlight = true;
            impactProcessed = false;
            if (releaseRoutine != null)
            {
                StopCoroutine(releaseRoutine);
                releaseRoutine = null;
            }

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

            if (bottleIconObject != null)
                bottleIconObject.SetActive(true);

            if (explosionObject != null)
                explosionObject.SetActive(false);

            if (rb != null)
            {
                CacheOriginalBodyType();
                rb.bodyType = originalBodyType;
                rb.linearVelocity = new Vector2(direction.x * GetProjectileSpeed(), GetArcUpVelocity());
            }
        }

        private void Update()
        {
            if (!activeFlight)
                return;

            aliveTime += Time.deltaTime;
            if (aliveTime >= GetProjectileLifetime())
            {
                Release();
                return;
            }

            if (rb == null)
                transform.position += (Vector3)(direction * GetProjectileSpeed() * Time.deltaTime);
            else if (rotateAlongDirection && rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!activeFlight || impactProcessed || other == null)
                return;

            if (owner != null && other.transform.root == owner.transform.root)
                return;

            if (!IsEnemyLayer(other.gameObject.layer) && !IsGroundLayer(other.gameObject.layer))
                return;

            if (IsEnemyLayer(other.gameObject.layer))
            {
                IDamageable damageable = ResolveDamageable(other, out MonoBehaviour _);
                if (damageable == null || !damageable.IsAlive)
                    return;
            }
            else if (!IsGroundLayer(other.gameObject.layer))
            {
                return;
            }

            Impact();
        }

        private void Impact()
        {
            impactProcessed = true;
            activeFlight = false;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Static;
            }

            if (hitCollider != null)
                hitCollider.enabled = false;

            if (bottleIconObject != null)
                bottleIconObject.SetActive(false);

            PlayExplosionObject();
            ApplyPoisonInRadius();
            releaseRoutine = StartCoroutine(ReleaseAfterDelay(GetReleaseDelayAfterImpact()));
        }

        private void PlayExplosionObject()
        {
            if (explosionObject == null)
                return;

            explosionObject.SetActive(false);
            explosionObject.SetActive(true);

            ParticleSystem[] particleSystems = explosionObject.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];
                if (ps == null)
                    continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }

        private void ApplyPoisonInRadius()
        {
            if (data == null)
                return;

            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useLayerMask = true;
            filter.layerMask = data.EnemyLayer;

            int count = Physics2D.OverlapCircle(transform.position, data.ImpactRadius, filter, overlapResults);
            HashSet<IDamageable> affected = new HashSet<IDamageable>();

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = overlapResults[i];
                if (hit == null)
                    continue;

                if (owner != null && hit.transform.root == owner.transform.root)
                    continue;

                IDamageable damageable = ResolveDamageable(hit, out MonoBehaviour host);
                if (damageable == null || !damageable.IsAlive || host == null)
                    continue;

                if (!affected.Add(damageable))
                    continue;

                PoisonStatusEffect poison = host.GetComponent<PoisonStatusEffect>();
                if (poison == null)
                    poison = host.gameObject.AddComponent<PoisonStatusEffect>();

                poison.Apply(damageable, owner, data.PoisonTickCount, data.PoisonDamagePerTick, data.PoisonTickInterval);
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

        private void Release()
        {
            activeFlight = false;
            releaseRoutine = null;

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            Action<PoisonBottleProjectile> callback = releaseToPool;
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

        private void CacheOriginalBodyType()
        {
            if (hasOriginalBodyType || rb == null)
                return;

            originalBodyType = rb.bodyType;
            hasOriginalBodyType = true;
        }

        private bool IsEnemyLayer(int layer)
        {
            return data != null && (data.EnemyLayer.value & (1 << layer)) != 0;
        }

        private bool IsGroundLayer(int layer)
        {
            return data != null && (data.GroundLayer.value & (1 << layer)) != 0;
        }

        private IEnumerator ReleaseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Release();
        }

        private float GetProjectileSpeed() => data != null ? Mathf.Max(0.1f, data.ProjectileSpeed) : 10f;
        private float GetArcUpVelocity() => data != null ? data.ArcUpVelocity : 5f;
        private float GetProjectileLifetime() => data != null ? Mathf.Max(0.1f, data.ProjectileLifetime) : 2f;
        private float GetReleaseDelayAfterImpact() => data != null ? data.ReleaseDelayAfterImpact : 1f;
    }
}
