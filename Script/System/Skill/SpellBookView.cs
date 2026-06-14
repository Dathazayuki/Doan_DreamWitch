using System;
using UnityEngine;
using UnityEngine.UI;
using DreamKnight.Player;

namespace DreamKnight.Systems.Skill
{
    public class SpellBookView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedImage;

        public void Bind(SpellBookSO book, bool isSelected, Action onSelected)
        {
            if (iconImage != null)
                iconImage.sprite = book != null ? book.icon : null;

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
