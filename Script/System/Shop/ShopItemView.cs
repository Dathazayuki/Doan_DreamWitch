using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DreamKnight.Systems.Inventory;

namespace DreamKnight.Systems.Shop
{
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI stockText;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedImage;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyButtonLabel;

        public void Bind(ItemDefinitionSO item, int stock, int price, bool isSelected, Action onSelected, bool canBuy, Action onBuyClicked)
        {
            if (iconImage != null)
                iconImage.sprite = item != null ? item.Icon : null;

            if (nameText != null)
                nameText.text = item != null ? item.DisplayName : string.Empty;

            if (priceText != null)
                priceText.text = price.ToString();

            if (stockText != null)
                stockText.text = stock > 0 ? $"Stock: {stock}" : "Sold out";

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
                    buyButton.interactable = canBuy && stock > 0;
                }
                else
                {
                    buyButton.interactable = false;
                }
            }

            if (buyButtonLabel != null)
                buyButtonLabel.text = "Buy";
        }
    }
}
