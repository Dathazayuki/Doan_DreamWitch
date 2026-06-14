using UnityEngine;
using System;
using DreamKnight.Player;

namespace DreamKnight.Systems.Inventory
{
    [CreateAssetMenu(fileName = "OneTimeWeapon", menuName = "DreamKnight/Inventory/Items/One-Time Weapon")]
    public class OneTimeWeaponItemSO : ItemDefinitionSO
    {
        [Header("Weapon")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private int usesPerPurchase = 1;

        [Header("Throw")]
        [SerializeField] private KnifeProjectile projectilePrefab;
        [SerializeField] private Vector2 throwOffset = new Vector2(0.65f, 0.35f);
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float projectileLifetime = 2f;

        public float Damage => damage;
        public int UsesPerPurchase => usesPerPurchase;
        public KnifeProjectile ProjectilePrefab => projectilePrefab;
        public Vector2 ThrowOffset => throwOffset;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;

        public event Action<OneTimeWeaponItemSO, ItemUseContext> OnUseRequested;

        public override bool Use(ItemUseContext context)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[OneTimeWeaponItemSO] '{name}' missing Projectile Prefab. Use cancelled.");
                return false;
            }

            if (OnUseRequested == null)
            {
                Debug.LogWarning($"[OneTimeWeaponItemSO] '{name}' has no throw handler subscribed. Add OneTimeWeaponThrowSystem and register this weapon.");
                return false;
            }

            OnUseRequested?.Invoke(this, context);
            return true;
        }
    }
}
