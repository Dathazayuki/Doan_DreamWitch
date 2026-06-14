using System.Collections.Generic;
using DreamKnight.Player;
using DreamKnight.Systems.Currency;
using DreamKnight.Systems.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.Systems.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private InventoryStateSO inventoryState;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private CurrencyWalletSO currencyWallet;
        [SerializeField] private HealingPotionEquipSO healingPotionEquip;
        [SerializeField] private ToolEquipSO toolEquip;

        [Header("View")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private InventoryItemView itemViewPrefab;
        [SerializeField] private GameObject emptyStateObject;

        [Header("Detail Panel")]
        [SerializeField] private Image selectedItemIcon;
        [SerializeField] private TextMeshProUGUI selectedItemNameText;
        [SerializeField] private TextMeshProUGUI selectedItemDescriptionText;
        [SerializeField] private Button useSelectedItemButton;
        [SerializeField] private TextMeshProUGUI useButtonLabelText;
        [SerializeField] private Button unequipSelectedItemButton;
        [SerializeField] private TextMeshProUGUI unequipButtonLabelText;

        private readonly List<InventoryItemView> spawnedViews = new List<InventoryItemView>();
        private ItemDefinitionSO selectedItem;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (inventoryState != null)
                inventoryState.OnInventoryChanged += Refresh;

            if (currencyWallet != null)
                currencyWallet.OnBalanceChanged += HandleWalletChanged;

            if (healingPotionEquip != null)
                healingPotionEquip.OnEquipmentChanged += Refresh;

            if (toolEquip != null)
                toolEquip.OnEquipmentChanged += Refresh;
        }

        private void Unsubscribe()
        {
            if (inventoryState != null)
                inventoryState.OnInventoryChanged -= Refresh;

            if (currencyWallet != null)
                currencyWallet.OnBalanceChanged -= HandleWalletChanged;

            if (healingPotionEquip != null)
                healingPotionEquip.OnEquipmentChanged -= Refresh;

            if (toolEquip != null)
                toolEquip.OnEquipmentChanged -= Refresh;
        }

        private void HandleWalletChanged(int balance)
        {
            Refresh();
        }

        public void Refresh()
        {
            ClearViews();

            if (inventoryState == null || contentRoot == null || itemViewPrefab == null)
                return;

            IReadOnlyList<InventoryStateSO.InventorySlot> slots = inventoryState.Slots;
            if (slots.Count == 0)
            {
                if (emptyStateObject != null)
                    emptyStateObject.SetActive(true);

                selectedItem = null;
                RefreshDetailPanel();
                return;
            }

            if (emptyStateObject != null)
                emptyStateObject.SetActive(false);

            bool selectedStillExists = false;
            for (int i = 0; i < slots.Count; i++)
            {
                InventoryStateSO.InventorySlot slot = slots[i];
                if (slot == null || slot.item == null || slot.quantity <= 0)
                    continue;

                if (slot.item == selectedItem)
                    selectedStillExists = true;
            }

            if (!selectedStillExists)
            {
                selectedItem = null;
                for (int i = 0; i < slots.Count; i++)
                {
                    InventoryStateSO.InventorySlot slot = slots[i];
                    if (slot != null && slot.item != null && slot.quantity > 0)
                    {
                        selectedItem = slot.item;
                        break;
                    }
                }
            }

            for (int i = 0; i < slots.Count; i++)
            {
                InventoryStateSO.InventorySlot slot = slots[i];
                if (slot == null || slot.item == null || slot.quantity <= 0)
                    continue;

                InventoryItemView view = Instantiate(itemViewPrefab, contentRoot);
                spawnedViews.Add(view);
                ItemDefinitionSO itemForClick = slot.item;
                view.Bind(slot.item, slot.quantity, slot.item == selectedItem, () => SelectItem(itemForClick));
            }

            RefreshDetailPanel();
        }

        private void TryUseItem(ItemDefinitionSO item)
        {
            if (inventoryState == null || item == null)
                return;

            if (item is HealingPotionItemSO)
                return;

            if (item is ToolItemSO)
                return;

            ItemUseContext context = new ItemUseContext(gameObject, inventoryState, currencyWallet, playerStats);
            inventoryState.UseItem(item, context);
        }

        private void TryEquipPotion(ItemDefinitionSO item)
        {
            if (healingPotionEquip == null || inventoryState == null || item == null)
                return;

            if (!healingPotionEquip.HasFreeSlot())
                return;

            if (!inventoryState.RemoveItem(item, 1))
                return;

            if (!healingPotionEquip.TryEquip(item))
                inventoryState.AddItem(item, 1);
        }

        private void TryUnequipPotion(ItemDefinitionSO item)
        {
            if (healingPotionEquip == null || inventoryState == null || item == null)
                return;

            if (!healingPotionEquip.TryUnequip(item))
                return;

            inventoryState.AddItem(item, 1);
        }

        private void TryEquipTool(ItemDefinitionSO item)
        {
            if (toolEquip == null || inventoryState == null || item == null)
                return;

            if (!toolEquip.HasFreeSlot())
                return;

            int quantity = inventoryState.GetQuantity(item);
            if (quantity <= 0)
                return;

            int freeSlots = toolEquip.SlotCount - toolEquip.GetEquippedCount(item);
            int toEquip = Mathf.Min(quantity, Mathf.Max(0, freeSlots));
            if (toEquip <= 0)
                return;

            for (int i = 0; i < toEquip; i++)
            {
                if (!toolEquip.TryEquip(item))
                    break;

                if (!inventoryState.RemoveItem(item, 1))
                {
                    toolEquip.TryUnequip(item);
                    break;
                }
            }
        }

        private void TryUnequipTool(ItemDefinitionSO item)
        {
            if (toolEquip == null || inventoryState == null || item == null)
                return;

            int removed = toolEquip.TryUnequipAll(item);
            if (removed > 0)
                inventoryState.AddItem(item, removed);
        }

        private void SelectItem(ItemDefinitionSO item)
        {
            selectedItem = item;
            Refresh();
        }

        private void RefreshDetailPanel()
        {
            if (selectedItemIcon != null)
                selectedItemIcon.sprite = selectedItem != null ? selectedItem.Icon : null;

            if (selectedItemNameText != null)
                selectedItemNameText.text = selectedItem != null ? selectedItem.DisplayName : string.Empty;

            if (selectedItemDescriptionText != null)
                selectedItemDescriptionText.text = selectedItem != null ? selectedItem.Description : string.Empty;

            if (useButtonLabelText != null)
            {
                if (selectedItem is HealingPotionItemSO)
                {
                    useButtonLabelText.text = "Equip";
                }
                else if (selectedItem is ToolItemSO)
                {
                    useButtonLabelText.text = "Equip";
                }
                else
                {
                    useButtonLabelText.text = selectedItem != null ? "Use" : string.Empty;
                }
            }

            if (unequipButtonLabelText != null)
            {
                if (selectedItem is HealingPotionItemSO)
                    unequipButtonLabelText.text = "Unequip";
                else if (selectedItem is ToolItemSO)
                    unequipButtonLabelText.text = "Unequip";
                else
                    unequipButtonLabelText.text = string.Empty;
            }

            if (useSelectedItemButton != null)
            {
                useSelectedItemButton.onClick.RemoveAllListeners();
                bool canUse = selectedItem != null;
                useSelectedItemButton.interactable = canUse;

                if (selectedItem is HealingPotionItemSO)
                {
                    int quantity = inventoryState != null ? inventoryState.GetQuantity(selectedItem) : 0;
                    bool canEquip = quantity > 0 && healingPotionEquip != null && healingPotionEquip.HasFreeSlot() && Storage.IsPlayerNear;
                    useSelectedItemButton.interactable = canEquip;
                    useSelectedItemButton.gameObject.SetActive(true);
                    useSelectedItemButton.onClick.AddListener(() => TryEquipPotion(selectedItem));
                }
                else if (selectedItem is ToolItemSO)
                {
                    int quantity = inventoryState != null ? inventoryState.GetQuantity(selectedItem) : 0;
                    bool canEquip = quantity > 0 && toolEquip != null && toolEquip.HasFreeSlot() && Storage.IsPlayerNear;
                    useSelectedItemButton.interactable = canEquip;
                    useSelectedItemButton.gameObject.SetActive(true);
                    useSelectedItemButton.onClick.AddListener(() => TryEquipTool(selectedItem));
                }
                else
                {
                    useSelectedItemButton.gameObject.SetActive(canUse);
                    if (canUse)
                        useSelectedItemButton.onClick.AddListener(() => TryUseItem(selectedItem));
                }
            }

            if (unequipSelectedItemButton != null)
            {
                unequipSelectedItemButton.onClick.RemoveAllListeners();
                bool hasEquipped = selectedItem is HealingPotionItemSO
                    && healingPotionEquip != null
                    && healingPotionEquip.IsEquipped(selectedItem);
                if (selectedItem is ToolItemSO)
                    hasEquipped = toolEquip != null && toolEquip.IsEquipped(selectedItem);
                unequipSelectedItemButton.gameObject.SetActive(hasEquipped);
                unequipSelectedItemButton.interactable = hasEquipped;
                if (hasEquipped)
                {
                    if (selectedItem is ToolItemSO)
                        unequipSelectedItemButton.onClick.AddListener(() => TryUnequipTool(selectedItem));
                    else
                        unequipSelectedItemButton.onClick.AddListener(() => TryUnequipPotion(selectedItem));
                }
            }
        }

        private void ClearViews()
        {
            for (int i = 0; i < spawnedViews.Count; i++)
            {
                if (spawnedViews[i] != null)
                    Destroy(spawnedViews[i].gameObject);
            }

            spawnedViews.Clear();
        }
    }
}
