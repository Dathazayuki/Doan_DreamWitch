using DreamKnight.Systems.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.UI
{
    public class PlayerHudPotionUI : MonoBehaviour
    {
        [SerializeField] private HealingPotionEquipSO equipState;
        [SerializeField] private Image[] slotImages;
        [SerializeField] private Sprite emptySprite;

        private void OnEnable()
        {
            if (equipState != null)
                equipState.OnEquipmentChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (equipState != null)
                equipState.OnEquipmentChanged -= Refresh;
        }

        public void Refresh()
        {
            if (slotImages == null || slotImages.Length == 0)
                return;

            int slotCount = equipState != null ? equipState.SlotCount : 0;

            for (int i = 0; i < slotImages.Length; i++)
            {
                Image image = slotImages[i];
                if (image == null)
                    continue;

                ItemDefinitionSO item = (equipState != null && i < slotCount) ? equipState.GetSlotItem(i) : null;
                image.sprite = item != null ? item.Icon : emptySprite;
            }
        }
    }
}
