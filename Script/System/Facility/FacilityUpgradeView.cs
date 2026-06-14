using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.Systems.Facility
{
    public class FacilityUpgradeView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image levelFillImage;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedImage;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeButtonLabel;

        public void Bind(
            FacilityUpgradeSO upgrade,
            int level,
            int maxLevel,
            int price,
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

            if (priceText != null)
                priceText.text = !isMaxLevel && price > 0 ? price.ToString() : string.Empty;

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
