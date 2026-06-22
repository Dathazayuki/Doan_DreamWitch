using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DreamKnight.Systems.Facility
{
    public class FacilityUpgradeView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image levelFillImage;
        [SerializeField] private TextMeshProUGUI levelText;
        [FormerlySerializedAs("priceText")]
        [SerializeField] private TextMeshProUGUI materialText;
        [FormerlySerializedAs("priceIconImage")]
        [SerializeField] private Image materialIconImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedImage;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeButtonLabel;

        public void Bind(
            FacilityUpgradeSO upgrade,
            int level,
            int maxLevel,
            Sprite requiredItemIcon,
            int requiredQuantity,
            int currentQuantity,
            bool isSelected,
            Action onSelected,
            bool canUpgrade,
            Action onUpgradeClicked)
        {
            if (iconImage != null)
                iconImage.sprite = upgrade != null ? upgrade.Icon : null;

            if (nameText != null)
                nameText.text = upgrade != null ? upgrade.DisplayName : string.Empty;

            if (levelFillImage != null)
                levelFillImage.fillAmount = maxLevel > 0 ? Mathf.Clamp01((float)level / maxLevel) : 0f;

            bool isMaxLevel = maxLevel > 0 && level >= maxLevel;

            if (levelText != null)
                levelText.text = maxLevel > 0 ? $"Lv {level}/{maxLevel}" : $"Lv {level}";

            if (materialText != null)
                materialText.text = !isMaxLevel && requiredQuantity > 0 ? $"{requiredQuantity}/{currentQuantity}" : string.Empty;

            if (materialIconImage != null)
            {
                materialIconImage.sprite = requiredItemIcon;
                materialIconImage.enabled = !isMaxLevel && requiredItemIcon != null;
            }

            if (selectedImage != null)
                selectedImage.SetActive(isSelected);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                if (onSelected != null)
                {
                    selectButton.onClick.AddListener(() => onSelected());
                    selectButton.interactable = true;
                }
                else
                {
                    selectButton.interactable = false;
                }
            }

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                if (onUpgradeClicked != null)
                    upgradeButton.onClick.AddListener(() => onUpgradeClicked());

                upgradeButton.interactable = canUpgrade;
            }

            if (upgradeButtonLabel != null)
                upgradeButtonLabel.text = isMaxLevel ? "Max" : "Upgrade";
        }
    }
}
