using System.Collections.Generic;
using DreamKnight.Systems.Interaction;
using DreamKnight.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.Systems.Skill
{
    public class StatusUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SpellManager spellManager;
        [SerializeField] private SpellEquipSO spellEquip;
        [SerializeField] private PlayerStats playerStats;

        [Header("View - Spells")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private SpellSkillView skillViewPrefab;

        [Header("View - Books")]
        [SerializeField] private Transform bookContentRoot;
        [SerializeField] private SpellBookView bookViewPrefab;

        [Header("View - General")]
        [SerializeField] private GameObject emptyStateObject;

        [Header("Detail Panel")]
        [SerializeField] private Image selectedSpellIcon;
        [SerializeField] private TextMeshProUGUI selectedSpellNameText;
        [SerializeField] private TextMeshProUGUI selectedSpellDescriptionText;
        [SerializeField] private TextMeshProUGUI playerStatsDisplayText;

        [Header("Detail Stats")]
        [SerializeField] private TextMeshProUGUI levelDisplayText;
        [SerializeField] private TextMeshProUGUI damageDisplayText;
        [SerializeField] private TextMeshProUGUI manaCostDisplayText;
        [SerializeField] private TextMeshProUGUI cooldownDisplayText;

        [Header("Buttons")]
        [SerializeField] private Button equipButton;
        [SerializeField] private TextMeshProUGUI equipButtonLabelText;
        [SerializeField] private Button unequipButton;
        [SerializeField] private TextMeshProUGUI unequipButtonLabelText;

        private readonly List<SpellSkillView> spawnedSpellViews = new List<SpellSkillView>();
        private readonly List<SpellBookView> spawnedBookViews = new List<SpellBookView>();
        private SpellData selectedSpell;
        private SpellBookSO selectedBook;
        private bool lastStorageNear;

        private void Awake()
        {
            if (spellManager == null)
                spellManager = FindAnyObjectByType<SpellManager>();

            if (spellEquip == null)
            {
                spellEquip = Resources.Load<SpellEquipSO>("SpellEquip");
            }

            GetPlayerStats();
        }

        private void OnEnable()
        {
            Subscribe();
            lastStorageNear = Storage.IsPlayerNear;
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            PlayerStats stats = GetPlayerStats();

            if (spellManager != null)
                spellManager.OnSpellProgressChanged += HandleSpellProgressChanged;

            if (spellEquip != null)
                spellEquip.OnEquipmentChanged += Refresh;

            if (stats != null)
                stats.OnSpellBooksChanged += Refresh;

            if (equipButton != null)
                equipButton.onClick.AddListener(EquipSelectedSpell);

            if (unequipButton != null)
                unequipButton.onClick.AddListener(UnequipSelectedSpell);
        }

        private void Unsubscribe()
        {
            PlayerStats stats = GetPlayerStats();

            if (spellManager != null)
                spellManager.OnSpellProgressChanged -= HandleSpellProgressChanged;

            if (spellEquip != null)
                spellEquip.OnEquipmentChanged -= Refresh;

            if (stats != null)
                stats.OnSpellBooksChanged -= Refresh;

            if (equipButton != null)
                equipButton.onClick.RemoveListener(EquipSelectedSpell);

            if (unequipButton != null)
                unequipButton.onClick.RemoveListener(UnequipSelectedSpell);
        }

        private void HandleSpellProgressChanged(string spellId, int level)
        {
            Refresh();
        }

        private void LateUpdate()
        {
            bool storageNear = Storage.IsPlayerNear;
            if (storageNear == lastStorageNear)
                return;

            lastStorageNear = storageNear;
            RefreshDetailPanel();
        }

        private PlayerStats GetPlayerStats()
        {
            if (playerStats == null)
            {
                playerStats = FindAnyObjectByType<PlayerStats>();
            }
            return playerStats;
        }

        public void Refresh()
        {
            ClearViews();

            // 1. Fetch unlocked spells
            List<SpellData> unlockedSpells = new List<SpellData>();
            if (spellManager != null)
            {
                var database = spellManager.SpellDatabase;
                if (database != null)
                {
                    IReadOnlyList<SpellData> allSpells = database.Spells;
                    foreach (SpellData spell in allSpells)
                    {
                        if (spellManager.IsUnlocked(spell))
                            unlockedSpells.Add(spell);
                    }
                }
            }

            // 2. Fetch unlocked books
            List<SpellBookSO> unlockedBooks = new List<SpellBookSO>();
            var stats = GetPlayerStats();
            if (stats != null)
            {
                var books = stats.UnlockedSpellBooks;
                if (books != null)
                {
                    foreach (var book in books)
                    {
                        if (book != null)
                            unlockedBooks.Add(book);
                    }
                }
            }

            // Check if currently selected items are still valid
            if (selectedSpell != null && !unlockedSpells.Contains(selectedSpell))
            {
                selectedSpell = null;
            }
            if (selectedBook != null && !unlockedBooks.Contains(selectedBook))
            {
                selectedBook = null;
            }

            // 3. Populate Spells
            if (contentRoot != null && skillViewPrefab != null)
            {
                for (int i = 0; i < unlockedSpells.Count; i++)
                {
                    SpellData spell = unlockedSpells[i];
                    SpellSkillView view = Instantiate(skillViewPrefab, contentRoot);
                    spawnedSpellViews.Add(view);

                    int level = spellManager != null ? spellManager.GetLevel(spell) : 0;
                    bool isSelected = selectedSpell == spell;

                    view.Bind(spell, level, isSelected, () => SelectSpell(spell));
                }
            }

            // 4. Populate Books
            if (bookContentRoot != null && bookViewPrefab != null)
            {
                for (int i = 0; i < unlockedBooks.Count; i++)
                {
                    SpellBookSO book = unlockedBooks[i];
                    SpellBookView view = Instantiate(bookViewPrefab, bookContentRoot);
                    spawnedBookViews.Add(view);

                    bool isSelected = selectedBook == book;

                    view.Bind(book, isSelected, () => SelectBook(book));
                }
            }

            // 5. Handle empty state UI
            int totalItems = unlockedSpells.Count + unlockedBooks.Count;
            if (emptyStateObject != null)
            {
                emptyStateObject.SetActive(totalItems == 0);
            }

            if (totalItems == 0)
            {
                selectedSpell = null;
                selectedBook = null;
                RefreshDetailPanel();
                return;
            }

            // 6. Auto-select first item if nothing is currently selected
            if (selectedSpell == null && selectedBook == null)
            {
                if (unlockedSpells.Count > 0)
                {
                    SelectSpell(unlockedSpells[0]);
                }
                else if (unlockedBooks.Count > 0)
                {
                    SelectBook(unlockedBooks[0]);
                }
            }
            else
            {
                RefreshDetailPanel();
            }
        }

        private void ClearViews()
        {
            for (int i = spawnedSpellViews.Count - 1; i >= 0; i--)
            {
                if (spawnedSpellViews[i] != null)
                    Destroy(spawnedSpellViews[i].gameObject);
            }
            spawnedSpellViews.Clear();

            for (int i = spawnedBookViews.Count - 1; i >= 0; i--)
            {
                if (spawnedBookViews[i] != null)
                    Destroy(spawnedBookViews[i].gameObject);
            }
            spawnedBookViews.Clear();
        }

        private void SelectSpell(SpellData spell)
        {
            selectedSpell = spell;
            selectedBook = null;
            Refresh();
        }

        private void SelectBook(SpellBookSO book)
        {
            selectedBook = book;
            selectedSpell = null;
            Refresh();
        }

        private void RefreshDetailPanel()
        {
            var stats = GetPlayerStats();

            if (selectedSpell != null)
            {
                // Icon
                if (selectedSpellIcon != null)
                    selectedSpellIcon.sprite = selectedSpell.icon;

                // Name
                if (selectedSpellNameText != null)
                    selectedSpellNameText.text = selectedSpell.spellName;

                // Description
                if (selectedSpellDescriptionText != null)
                    selectedSpellDescriptionText.text = selectedSpell.description;

                // Get current level stats
                int currentLevel = spellManager != null ? spellManager.GetLevel(selectedSpell) : 0;
                float damage = selectedSpell.GetDamage(currentLevel);
                float manaCost = selectedSpell.GetManaCost(currentLevel);
                float cooldown = selectedSpell.GetCooldown(currentLevel);

                // Level
                if (levelDisplayText != null)
                    levelDisplayText.text = $"Lv. {currentLevel}";

                // Damage
                if (damageDisplayText != null)
                    damageDisplayText.text = $"{damage:F1}";

                // Mana Cost
                if (manaCostDisplayText != null)
                    manaCostDisplayText.text = $"{manaCost:F1}";

                // Cooldown
                if (cooldownDisplayText != null)
                    cooldownDisplayText.text = $"{cooldown:F2}s";

                bool canManageEquipment = Storage.IsPlayerNear;

                // Show equip buttons for Spells only near Storage
                if (equipButton != null)
                    equipButton.gameObject.SetActive(canManageEquipment);
                if (unequipButton != null)
                    unequipButton.gameObject.SetActive(canManageEquipment);

                // Update button states
                bool isEquipped = spellEquip != null && spellEquip.EquippedSpell == selectedSpell;
                if (equipButton != null)
                    equipButton.interactable = canManageEquipment && !isEquipped;
                if (unequipButton != null)
                    unequipButton.interactable = canManageEquipment && isEquipped;

                // Update button labels
                if (equipButtonLabelText != null)
                    equipButtonLabelText.text = isEquipped ? "Equipped" : "Equip";
                if (unequipButtonLabelText != null)
                    unequipButtonLabelText.text = "Unequip";
            }
            else if (selectedBook != null)
            {
                // Icon
                if (selectedSpellIcon != null)
                    selectedSpellIcon.sprite = selectedBook.icon;

                // Name
                if (selectedSpellNameText != null)
                    selectedSpellNameText.text = selectedBook.displayName;

                // Description
                if (selectedSpellDescriptionText != null)
                    selectedSpellDescriptionText.text = selectedBook.description;

                // Hide spell-specific detail texts
                if (levelDisplayText != null)
                    levelDisplayText.text = "";
                if (damageDisplayText != null)
                    damageDisplayText.text = "";
                if (manaCostDisplayText != null)
                    manaCostDisplayText.text = "";
                if (cooldownDisplayText != null)
                    cooldownDisplayText.text = "";

                // Hide equip/unequip buttons for Books
                if (equipButton != null)
                    equipButton.gameObject.SetActive(false);
                if (unequipButton != null)
                    unequipButton.gameObject.SetActive(false);
            }
            else
            {
                ClearDetailPanel();
            }

            // Always update Player Stats separate text field
            RefreshPlayerStatsText();
        }

        private void RefreshPlayerStatsText()
        {
            if (playerStatsDisplayText == null)
                return;

            var stats = GetPlayerStats();
            if (stats == null)
            {
                playerStatsDisplayText.text = "Player Stats: PlayerStats data not found.";
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<b><color=#00FFFF>Current Player Stats:</color></b>");
            sb.AppendLine($"• Health (HP): <color=#32CD32>{stats.MaxHealth:F0}</color>");
            sb.AppendLine($"• Mana (MP): <color=#1E90FF>{stats.MaxMana:F0}</color>");
            sb.AppendLine($"• Move Speed: <color=#FF8C00>{stats.MoveSpeed:F1}</color>");

            float defense = 0f;
            float critChance = 0f;
            float critDmg = 1.5f;
            float skillMult = 1f;
            float attackMult = 1f;
            float cdRed = 0f;

            if (stats.ActiveSpellBook != null)
            {
                defense = stats.ActiveSpellBook.defenseBonus;
                critChance = stats.ActiveSpellBook.critChance * 100f;
                critDmg = 1.5f + stats.ActiveSpellBook.critDamageMultiplierBonus;
                skillMult = stats.ActiveSpellBook.skillDamageMultiplier;
                attackMult = stats.ActiveSpellBook.basicAttackDamageMultiplier;
                cdRed = stats.ActiveSpellBook.skillCooldownReduction * 100f;
            }

            sb.AppendLine($"• Defense: <color=#9370DB>+{defense:F0}</color>");
            sb.AppendLine($"• Basic Attack Damage: <color=#FF4500>x{attackMult * 100:F0}%</color>");
            sb.AppendLine($"• Skill Damage: <color=#FF4500>x{skillMult * 100:F0}%</color>");
            sb.AppendLine($"• Critical Chance: <color=#FFD700>{critChance:F0}%</color>");
            sb.AppendLine($"• Critical Damage: <color=#FFD700>{critDmg * 100:F0}%</color>");
            sb.AppendLine($"• Skill Cooldown Reduction: <color=#00FFFF>{cdRed:F0}%</color>");

            playerStatsDisplayText.text = sb.ToString();
        }

        private void ClearDetailPanel()
        {
            if (selectedSpellIcon != null)
                selectedSpellIcon.sprite = null;
            if (selectedSpellNameText != null)
                selectedSpellNameText.text = "";
            if (selectedSpellDescriptionText != null)
                selectedSpellDescriptionText.text = "";
            if (levelDisplayText != null)
                levelDisplayText.text = "";
            if (damageDisplayText != null)
                damageDisplayText.text = "";
            if (manaCostDisplayText != null)
                manaCostDisplayText.text = "";
            if (cooldownDisplayText != null)
                cooldownDisplayText.text = "";

            if (equipButton != null)
                equipButton.gameObject.SetActive(false);
            if (unequipButton != null)
                unequipButton.gameObject.SetActive(false);
        }

        private void EquipSelectedSpell()
        {
            if (selectedSpell != null)
            {
                if (spellEquip != null)
                {
                    spellEquip.Equip(selectedSpell);
                    Refresh();
                }
            }
        }

        private void UnequipSelectedSpell()
        {
            if (selectedSpell != null)
            {
                if (spellEquip != null && spellEquip.HasSpell())
                {
                    spellEquip.Unequip();
                    Refresh();
                }
            }
        }
    }
}
