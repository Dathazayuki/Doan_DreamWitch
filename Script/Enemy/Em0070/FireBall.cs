using UnityEngine;
using DreamKnight.Interfaces;

namespace DreamKnight.Enemy
{
    [DisallowMultipleComponent]
    public class FireBall : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 4f;

        [Header("Damage")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private LayerMask targetLayer;

        public float Damage => damage;

        private GameObject owner;
        private Vector2 direction;
        private float timer;

        // Pooling fields
        private FireBallPoolManager poolManager;
        private int sourcePrefabId;
        private bool isPooled;

        public void InitializePool(FireBallPoolManager manager, int prefabId)
        {
            poolManager = manager;
            sourcePrefabId = prefabId;
            isPooled = true;
        }

        public void Initialize(GameObject ownerObject, float damageAmount, Vector2 travelDirection)
        {
            owner = ownerObject;
            damage = damageAmount;
            direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector2.right;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            timer = 0f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                ReleaseOrDestroy();
                return;
            }

            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (owner != null && other.transform.root == owner.transform.root) return;

            if (((1 << other.gameObject.layer) & targetLayer.value) != 0)
            {
                IDamageable damageable = other.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.TakeDamage(damage, owner);
                    ReleaseOrDestroy();
                }
            }
        }

        public void ReleaseOrDestroy()
        {
            if (isPooled && poolManager != null)
            {
                poolManager.Release(this, sourcePrefabId);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
