using System;
using System.Collections;
using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class KnifeProjectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D hitCollider;
        [SerializeField] private SpriteRenderer knifeVisual;
        [SerializeField] private bool rotateAlongDirection = true;

        [Header("Enemy Layer")]
        [Tooltip("Chọn Layer của Enemy. Dao chỉ dừng + gây damage khi trúng đúng layer này.")]
        [SerializeField] private LayerMask enemyLayer;

        [Header("Embedded Hit VFX")]
        [SerializeField] private GameObject hitVfxObject;
        [SerializeField] private float hitVfxDuration = 0.2f;

        private KnifeThrowBehaviourSO data;
        private GameObject owner;
        private Action<KnifeProjectile> releaseToPool;
        private Vector2 direction;
        private float aliveTime;
        private bool activeFlight;
        private bool hitProcessed;
        private Coroutine hitReleaseRoutine;

        // ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();

            if (knifeVisual == null)
                knifeVisual = GetComponentInChildren<SpriteRenderer>();

            if (hitVfxObject != null)
                hitVfxObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (hitReleaseRoutine != null)
            {
                StopCoroutine(hitReleaseRoutine);
                hitReleaseRoutine = null;
            }

            activeFlight = false;
            hitProcessed = false;
            aliveTime    = 0f;
            data         = null;
            owner        = null;
            releaseToPool = null;

            if (knifeVisual  != null) knifeVisual.enabled  = true;
            if (hitCollider  != null) hitCollider.enabled  = true;
            if (hitVfxObject != null) hitVfxObject.SetActive(false);
            if (rb           != null) rb.linearVelocity    = Vector2.zero;
        }

        // ─────────────────────────────────────────────────────────────────
        public void Launch(KnifeThrowBehaviourSO behaviourData, GameObject ownerObject,
                           Vector2 travelDirection, Action<KnifeProjectile> releaseCallback)
        {
            enabled = true;

            if (hitReleaseRoutine != null)
            {
                StopCoroutine(hitReleaseRoutine);
                hitReleaseRoutine = null;
            }

            data          = behaviourData;
            owner         = ownerObject;
            releaseToPool = releaseCallback;
            direction     = travelDirection.sqrMagnitude > 0.0001f
                            ? travelDirection.normalized : Vector2.right;
            aliveTime     = 0f;
            activeFlight  = true;
            hitProcessed  = false;

            if (rotateAlongDirection)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (knifeVisual  != null) knifeVisual.enabled  = true;
            if (hitCollider  != null) hitCollider.enabled  = true;
            if (hitVfxObject != null) hitVfxObject.SetActive(false);
            if (rb           != null) rb.linearVelocity    = direction * GetProjectileSpeed();
        }

        // ─────────────────────────────────────────────────────────────────
        private void Update()
        {
            if (!activeFlight) return;

            aliveTime += Time.deltaTime;

            // Hết lifetime → trả về pool, không bật VFX
            if (aliveTime >= GetProjectileLifetime())
            {
                Release();
                return;
            }

            // Di chuyển thủ công nếu không có Rigidbody
            if (rb == null)
                transform.position += (Vector3)(direction * GetProjectileSpeed() * Time.deltaTime);
        }

        // ─────────────────────────────────────────────────────────────────
        // Chỉ phản ứng khi trúng đúng Enemy Layer — mọi layer khác bỏ qua
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (!IsEnemyLayer(other.gameObject.layer)) return;

            ProcessHit(other.transform, other.ClosestPoint(transform.position));
        }

        // ─────────────────────────────────────────────────────────────────
        private void ProcessHit(Transform hitTransform, Vector2 hitPoint)
        {
            if (!activeFlight || hitProcessed || hitTransform == null) return;

            // Bỏ qua nếu chạm chính owner
            if (owner != null && hitTransform.root == owner.transform.root) return;

            hitProcessed = true;
            activeFlight = false;

            // Gây damage
            IDamageable damageable = hitTransform.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
                damageable.TakeDamage(GetDamage(), owner);

            // Dừng dao
            if (rb           != null) rb.linearVelocity   = Vector2.zero;
            if (hitCollider  != null) hitCollider.enabled  = false;
            if (knifeVisual  != null) knifeVisual.enabled  = false;

            // Bật VFX rồi trả pool
            bool hasVfx = PlayEmbeddedHitVfx();
            if (hasVfx)
            {
                float delay = Mathf.Max(0.01f, hitVfxDuration);
                hitReleaseRoutine = StartCoroutine(ReleaseAfterDelay(delay));
            }
            else
            {
                Release();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        private bool PlayEmbeddedHitVfx()
        {
            if (hitVfxObject == null) return false;

            hitVfxObject.SetActive(false);
            hitVfxObject.SetActive(true);

            foreach (ParticleSystem ps in hitVfxObject.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps == null) continue;
                ps.Clear(true);
                ps.Play(true);
            }

            return true;
        }

        private IEnumerator ReleaseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            hitReleaseRoutine = null;
            Release();
        }

        private void Release()
        {
            activeFlight = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            Action<KnifeProjectile> callback = releaseToPool;
            if (callback != null)
                callback.Invoke(this);
            else
                gameObject.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────
        private bool IsEnemyLayer(int layer)
        {
            return (enemyLayer.value & (1 << layer)) != 0;
        }

        private float GetDamage()             => data != null ? Mathf.Max(0f,   data.Damage)             : 0f;
        private float GetProjectileSpeed()    => data != null ? Mathf.Max(0.1f, data.ProjectileSpeed)    : 12f;
        private float GetProjectileLifetime() => data != null ? Mathf.Max(0.1f, data.ProjectileLifetime) : 2f;
    }
}
