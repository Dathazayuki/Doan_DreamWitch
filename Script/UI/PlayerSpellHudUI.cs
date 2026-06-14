using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DreamKnight.Systems.Skill;

namespace DreamKnight.UI
{
    [DisallowMultipleComponent]
    public class PlayerSpellHudUI : MonoBehaviour
    {
        [SerializeField] private SpellEquipSO spellEquip;
        [SerializeField] private SpellUseSystem spellUseSystem;
        [SerializeField] private SpellManager spellManager;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownFillImage;
        [SerializeField] private TextMeshProUGUI levelText;

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (spellUseSystem == null)
                spellUseSystem = FindAnyObjectByType<SpellUseSystem>();
            
            if (spellManager == null)
                spellManager = FindAnyObjectByType<SpellManager>();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (spellEquip != null)
                spellEquip.OnEquipmentChanged += UpdateDisplay;

            if (spellUseSystem != null)
                spellUseSystem.OnSpellUsed += HandleSpellUsed;

            UpdateDisplay();
        }

        private void OnDisable()
        {
            if (spellEquip != null)
                spellEquip.OnEquipmentChanged -= UpdateDisplay;

            if (spellUseSystem != null)
                spellUseSystem.OnSpellUsed -= HandleSpellUsed;
        }

        private void Update()
        {
            ResolveReferences();

            if (spellEquip == null || spellUseSystem == null || spellManager == null) return;
            
            var spell = spellEquip.EquippedSpell;
            if (spell == null)
            {
                if (iconImage != null) iconImage.enabled = false;
                if (cooldownFillImage != null) cooldownFillImage.enabled = false;
                if (levelText != null) levelText.text = "";
                return;
            }

            // Update icon
            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = spell.icon;
            }

            // Get current spell level and cooldown
            int currentLevel = spellManager.GetLevel(spell);
            float totalCooldown = spell.GetCooldown(currentLevel);
            float remainingCooldown = spellUseSystem.GetRemainingCooldown(spell);

            // Update cooldown radial fill
            if (cooldownFillImage != null)
            {
                cooldownFillImage.enabled = true;
                // fillAmount: 1 = full, 0 = empty. Show fill depleting as cooldown counts down
                cooldownFillImage.fillAmount = remainingCooldown > 0f ? remainingCooldown / totalCooldown : 1f;
            }

            // Update level text
            if (levelText != null)
            {
                levelText.text = "LV" + currentLevel.ToString();
            }
        }

        private void UpdateDisplay()
        {
            var spell = spellEquip != null ? spellEquip.EquippedSpell : null;
            if (iconImage != null)
            {
                if (spell != null && spell.icon != null)
                {
                    iconImage.sprite = spell.icon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }
        }

        private void HandleSpellUsed(string spellId, float cooldownEnd)
        {
            // optional: immediate update
        }
    }
}
