using DreamKnight.Systems.Combat;
using DreamKnight.Systems.Skill;
using DreamKnight.Systems.SkillTree;
using DreamKnight.Player.States;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerCollision : MonoBehaviour
    {
        [Header("Damage / Hit Reaction")]
        [SerializeField] private float hitKnockbackX = 5f;
        [SerializeField] private float hitKnockbackY = 2.2f;
        [SerializeField] private float invincibleDuration = 1.2f;
        [SerializeField] private float blinkInterval = 0.08f;
        [SerializeField] private float blinkAlpha = 0.35f;

        private PlayerController controller;
        private PlayerMovement playerMovement;
        private PlayerStats playerStats;
        private SpriteRenderer spriteRenderer;

        private bool isInvincible;
        private Coroutine invincibleCoroutine;

        public void Initialize(PlayerController owner, PlayerMovement movement, PlayerStats stats, SpriteRenderer renderer)
        {
            controller = owner;
            playerMovement = movement;
            playerStats = stats;
            spriteRenderer = renderer;
        }

        public void ReceiveDamage(float damage, GameObject damageSource = null)
        {
            if (playerStats == null || !playerStats.IsAlive) return;
            if (isInvincible) return;

            // Miễn sát thương ngay sau khi spawn qua cửa (SceneTransitionManager cấp)
            // Chặn toàn bộ: không damage, không HitState, không knockback
            if (playerStats.IsSpawnImmune)
            {
                return;
            }

            if (IsSkillTreeDashInvincible())
            {
                return;
            }

            PlayerSpellShield shield = controller != null ? controller.GetComponent<PlayerSpellShield>() : GetComponent<PlayerSpellShield>();
            if (shield != null && shield.TryBlockDamage(damage, damageSource))
            {
                return;
            }

            // KIỂM TRA GUARD COLLIDER (Chặn đòn)
            if (damageSource != null && controller != null && controller.FormManager != null)
            {
                Collider2D guardCol = controller.FormManager.ActiveGuardCollider;
                if (guardCol != null && guardCol.gameObject.activeInHierarchy)
                {
                    // Nếu một trong các collider của nguồn sát thương đang chạm vào Guard Collider
                    Collider2D[] sourceCols = damageSource.GetComponentsInChildren<Collider2D>();
                    foreach (var srcCol in sourceCols)
                    {
                        if (srcCol.gameObject.activeInHierarchy && srcCol.IsTouching(guardCol))
                        {
                            Debug.Log($"[PlayerCollision] Damage blocked by Guard Collider! Source: {damageSource.name}");
                            return; // Khởi tạo hiệu ứng Block ở đây nếu cần, sau đó bỏ qua sát thương.
                        }
                    }
                }
            }

            bool isTransformed = controller != null && controller.IsTransformed;
            playerStats.TakeDamage(damage, !isTransformed);
            DamageTextService.ShowPlayerDamage(damage, ResolveDamageTextPosition());

            if (isTransformed && playerStats.CurrentHealth <= 0f)
            {
                controller?.HandleTransformedFormDepleted();
                return;
            }

            if (!playerStats.IsAlive)
                return;

            if (controller != null)
            {
                PlayerState hitState = controller.GetHitStateForCurrentForm();
                controller.StateMachine.ChangeState(hitState);
            }

            if (damageSource != null && playerMovement != null)
            {
                float direction = Mathf.Sign(transform.position.x - damageSource.transform.position.x);
                if (Mathf.Abs(direction) < 0.01f)
                    direction = transform.localScale.x >= 0f ? 1f : -1f;

                playerMovement.SetVelocity(new Vector2(direction * hitKnockbackX, hitKnockbackY));
            }

            StartInvincibility();

            Debug.Log($"Player took {damage} damage! Current HP: {playerStats.CurrentHealth}");
        }

        private bool IsSkillTreeDashInvincible()
        {
            SkillTreeManager manager = SkillTreeManager.Instance;
            return manager != null
                && manager.IsDashInvincibleUnlocked()
                && playerMovement != null
                && playerMovement.IsDashing;
        }

        private Vector3 ResolveDamageTextPosition()
        {
            if (spriteRenderer != null)
                return spriteRenderer.bounds.center;

            // Dùng collider từ form đang active
            var formManager = GetComponent<PlayerFormManager>();
            if (formManager != null && formManager.ActiveBodyCollider != null)
            {
                return formManager.ActiveBodyCollider.bounds.center;
            }

            // Fallback
            Collider2D collider2D = GetComponent<Collider2D>();
            if (collider2D == null)
                collider2D = GetComponentInChildren<Collider2D>();
            if (collider2D != null)
                return collider2D.bounds.center;

            return transform.position;
        }

        private void StartInvincibility()
        {
            if (invincibleCoroutine != null)
                StopCoroutine(invincibleCoroutine);

            invincibleCoroutine = StartCoroutine(InvincibilityCoroutine());
        }

        /// <summary>
        /// Hủy bỏ trạng thái vô hiệu hóa sát thương (dùng khi Player respawn).
        /// </summary>
        public void CancelInvincibility()
        {
            if (invincibleCoroutine != null)
            {
                StopCoroutine(invincibleCoroutine);
                invincibleCoroutine = null;
            }

            isInvincible = false;

            // Khôi phục alpha sprite nếu đang bị làm mờ trong lc blink
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f;
                spriteRenderer.color = c;
            }
        }

        private System.Collections.IEnumerator InvincibilityCoroutine()
        {
            if (spriteRenderer == null)
            {
                isInvincible = true;
                yield return new WaitForSeconds(invincibleDuration);
                isInvincible = false;
                invincibleCoroutine = null;
                yield break;
            }

            isInvincible = true;
            float timer = 0f;
            Color baseColor = spriteRenderer.color;

            while (timer < invincibleDuration)
            {
                Color c = spriteRenderer.color;
                c.a = blinkAlpha;
                spriteRenderer.color = c;
                yield return new WaitForSeconds(blinkInterval);
                timer += blinkInterval;

                c = spriteRenderer.color;
                c.a = baseColor.a;
                spriteRenderer.color = c;
                yield return new WaitForSeconds(blinkInterval);
                timer += blinkInterval;
            }

            Color restore = spriteRenderer.color;
            restore.a = baseColor.a;
            spriteRenderer.color = restore;

            isInvincible = false;
            invincibleCoroutine = null;
        }
    }
}
