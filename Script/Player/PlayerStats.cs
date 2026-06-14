using UnityEngine;
using System;
using System.Collections.Generic;

namespace DreamKnight.Player
{
    /// <summary>
    /// Quản lý stats của Player (HP, Stamina, Movement speeds)
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        private float currentHealth;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float staminaRegenDelay = 1f;
        private float currentStamina;
        private float staminaRegenTimer;

        [Header("Mana")]
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float manaRegenRate = 5f;
        [SerializeField] private float manaRegenDelay = 1f;
        private float currentMana;
        private float manaRegenTimer;
        private float facilityMaxHealthBonus;
        private float facilityMaxManaBonus;

        [Header("Movement Stats")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1f;

        [Header("Wall Climb Stats")]
        [SerializeField] private float wallClimbSpeed = 5f;
        [SerializeField] private float wallSlideSpeed = 2f;
        [SerializeField] private float wallClimbStaminaCost = 15f;

        [Header("Dash Stamina")]
        [SerializeField] private float dashStaminaCost = 20f;

        // Events
        public event Action OnDeath;
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action<float, float> OnManaChanged;
        public event Action<float> OnDamaged;
        public event Action OnSpellBooksChanged;

        // Spawn immunity (set bởi SceneTransitionManager sau khi teleport qua cửa)
        private bool isSpawnImmune;
        public bool IsSpawnImmune => isSpawnImmune;

        public void SetSpawnImmunity(bool immune)
        {
            isSpawnImmune = immune;
        }

        // Properties
        public bool IsAlive => currentHealth > 0;
        public float CurrentHealth => currentHealth;
        public float MaxHealth
        {
            get
            {
                float bonus = 0f;
                if (playerController == null || playerController.CurrentFormId == PlayerFormId.Human)
                {
                    if (activeSpellBook != null)
                    {
                        bonus = activeSpellBook.healthBonus + (maxHealth * activeSpellBook.healthPercentBonus);
                    }
                }
                return maxHealth + facilityMaxHealthBonus + bonus;
            }
        }
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float CurrentMana => currentMana;
        public float MaxMana
        {
            get
            {
                float bonus = 0f;
                if (playerController == null || playerController.CurrentFormId == PlayerFormId.Human)
                {
                    if (activeSpellBook != null)
                    {
                        bonus = activeSpellBook.manaBonus;
                    }
                }
                return maxMana + facilityMaxManaBonus + bonus;
            }
        }
        public float MoveSpeed
        {
            get
            {
                float bonus = 0f;
                if (playerController == null || playerController.CurrentFormId == PlayerFormId.Human)
                {
                    if (activeSpellBook != null)
                    {
                        bonus = moveSpeed * activeSpellBook.moveSpeedPercentBonus;
                    }
                }
                return moveSpeed + bonus;
            }
        }
        public float JumpForce => jumpForce;
        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDuration;
        public float DashCooldown => dashCooldown;
        public float DashStaminaCost => dashStaminaCost;
        public float WallClimbSpeed => wallClimbSpeed;
        public float WallSlideSpeed => wallSlideSpeed;
        public float WallClimbStaminaCost => wallClimbStaminaCost;

        [Header("Spell Book")]
        [SerializeField] private SpellBookSO activeSpellBook;
        public SpellBookSO ActiveSpellBook => activeSpellBook;

        [SerializeField] private List<SpellBookSO> unlockedSpellBooks = new List<SpellBookSO>();
        public List<SpellBookSO> UnlockedSpellBooks => unlockedSpellBooks;

        public void UnlockSpellBook(SpellBookSO book)
        {
            if (book == null) return;
            if (!unlockedSpellBooks.Contains(book))
            {
                unlockedSpellBooks.Add(book);
                OnSpellBooksChanged?.Invoke();
            }
        }

        private PlayerController playerController;

        private void Awake()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentMana = maxMana;
            playerController = GetComponent<PlayerController>();
            if (activeSpellBook != null)
            {
                UnlockSpellBook(activeSpellBook);
            }
        }

        private void Update()
        {
            RegenerateStamina();
            RegenerateHpAndMana();
        }

        #region Health

        public void TakeDamage(float damage)
        {
            TakeDamage(damage, true);
        }

        public void TakeDamage(float damage, bool allowDeath)
        {
            if (!IsAlive) return;
            if (isSpawnImmune)
            {
                return;
            }

            float finalDamage = damage;
            if (playerController == null || playerController.CurrentFormId == PlayerFormId.Human)
            {
                if (activeSpellBook != null)
                {
                    finalDamage = Mathf.Max(0f, damage - activeSpellBook.defenseBonus);
                }
            }

            OnDamaged?.Invoke(finalDamage);
            currentHealth = Mathf.Max(0, currentHealth - finalDamage);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);

            if (allowDeath && currentHealth <= 0)
            {
                Die();
            }
        }

        public void SetHealth(float current, float max)
        {
            float spellFlatBonus = 0f;
            float spellPercentBonus = 0f;
            if (playerController == null || playerController.CurrentFormId == PlayerFormId.Human)
            {
                if (activeSpellBook != null)
                {
                    spellFlatBonus = activeSpellBook.healthBonus;
                    spellPercentBonus = activeSpellBook.healthPercentBonus;
                }
            }

            float divisor = 1f + spellPercentBonus;
            if (divisor > 0.001f)
            {
                maxHealth = Mathf.Max(0.0001f, (max - facilityMaxHealthBonus - spellFlatBonus) / divisor);
            }
            else
            {
                maxHealth = Mathf.Max(0.0001f, max - facilityMaxHealthBonus - spellFlatBonus);
            }

            currentHealth = Mathf.Clamp(current, 0f, MaxHealth);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void ReviveToFullHealth()
        {
            currentHealth = MaxHealth;
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        private void Die()
        {
            ClearSpellBooks();
            OnDeath?.Invoke();
            Debug.Log("Player died!");
        }

        public void SetActiveSpellBook(SpellBookSO spellBook)
        {
            if (spellBook != null)
            {
                UnlockSpellBook(spellBook);
            }

            float oldHealthBonus = 0f;
            if (activeSpellBook != null)
            {
                oldHealthBonus = activeSpellBook.healthBonus + (maxHealth * activeSpellBook.healthPercentBonus);
            }

            activeSpellBook = spellBook;

            float newHealthBonus = 0f;
            if (activeSpellBook != null)
            {
                newHealthBonus = activeSpellBook.healthBonus + (maxHealth * activeSpellBook.healthPercentBonus);
            }

            float hpDiff = newHealthBonus - oldHealthBonus;
            if (hpDiff != 0f)
            {
                currentHealth = Mathf.Clamp(currentHealth + hpDiff, 1f, MaxHealth);
            }

            NotifyStatsChanged();
            OnSpellBooksChanged?.Invoke();
        }

        public void SetFacilityMaxStatBonuses(float maxHealthBonus, float maxManaBonus)
        {
            float oldMaxHealth = MaxHealth;
            float oldMaxMana = MaxMana;

            facilityMaxHealthBonus = Mathf.Max(0f, maxHealthBonus);
            facilityMaxManaBonus = Mathf.Max(0f, maxManaBonus);

            float healthDiff = MaxHealth - oldMaxHealth;
            if (healthDiff > 0f)
                currentHealth = Mathf.Min(MaxHealth, currentHealth + healthDiff);
            else
                currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);

            float manaDiff = MaxMana - oldMaxMana;
            if (manaDiff > 0f)
                currentMana = Mathf.Min(MaxMana, currentMana + manaDiff);
            else
                currentMana = Mathf.Clamp(currentMana, 0f, MaxMana);

            NotifyStatsChanged();
        }

        private void ClearSpellBooks()
        {
            bool changed = activeSpellBook != null || unlockedSpellBooks.Count > 0;
            activeSpellBook = null;
            unlockedSpellBooks.Clear();
            NotifyStatsChanged();

            if (changed)
                OnSpellBooksChanged?.Invoke();
        }

        #endregion

        #region Stamina

        public bool HasEnoughStamina(float amount)
        {
            return currentStamina >= amount;
        }

        public bool UseStamina(float amount)
        {
            if (!HasEnoughStamina(amount)) return false;

            currentStamina = Mathf.Max(0, currentStamina - amount);
            staminaRegenTimer = staminaRegenDelay;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return true;
        }

        public void RestoreStamina(float amount)
        {
            if (amount <= 0f) return;

            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        private void RegenerateStamina()
        {
            if (currentStamina >= maxStamina) return;

            if (staminaRegenTimer > 0)
            {
                staminaRegenTimer -= Time.deltaTime;
                return;
            }

            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        #endregion

        #region Mana

        public bool HasEnoughMana(float amount)
        {
            return currentMana >= amount;
        }

        public bool UseMana(float amount)
        {
            if (!HasEnoughMana(amount)) return false;

            currentMana = Mathf.Max(0, currentMana - amount);
            OnManaChanged?.Invoke(currentMana, MaxMana);
            return true;
        }

        public void RestoreMana(float amount)
        {
            if (amount <= 0f) return;

            currentMana = Mathf.Min(MaxMana, currentMana + amount);
            OnManaChanged?.Invoke(currentMana, MaxMana);
        }

        public void NotifyStatsChanged()
        {
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
            OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
            OnManaChanged?.Invoke(currentMana, MaxMana);
        }

        private void RegenerateHpAndMana()
        {
            if (!IsAlive) return;

            if (playerController == null || playerController.CurrentFormId == PlayerFormId.Human)
            {
                if (activeSpellBook != null)
                {
                    // HP Regen
                    if (activeSpellBook.healthRegenBonus > 0f)
                    {
                        currentHealth = Mathf.Min(MaxHealth, currentHealth + activeSpellBook.healthRegenBonus * Time.deltaTime);
                        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
                    }

                    // Mana Regen
                    if (activeSpellBook.manaRegenBonus > 0f)
                    {
                        currentMana = Mathf.Min(MaxMana, currentMana + activeSpellBook.manaRegenBonus * Time.deltaTime);
                        OnManaChanged?.Invoke(currentMana, MaxMana);
                    }
                }
            }
        }

        #endregion
    }
}
