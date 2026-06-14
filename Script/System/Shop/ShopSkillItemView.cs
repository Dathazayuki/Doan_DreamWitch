using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DreamKnight.Systems.Skill;

namespace DreamKnight.Systems.Shop
{
    public class ShopSkillItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image lockIconImage;
        [SerializeField] private Image levelFillImage;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedImage;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyButtonLabel;

        public void Bind(SpellData spell, int level, int maxLevel, int price, bool isSelected, Action onSelected, bool canBuy, Action onBuyClicked)
        {
            if (iconImage != null)
                iconImage.sprite = spell != null ? spell.icon : null;

            if (nameText != null)
                nameText.text = spell != null ? spell.spellName : string.Empty;

            bool unlocked = level > 0;

            if (lockIconImage != null)
                lockIconImage.gameObject.SetActive(!unlocked);

            float fill = 0f;
            if (maxLevel > 0 && level > 0)
                fill = Mathf.Clamp01((float)level / maxLevel);

            if (levelFillImage != null)
                levelFillImage.fillAmount = fill;

            if (levelText != null)
            {
                if (!unlocked)
                    levelText.text = "Locked";
                else if (maxLevel > 0)
                    levelText.text = $"Lv {level}/{maxLevel}";
                else
                    levelText.text = $"Lv {level}";
            }

            if (priceText != null)
                priceText.text = price > 0 ? price.ToString() : string.Empty;

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

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                if (onBuyClicked != null)
                {
                    buyButton.onClick.AddListener(() => onBuyClicked());
                    buyButton.interactable = canBuy;
                }
                else
                {
                    buyButton.interactable = false;
                }
            }

            if (buyButtonLabel != null)
                buyButtonLabel.text = unlocked ? "Upgrade" : "Buy";
        }
    }
}
