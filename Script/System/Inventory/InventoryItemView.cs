using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace DreamKnight.Systems.Inventory
{
    public class InventoryItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedImage;

        public void Bind(ItemDefinitionSO item, int quantity, bool isSelected, Action onSelected)
        {
            if (iconImage != null)
                iconImage.sprite = item != null ? item.Icon : null;

            if (quantityText != null)
                quantityText.text = quantity > 0 ? $"x{quantity}" : string.Empty;

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
        }
    }
}
