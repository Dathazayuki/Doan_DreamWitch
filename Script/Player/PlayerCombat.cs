using System.Collections.Generic;
using DreamKnight.Interfaces;
using DreamKnight.Player.States;
using DreamKnight.Systems.Combat;
using DreamKnight.Systems.SkillTree;
using Mv;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerCombat : MonoBehaviour
    {
        private struct DamageCandidate
        {
            public IDamageable Damageable;
            public MvEnemyBase EnemyBase;
            public Collider2D BlockedHitCollider;
            public bool IsBlocked;
        }

        [Header("Combat")]
        [Tooltip("Damage cho từng đòn trong combo (index 0 = đòn 1, index 1 = đòn 2, ...). Nếu đòn vượt quá mảng thì dùng giá trị cuối cùng.")]
        [SerializeField] private float[] comboDamagePerStep = { 20f, 22f, 30f };
        [SerializeField] private float upAttackDamage = 25f;
        [SerializeField] private float heavyStrikeDamage = 50f;

        [SerializeField] private LayerMask attackTargetMask = ~0;
        [Tooltip("Nếu bật, chỉ gây damage lên các Enemy (có MvEnemyBase hoặc MvDamage). Tắt nếu muốn tấn công cả object khác.")]
        [SerializeField] private bool attackEnemyOnly = true;

        [Header("Block Recoil")]
        [SerializeField] private bool applyRecoilWhenBlocked = true;
        [SerializeField] private float blockedRecoilX = 2.2f;
        [SerializeField] private float blockedRecoilY = 0f;
        [SerializeField] private float blockedRecoilDuration = 0.08f;
        [Header("Mana On Hit")]
        [SerializeField] private float manaRestoreOnHit = 10f;

        private PlayerController controller;
        private PlayerMovement movement;
        private float blockedRecoilTimer;
        private Vector2 blockedRecoilVelocity;
        private int currentComboStep = 1;
        private bool isHeavyStrike;
        private bool isUpAttack;
        private bool lastCalculatedDamageWasCritical;
        private float facilityBasicAttackDamageBonus;
        // Backup của giá trị Inspector gốc để restore khi quay lại Human form
        private float[] defaultComboDamagePerStep;
        private float defaultUpAttackDamage;
        private float defaultHeavyStrikeDamage;
        // Tham chiếu đến PlayerFormManager để lấy hitbox của form đang active
        private PlayerFormManager playerFormManager;

        public void Initialize(PlayerController owner, PlayerMovement playerMovement)
        {
            controller = owner;
            movement = playerMovement;
            playerFormManager = owner.GetComponent<PlayerFormManager>();
            // Lưu lại giá trị mặc định từ Inspector để có thể reset sau
            defaultComboDamagePerStep = (float[])comboDamagePerStep.Clone();
            defaultUpAttackDamage = upAttackDamage;
            defaultHeavyStrikeDamage = heavyStrikeDamage;
        }

        /// <summary>
        /// Gọi từ AttackState trước khi animation hit event xảy ra.
        /// Cho phép PlayerCombat biết đang ở đòn thứ mấy để tính đúng damage.
        /// </summary>
        public void SetCurrentComboStep(int step, bool heavyStrike = false, bool upAttack = false)
        {
            currentComboStep = Mathf.Max(1, step);
            isHeavyStrike = heavyStrike;
            isUpAttack = upAttack;
        }

        /// <summary>
        /// Áp dụng thông số combat của form hiện tại.
        /// Gọi từ PlayerController khi chuyển form.
        /// </summary>
        public void ApplyFormProfile(FormCombatProfile profile)
        {
            if (profile == null)
            {
                ResetToDefaultProfile();
                return;
            }

            if (profile.comboDamagePerStep != null && profile.comboDamagePerStep.Length > 0)
                comboDamagePerStep = profile.comboDamagePerStep;

            upAttackDamage = profile.upAttackDamage;
            heavyStrikeDamage = profile.heavyStrikeDamage;
        }

        /// <summary>
        /// Khôi phục thông số mặc định (dùng khi quay lại Human form).
        /// Các giá trị được lấy lại từ defaultComboDamagePerStep đã lưu lúc khởi tạo.
        /// </summary>
        public void ResetToDefaultProfile()
        {
            comboDamagePerStep = defaultComboDamagePerStep;
            upAttackDamage = defaultUpAttackDamage;
            heavyStrikeDamage = defaultHeavyStrikeDamage;
        }

        public void SetFacilityBasicAttackDamageBonus(float damageBonus)
        {
            facilityBasicAttackDamageBonus = Mathf.Max(0f, damageBonus);
        }

        public float GetCurrentDamage()
        {
            lastCalculatedDamageWasCritical = false;
            float baseDamage = 20f;
            if (isHeavyStrike) baseDamage = heavyStrikeDamage;
            else if (isUpAttack) baseDamage = upAttackDamage;
            else if (comboDamagePerStep != null && comboDamagePerStep.Length > 0)
            {
                int index = Mathf.Clamp(currentComboStep - 1, 0, comboDamagePerStep.Length - 1);
                baseDamage = comboDamagePerStep[index];
            }

            baseDamage += facilityBasicAttackDamageBonus;

            if (controller != null && controller.CurrentFormId == PlayerFormId.Human)
            {
                var stats = controller.Stats;
                float critChance = 0f;
                float critDamageBonus = 0f;

                if (stats != null && stats.ActiveSpellBook != null)
                {
                    // Apply basic attack damage multiplier (e.g. Red Grimoire)
                    baseDamage *= stats.ActiveSpellBook.basicAttackDamageMultiplier;
                    critChance += stats.ActiveSpellBook.critChance;
                    critDamageBonus += stats.ActiveSpellBook.critDamageMultiplierBonus;
                }

                SkillTreeManager skillTreeManager = SkillTreeManager.Instance;
                if (skillTreeManager != null)
                {
                    critChance += skillTreeManager.GetCriticalRateBonus();
                    critDamageBonus += skillTreeManager.GetCriticalDamageBonus();
                }

                // Apply critical strike chance (e.g. Gold Grimoire + Skill Tree)
                if (critChance > 0f && Random.value < Mathf.Clamp01(critChance))
                {
                    baseDamage *= (1.5f + critDamageBonus);
                    lastCalculatedDamageWasCritical = true;
                    Debug.Log("[PlayerCombat] Critical Hit! Damage: " + baseDamage);
                }
            }

            return baseDamage;
        }

        public bool OnAttackHit()
        {
            if (controller == null || !controller.IsAlive) return false;

            if (controller.StateMachine?.CurrentState is IAttackTriggerHandler attackHandler)
            {
                if (attackHandler.OnAttackHitTriggered())
                {
                    return true;
                }
            }

            float damage = GetCurrentDamage();
            HashSet<IDamageable> uniqueTargets = new HashSet<IDamageable>();

            // Lấy hitbox từ PlayerFormManager
            Collider2D activeNormal = null, activeUpper = null, activeHeavy = null;
            if (playerFormManager != null)
            {
                playerFormManager.TryGetActiveHitboxes(out activeNormal, out activeUpper, out activeHeavy);
            }

            // Chọn hitbox phù hợp theo loại đòn
            Collider2D hitbox;
            if (isUpAttack)
                hitbox = activeUpper != null ? activeUpper : activeNormal;
            else if (isHeavyStrike)
                hitbox = activeHeavy != null ? activeHeavy : activeNormal;
            else
                hitbox = activeNormal;

            if (hitbox == null)
            {
                Debug.LogWarning("[PlayerCombat] Hitbox chưa được gán trong Inspector!");
                return false;
            }

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = attackTargetMask;
            filter.useTriggers = true; // MvDamage HurtBox là trigger → phải bật true để detect được

            Collider2D[] results = new Collider2D[16];
            int count = Physics2D.OverlapCollider(hitbox, filter, results);

            if (count == 0) return false;

            System.Array.Resize(ref results, count);
            return ApplyDamageFromHits(results, uniqueTargets, damage);
        }

        private void FixedUpdate()
        {
            if (blockedRecoilTimer <= 0f || movement == null)
                return;

            blockedRecoilTimer -= Time.fixedDeltaTime;
            movement.SetVelocity(new Vector2(blockedRecoilVelocity.x, movement.Velocity.y));
        }

        public void DrawAttackGizmos(bool isPlaying)
        {
            if (playerFormManager == null) return;
            if (playerFormManager.TryGetActiveHitboxes(out Collider2D normal, out Collider2D upper, out Collider2D heavy))
            {
                DrawHitboxGizmo(normal, Color.red);
                DrawHitboxGizmo(upper, Color.yellow);
                DrawHitboxGizmo(heavy, new Color(1f, 0.5f, 0f)); // cam
            }
        }

        private static void DrawHitboxGizmo(Collider2D col, Color color)
        {
            if (col == null) return;
            Gizmos.color = color;

            if (col is BoxCollider2D box)
            {
                Vector3 center = col.transform.TransformPoint(box.offset);
                Vector3 size = new Vector3(box.size.x * col.transform.lossyScale.x,
                                           box.size.y * col.transform.lossyScale.y, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }
            else if (col is CircleCollider2D circle)
            {
                Vector3 center = col.transform.TransformPoint(circle.offset);
                Gizmos.DrawWireSphere(center, circle.radius * Mathf.Max(
                    col.transform.lossyScale.x, col.transform.lossyScale.y));
            }
            else if (col is CapsuleCollider2D capsule)
            {
                Vector3 center = col.transform.TransformPoint(capsule.offset);
                Gizmos.DrawWireSphere(center, capsule.size.x * 0.5f);
            }
        }

        private bool ApplyDamageFromHits(Collider2D[] hitColliders, HashSet<IDamageable> uniqueTargets, float damage)
        {
            bool hitAny = false;
            if (hitColliders == null || uniqueTargets == null) return false;

            Dictionary<IDamageable, DamageCandidate> candidates = new Dictionary<IDamageable, DamageCandidate>();

            for (int i = 0; i < hitColliders.Length; i++)
            {
                Collider2D hitCol = hitColliders[i];
                if (hitCol == null) continue;
                if (hitCol.transform.IsChildOf(controller.transform)) continue;

                IDamageable damageable = hitCol.GetComponent<IDamageable>();
                if (damageable == null)
                    damageable = hitCol.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive) continue;

                // Không cho phép Player tự gây damage cho chính mình (kể cả qua reflection)
                if (damageable is PlayerController) continue;

                MvEnemyBase enemyBase = hitCol.GetComponentInParent<MvEnemyBase>();

                // Nếu bật attackEnemyOnly: bỏ qua các object không phải Enemy
                // (doors, shrines, traps, interact prompts...)
                if (attackEnemyOnly && enemyBase == null)
                {
                    // Cho phép các IDamageable không phải PlayerController nhưng có MvDamage
                    MvDamage mvDamage = hitCol.GetComponentInParent<MvDamage>();
                    if (mvDamage == null) continue;
                }
                IDamageable uniqueKey = enemyBase != null ? enemyBase : damageable;

                if (!candidates.TryGetValue(uniqueKey, out DamageCandidate candidate))
                {
                    candidate = new DamageCandidate
                    {
                        Damageable = damageable,
                        EnemyBase = enemyBase,
                        BlockedHitCollider = null,
                        IsBlocked = false
                    };
                }
                else if (candidate.Damageable == candidate.EnemyBase && damageable != enemyBase)
                {
                    candidate.Damageable = damageable;
                }

                if (enemyBase != null && !enemyBase.CanReceiveDamage(damage, gameObject, hitCol))
                {
                    candidate.IsBlocked = true;
                    candidate.BlockedHitCollider = hitCol;
                }

                candidates[uniqueKey] = candidate;
            }

            foreach (KeyValuePair<IDamageable, DamageCandidate> pair in candidates)
            {
                IDamageable uniqueKey = pair.Key;
                DamageCandidate candidate = pair.Value;

                if (!uniqueTargets.Add(uniqueKey))
                    continue;

                if (candidate.IsBlocked)
                {
                    candidate.EnemyBase?.OnDamageBlocked(damage, gameObject, candidate.BlockedHitCollider);
                    ApplyBlockedRecoil(candidate.BlockedHitCollider);
                    continue;
                }

                if (candidate.Damageable == null || !candidate.Damageable.IsAlive)
                    continue;

                if (lastCalculatedDamageWasCritical)
                    DamageTextService.MarkNextEnemyDamageCritical();

                candidate.Damageable.TakeDamage(damage, gameObject);

                float skillTreeExtraDamage = GetSkillTreeComboThirdHitExtraDamage();
                if (skillTreeExtraDamage > 0f && candidate.Damageable.IsAlive)
                    candidate.Damageable.TakeDamage(skillTreeExtraDamage, gameObject);

                // Restore mana to player when a normal attack successfully damages an enemy
                if (controller != null)
                {
                    var stats = controller.Stats;
                    float restoreAmt = manaRestoreOnHit;
                    restoreAmt += GetSkillTreeComboThirdHitManaRestoreBonus();
                    if (stats != null && controller.CurrentFormId == PlayerFormId.Human && stats.ActiveSpellBook != null)
                    {
                        restoreAmt += stats.ActiveSpellBook.manaRegenPerHitBonus;
                    }
                    if (restoreAmt > 0f)
                    {
                        stats?.RestoreMana(restoreAmt);
                    }
                }
                hitAny = true;
            }

            return hitAny;
        }

        private float GetSkillTreeComboThirdHitManaRestoreBonus()
        {
            if (!IsNormalComboThirdHit())
                return 0f;

            SkillTreeManager manager = SkillTreeManager.Instance;
            return manager != null ? manager.GetComboThirdHitManaRestoreBonus() : 0f;
        }

        private float GetSkillTreeComboThirdHitExtraDamage()
        {
            if (!IsNormalComboThirdHit())
                return 0f;

            SkillTreeManager manager = SkillTreeManager.Instance;
            return manager != null ? manager.GetComboThirdHitExtraDamage() : 0f;
        }

        private bool IsNormalComboThirdHit()
        {
            return !isHeavyStrike && !isUpAttack && currentComboStep == 3;
        }

        private void ApplyBlockedRecoil(Collider2D blockedHitCollider)
        {
            if (!applyRecoilWhenBlocked) return;
            if (movement == null) return;

            float direction = 0f;
            if (blockedHitCollider != null)
                direction = Mathf.Sign(transform.position.x - blockedHitCollider.bounds.center.x);

            if (Mathf.Abs(direction) < 0.001f)
                direction = movement.FacingRight ? -1f : 1f;

            float recoilX = Mathf.Max(0f, blockedRecoilX);
            float recoilY = Mathf.Max(0f, blockedRecoilY);
            blockedRecoilVelocity = new Vector2(direction * recoilX, recoilY);
            blockedRecoilTimer = Mathf.Max(0f, blockedRecoilDuration);
            movement.SetVelocity(blockedRecoilVelocity);
        }
    }
}
