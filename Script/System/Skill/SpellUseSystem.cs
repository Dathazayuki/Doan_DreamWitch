using System;
using System.Collections.Generic;
using UnityEngine;
using DreamKnight.Player;
using DreamKnight.Systems.SkillTree;

namespace DreamKnight.Systems.Skill
{
    [DisallowMultipleComponent]
    public class SpellUseSystem : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SpellManager spellManager;
        [SerializeField] private SpellEquipSO spellEquip;
        [SerializeField] private PlayerStats playerStats;

        // cooldown end times by spellId
        private readonly Dictionary<string, float> cooldownEndTimes = new Dictionary<string, float>();

        public event Action<string, float> OnSpellUsed; // spellId, cooldownEndTime

        private void Awake()
        {
            if (playerStats == null)
                playerStats = GetComponentInParent<PlayerStats>();
        }

        public void Configure(SpellManager manager, SpellEquipSO equip, PlayerStats stats)
        {
            if (manager != null)
                spellManager = manager;

            if (equip != null)
                spellEquip = equip;

            if (stats != null)
                playerStats = stats;
        }

        public static SpellUseSystem GetOrCreate(
            Transform owner,
            SpellManager manager,
            SpellEquipSO equip,
            PlayerStats stats)
        {
            if (owner == null)
                return null;

            SpellUseSystem existing = owner.GetComponentInChildren<SpellUseSystem>(true);
            if (existing == null)
            {
                GameObject host = new GameObject("SpellUseSystem_Auto");
                host.transform.SetParent(owner, false);
                existing = host.AddComponent<SpellUseSystem>();
            }

            existing.Configure(manager, equip, stats);
            return existing;
        }

        public bool TryUseEquippedSpell()
        {
            if (spellEquip == null || spellManager == null || playerStats == null)
                return false;

            var spell = spellEquip.EquippedSpell;
            if (spell == null) return false;

            int level = spellManager.GetLevel(spell);
            if (level <= 0) return false;

            if (!spell.TryGetLevelData(level, out SpellLevelData levelData))
                return false;

            string id = spell.GetStableId();
            float now = Time.time;
            if (cooldownEndTimes.TryGetValue(id, out float end) && now < end)
                return false; // still cooling down

            if (spell.castType == SpellCastType.Shield && HasActiveShield())
                return false;

            float manaCost = spell.GetManaCost(level);
            if (manaCost > 0f && !playerStats.UseMana(manaCost))
                return false;

            SpellLevelData activeLevelData = levelData;
            float cooldown = spell.GetCooldown(level);

            var playerController = playerStats != null ? playerStats.GetComponent<PlayerController>() : null;
            if (playerController != null && playerController.CurrentFormId == PlayerFormId.Human)
            {
                float damageMultiplier = 1f;
                var spellBook = playerStats.ActiveSpellBook;
                if (spellBook != null)
                {
                    if (spellBook.skillDamageMultiplier > 0.001f)
                        damageMultiplier *= spellBook.skillDamageMultiplier;

                    cooldown *= (1f - spellBook.skillCooldownReduction);
                }

                SkillTreeManager skillTreeManager = SkillTreeManager.Instance;
                if (skillTreeManager != null && skillTreeManager.IsSpellBookSpellDamageUnlocked())
                    damageMultiplier *= 1f + skillTreeManager.GetSpellBookSpellDamageBonus();

                if (Mathf.Abs(damageMultiplier - 1f) > 0.001f)
                {
                    activeLevelData = new SpellLevelData
                    {
                        upgradePrice = levelData.upgradePrice,
                        damage = levelData.damage * damageMultiplier,
                        cooldown = levelData.cooldown,
                        manaCost = levelData.manaCost,
                        shieldBlockCount = levelData.shieldBlockCount
                    };
                }
            }

            CastSpellPrefab(spell, activeLevelData);

            float cooldownEnd = now + cooldown;
            cooldownEndTimes[id] = cooldownEnd;
            OnSpellUsed?.Invoke(id, cooldownEnd);

            return true;
        }

        private bool HasActiveShield()
        {
            if (playerStats == null)
                return false;

            PlayerSpellShield shield = playerStats.GetComponent<PlayerSpellShield>();
            return shield != null && shield.IsActive;
        }

        private void CastSpellPrefab(SpellData spell, SpellLevelData levelData)
        {
            if (spell == null || playerStats == null)
                return;

            GameObject caster = playerStats.gameObject;
            if (spell.castType == SpellCastType.Shield)
            {
                ActivateShield(spell, levelData, caster);
                return;
            }

            if (spell.prefab == null)
                return;

            if (spell.castType == SpellCastType.ForwardProjectile)
            {
                SpawnForwardProjectile(spell, levelData, caster);
                return;
            }

            if (spell.castType == SpellCastType.BigSword)
            {
                SpawnBigSword(spell, levelData, caster);
                return;
            }

            GameObject go = Instantiate(spell.prefab, playerStats.transform.position, Quaternion.identity);
            ISpellCast castComp = go.GetComponent<ISpellCast>();
            if (castComp == null)
                castComp = go.GetComponentInChildren<ISpellCast>();

            castComp?.Cast(caster, levelData);
        }

        private void SpawnForwardProjectile(SpellData spell, SpellLevelData levelData, GameObject caster)
        {
            ForwardSpellProjectile projectilePrefab = spell.prefab.GetComponent<ForwardSpellProjectile>();
            if (projectilePrefab == null)
                projectilePrefab = spell.prefab.GetComponentInChildren<ForwardSpellProjectile>(true);

            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[SpellUseSystem] ForwardProjectile spell '{spell.GetStableId()}' needs a ForwardSpellProjectile component on its prefab.");
                return;
            }

            Vector2 direction = ResolveForwardDirection(caster);
            Vector2 offset = spell.projectileSpawnOffset;
            offset.x *= direction.x >= 0f ? 1f : -1f;

            Vector3 spawnPosition = playerStats.transform.position + (Vector3)offset;
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            int maxPoolSize = Mathf.Max(1, spell.projectileMaxPoolSize);

            ForwardSpellProjectile projectile = SpellProjectilePoolManager.Instance.Spawn(
                projectilePrefab,
                spawnPosition,
                rotation,
                maxPoolSize);

            if (projectile == null)
                return;

            projectile.Launch(
                caster,
                levelData,
                direction,
                spell,
                p => SpellProjectilePoolManager.Instance.Release(p, maxPoolSize));
        }

        private void SpawnBigSword(SpellData spell, SpellLevelData levelData, GameObject caster)
        {
            BigSwordSpellProjectile projectilePrefab = spell.prefab.GetComponent<BigSwordSpellProjectile>();
            if (projectilePrefab == null)
                projectilePrefab = spell.prefab.GetComponentInChildren<BigSwordSpellProjectile>(true);

            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[SpellUseSystem] BigSword spell '{spell.GetStableId()}' needs a BigSwordSpellProjectile component on its prefab.");
                return;
            }

            Vector2 direction = ResolveForwardDirection(caster);
            Vector2 offset = spell.bigSwordSpawnOffset;
            offset.x *= direction.x >= 0f ? 1f : -1f;

            Vector3 spawnPosition = playerStats.transform.position + (Vector3)offset;
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            int maxPoolSize = Mathf.Max(1, spell.bigSwordMaxPoolSize);

            BigSwordSpellProjectile projectile = BigSwordSpellPoolManager.Instance.Spawn(
                projectilePrefab,
                spawnPosition,
                rotation);

            if (projectile == null)
                return;

            projectile.Launch(
                caster,
                levelData,
                direction,
                spell,
                p => BigSwordSpellPoolManager.Instance.Release(p, maxPoolSize));
        }

        private void ActivateShield(SpellData spell, SpellLevelData levelData, GameObject caster)
        {
            if (caster == null)
                return;

            PlayerSpellShield shield = PlayerSpellShield.GetOrCreate(caster.transform);
            if (shield == null)
                return;

            int blockCount = levelData != null ? Mathf.Max(0, levelData.shieldBlockCount) : 0;
            shield.Activate(blockCount, spell.shieldVisualPrefab);
        }

        private Vector2 ResolveForwardDirection(GameObject caster)
        {
            if (caster != null)
            {
                PlayerController player = caster.GetComponent<PlayerController>();
                if (player != null && player.Movement != null)
                    return player.Movement.FacingRight ? Vector2.right : Vector2.left;

                PlayerMovement movement = caster.GetComponent<PlayerMovement>();
                if (movement != null)
                    return movement.FacingRight ? Vector2.right : Vector2.left;

                return caster.transform.localScale.x < 0f ? Vector2.left : Vector2.right;
            }

            return Vector2.right;
        }

        public float GetRemainingCooldown(SpellData spell)
        {
            if (spell == null) return 0f;
            string id = spell.GetStableId();
            if (cooldownEndTimes.TryGetValue(id, out float end))
            {
                return Mathf.Max(0f, end - Time.time);
            }
            return 0f;
        }
    }
}
