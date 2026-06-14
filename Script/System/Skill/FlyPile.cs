using System.Collections.Generic;
using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    [DisallowMultipleComponent]
    public class FlyPile : MonoBehaviour, ISpellCast
    {
        [Header("Movement")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float hitDistance = 0.35f;

        [Header("Targeting")]
        [SerializeField] private LayerMask targetLayer = ~0;
        [SerializeField] private float searchRadius = 8f;
        [SerializeField] private float searchInterval = 0.1f;

        private GameObject caster;
        private float damage;
        private IDamageable currentTarget;
        private Collider2D currentTargetCollider;
        
        private float timer;
        private float searchTimer;
        
        private static readonly Collider2D[] searchResults = new Collider2D[16];

        public void Initialize(GameObject casterObject, float damageAmount, Vector2 initialDir)
        {
            caster = casterObject;
            damage = damageAmount;
            
            if (initialDir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(initialDir.y, initialDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            
            timer = 0f;
            searchTimer = searchInterval; // Search immediately on next frame
        }

        public void Cast(GameObject casterObject, SpellLevelData levelData)
        {
            if (casterObject == null)
            {
                Destroy(gameObject);
                return;
            }
            Initialize(casterObject, levelData != null ? Mathf.Max(0f, levelData.damage) : 10f, casterObject.transform.right);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (lifetime > 0f && timer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Target search
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                if (currentTarget == null || !currentTarget.IsAlive)
                {
                    if (FindNearestEnemy(out IDamageable enemy, out Collider2D col))
                    {
                        currentTarget = enemy;
                        currentTargetCollider = col;
                    }
                    else
                    {
                        currentTarget = null;
                        currentTargetCollider = null;
                    }
                }
            }

            // Movement
            if (currentTarget != null && currentTarget.IsAlive)
            {
                Vector2 targetPos = GetTargetPosition(currentTarget, currentTargetCollider);
                Vector2 direction = targetPos - (Vector2)transform.position;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

                if (direction.sqrMagnitude <= hitDistance * hitDistance)
                {
                    currentTarget.TakeDamage(damage, caster);
                    Destroy(gameObject);
                }
            }
            else
            {
                transform.position += transform.right * speed * Time.deltaTime;
            }
        }

        private Vector2 GetTargetPosition(IDamageable damageable, Collider2D preferredCollider)
        {
            if (preferredCollider != null) return preferredCollider.transform.position;
            
            MonoBehaviour behaviour = damageable as MonoBehaviour;
            if (behaviour != null) return behaviour.transform.position;

            return transform.position;
        }

        private bool FindNearestEnemy(out IDamageable nearest, out Collider2D nearestCollider)
        {
            nearest = null;
            nearestCollider = null;

            Vector2 center = transform.position;
            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = targetLayer;
            filter.useTriggers = true;
            
            int count = Physics2D.OverlapCircle(center, searchRadius, filter, searchResults);
            
            if (count == 0) return false;

            float nearestDistanceSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = searchResults[i];
                if (hit == null) continue;

                if (caster != null && hit.transform.root == caster.transform.root) continue;

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                float distSqr = ((Vector2)transform.position - (Vector2)hit.transform.position).sqrMagnitude;
                if (distSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distSqr;
                    nearest = damageable;
                    nearestCollider = hit;
                }
            }

            return nearest != null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (caster != null && other.transform.root == caster.transform.root) return;

            if (((1 << other.gameObject.layer) & targetLayer.value) != 0)
            {
                IDamageable damageable = other.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.TakeDamage(damage, caster);
                    Destroy(gameObject);
                }
            }
        }
    }
}
