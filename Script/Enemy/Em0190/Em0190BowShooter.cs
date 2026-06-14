using DreamKnight.Player;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class Em0190BowShooter : MonoBehaviour
    {
        [Header("Arrow")]
        [SerializeField] private Em0190Arrow arrowPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float arrowSpeed = 10f;
        [SerializeField] private float arrowDamage = 10f;

        [Header("Fallback Shoot")]
        [SerializeField] private bool shootOnAttackStartFallback = true;
        [SerializeField] private float fallbackShootDelay = 0.12f;

        private MvEnemyBase owner;
        private MvAttack attack;
        private PlayerController cachedPlayer;
        private float lastHandledAttackTime = -999f;

        private void Awake()
        {
            owner = GetComponent<MvEnemyBase>();
            if (owner == null)
                owner = GetComponentInParent<MvEnemyBase>();

            attack = GetComponent<MvAttack>();
            if (attack == null)
                attack = GetComponentInParent<MvAttack>();

            cachedPlayer = FindFirstObjectByType<PlayerController>();
        }

        private void Update()
        {
            TryFallbackShootFromAttackStart();
        }

        private void TryFallbackShootFromAttackStart()
        {
            if (!shootOnAttackStartFallback) return;
            if (attack == null) return;

            float attackStartTime = attack.LastAttackTime;
            if (attackStartTime <= -100f) return;
            if (attackStartTime <= lastHandledAttackTime) return;
            if (Time.time < attackStartTime + Mathf.Max(0f, fallbackShootDelay)) return;

            lastHandledAttackTime = attackStartTime;
            ShootArrow();
        }

        private void ShootArrow()
        {
            if (arrowPrefab == null)
                return;

            Transform spawn = firePoint != null ? firePoint : transform;
            
            // XÓA BỎ LỖI BẮN NGƯỢC DO KHOẢNG CÁCH:
            // Ép buộc hướng bắn phải khóa chặt vào "hướng mặt" của con Quái (owner), không phụ thuộc vào vị trí Player hay nòng súng nữa.
            Transform rootTransform = owner != null ? owner.transform : transform;
            
            // (Nếu cung vẫn bị bắn lùi ra sau gáy con quái, hãy đổi chỗ Vector2.right và Vector2.left ở 2 dòng dưới)
            Vector2 direction = rootTransform.localScale.x >= 0f ? Vector2.right : Vector2.left;
            if (direction.sqrMagnitude < 0.0001f)
                direction = rootTransform.localScale.x >= 0f ? Vector2.right : Vector2.left;

            Em0190Arrow arrow = Instantiate(arrowPrefab, spawn.position, Quaternion.identity);
            arrow.Launch(direction.normalized, arrowSpeed, arrowDamage, owner != null ? owner.gameObject : gameObject);
        }
    }
}
