using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DreamKnight.Player;

namespace DreamKnight.Systems.Skill
{
    public class SpellSkillView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedImage;

        public void Bind(SpellData spell, int level, bool isSelected, Action onSelected)
        {
            if (iconImage != null)
                iconImage.sprite = spell != null ? spell.icon : null;

            if (levelText != null)
                levelText.text = level > 0 ? $"Lv.{level}" : string.Empty;

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

        public void Bind(SpellBookSO book, bool isEquipped, bool isSelected, Action onSelected)
        {
            if (iconImage != null)
                iconImage.sprite = book != null ? book.icon : null;

            if (levelText != null)
                levelText.text = isEquipped ? "EQUIP" : string.Empty;

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
