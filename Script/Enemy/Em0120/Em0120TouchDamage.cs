using System.Collections.Generic;
using DreamKnight.Interfaces;
using DreamKnight.Player;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class Em0120TouchDamage : MonoBehaviour
    {
        [Header("Contact Damage")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float damageInterval = 0.2f;
        [SerializeField] private Collider2D damageTrigger;
        [SerializeField] private bool forceIsTrigger = true;

        private readonly Dictionary<int, float> nextHitTimeByTarget = new Dictionary<int, float>();
        private MvEnemyBase owner;

        private void Awake()
        {
            owner = GetComponentInParent<MvEnemyBase>();

            if (damageTrigger == null)
                damageTrigger = GetComponent<Collider2D>();
            if (damageTrigger == null)
                damageTrigger = GetComponentInChildren<Collider2D>(true);

            if (damageTrigger != null && forceIsTrigger)
                damageTrigger.isTrigger = true;
        }

        private void OnDisable()
        {
            nextHitTimeByTarget.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDealDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDealDamage(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null)
                return;

            int key = other.transform.root.GetInstanceID();
            nextHitTimeByTarget.Remove(key);
        }

        private void TryDealDamage(Collider2D other)
        {
            if (other == null)
                return;
            if (owner != null && !owner.IsAlive)
                return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
                return;
            if (!(damageable is PlayerController))
                return;

            // Chỉ xử lý body collider chính của Player (từ form active)
            // Bỏ qua các child collider (hitbox attack) để tránh nhận damage nhiều lần.
            PlayerFormManager formManager = other.GetComponentInParent<PlayerFormManager>();
            if (formManager != null)
            {
                if (formManager.ActiveBodyColliderObject != other.gameObject)
                    return;
            }
            else
            {
                // Fallback nếu không có FormManager (cấu trúc cũ)
                if (other.transform != other.transform.root)
                    return;
            }

            int key = other.transform.root.GetInstanceID();
            float now = Time.time;
            if (nextHitTimeByTarget.TryGetValue(key, out float nextAllowedTime) && now < nextAllowedTime)
                return;

            nextHitTimeByTarget[key] = now + Mathf.Max(0.01f, damageInterval);
            damageable.TakeDamage(Mathf.Max(0f, damage), owner != null ? owner.gameObject : gameObject);
        }
    }
}
