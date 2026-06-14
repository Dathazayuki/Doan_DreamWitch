using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Inventory
{
    [CreateAssetMenu(fileName = "InventoryState", menuName = "DreamKnight/Inventory/Inventory State")]
    public class InventoryStateSO : ScriptableObject
    {
        [Serializable]
        public class InventorySlot
        {
            public ItemDefinitionSO item;
            public int quantity;
        }

        [SerializeField] private ItemDatabaseSO itemDatabase;
        [SerializeField] private List<ItemStackSaveData> startingItems = new List<ItemStackSaveData>();

        [NonSerialized] private readonly List<InventorySlot> runtimeSlots = new List<InventorySlot>();
        [NonSerialized] private bool runtimeInitialized;

        public event Action OnInventoryChanged;
        public event Action<ItemDefinitionSO, int> OnItemQuantityChanged;

        public IReadOnlyList<InventorySlot> Slots
        {
            get
            {
                EnsureInitialized();
                return runtimeSlots;
            }
        }

        public bool HasItem(ItemDefinitionSO item)
        {
            return GetQuantity(item) > 0;
        }

        public int GetQuantity(ItemDefinitionSO item)
        {
            EnsureInitialized();
            if (item == null)
                return 0;

            for (int i = 0; i < runtimeSlots.Count; i++)
            {
                InventorySlot slot = runtimeSlots[i];
                if (slot.item == item)
                    return slot.quantity;
            }

            return 0;
        }

        public void AddItem(ItemDefinitionSO item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return;

            EnsureInitialized();
            InventorySlot slot = GetOrCreateSlot(item);
            slot.quantity += quantity;
            RaiseChanged(item, slot.quantity);
        }

        public bool RemoveItem(ItemDefinitionSO item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;

            EnsureInitialized();
            InventorySlot slot = GetSlot(item);
            if (slot == null || slot.quantity < quantity)
                return false;

            slot.quantity -= quantity;
            if (slot.quantity <= 0)
                runtimeSlots.Remove(slot);

            RaiseChanged(item, GetQuantity(item));
            return true;
        }

        public bool UseItem(ItemDefinitionSO item, ItemUseContext context)
        {
            if (item == null)
                return false;

            EnsureInitialized();
            if (!HasItem(item))
                return false;

            bool used = item.Use(context);
            if (!used)
                return false;

            return RemoveItem(item, 1);
        }

        public void Clear()
        {
            EnsureInitialized();
            runtimeSlots.Clear();
            OnInventoryChanged?.Invoke();
        }

        public InventorySaveData CaptureSaveData()
        {
            EnsureInitialized();

            InventorySaveData saveData = new InventorySaveData();
            for (int i = 0; i < runtimeSlots.Count; i++)
            {
                InventorySlot slot = runtimeSlots[i];
                if (slot.item == null || slot.quantity <= 0)
                    continue;

                saveData.items.Add(new ItemStackSaveData
                {
                    itemId = slot.item.ItemId,
                    quantity = slot.quantity
                });
            }

            return saveData;
        }

        public void LoadFromSaveData(InventorySaveData saveData, ItemDatabaseSO itemDatabase)
        {
            EnsureInitialized();
            runtimeSlots.Clear();

            ItemDatabaseSO database = itemDatabase != null ? itemDatabase : this.itemDatabase;

            if (saveData != null && saveData.items != null && database != null)
            {
                for (int i = 0; i < saveData.items.Count; i++)
                {
                    ItemStackSaveData stack = saveData.items[i];
                    if (stack == null || string.IsNullOrWhiteSpace(stack.itemId) || stack.quantity <= 0)
                        continue;

                    ItemDefinitionSO item = database.FindById(stack.itemId);
                    if (item == null)
                        continue;

                    runtimeSlots.Add(new InventorySlot
                    {
                        item = item,
                        quantity = stack.quantity
                    });
                }
            }

            OnInventoryChanged?.Invoke();
        }

        public void ResetToStartingItems(ItemDatabaseSO itemDatabase)
        {
            runtimeSlots.Clear();

            ItemDatabaseSO database = itemDatabase;
            if (database != null)
            {
                for (int i = 0; i < startingItems.Count; i++)
                {
                    ItemStackSaveData stack = startingItems[i];
                    if (stack == null || string.IsNullOrWhiteSpace(stack.itemId) || stack.quantity <= 0)
                        continue;

                    ItemDefinitionSO item = database.FindById(stack.itemId);
                    if (item == null)
                        continue;

                    runtimeSlots.Add(new InventorySlot
                    {
                        item = item,
                        quantity = stack.quantity
                    });
                }
            }

            runtimeInitialized = true;
            OnInventoryChanged?.Invoke();
        }

        private InventorySlot GetSlot(ItemDefinitionSO item)
        {
            for (int i = 0; i < runtimeSlots.Count; i++)
            {
                if (runtimeSlots[i].item == item)
                    return runtimeSlots[i];
            }

            return null;
        }

        private InventorySlot GetOrCreateSlot(ItemDefinitionSO item)
        {
            InventorySlot slot = GetSlot(item);
            if (slot != null)
                return slot;

            slot = new InventorySlot
            {
                item = item,
                quantity = 0
            };
            runtimeSlots.Add(slot);
            return slot;
        }

        private void RaiseChanged(ItemDefinitionSO item, int quantity)
        {
            OnInventoryChanged?.Invoke();
            OnItemQuantityChanged?.Invoke(item, quantity);
        }

        private void EnsureInitialized()
        {
            if (runtimeInitialized)
                return;

            ResetToStartingItems();
        }

        private void ResetToStartingItems()
        {
            runtimeSlots.Clear();

            if (itemDatabase != null)
            {
                for (int i = 0; i < startingItems.Count; i++)
                {
                    ItemStackSaveData stack = startingItems[i];
                    if (stack == null || string.IsNullOrWhiteSpace(stack.itemId) || stack.quantity <= 0)
                        continue;

                    ItemDefinitionSO item = itemDatabase.FindById(stack.itemId);
                    if (item == null)
                        continue;

                    runtimeSlots.Add(new InventorySlot
                    {
                        item = item,
                        quantity = stack.quantity
                    });
                }
            }

            runtimeInitialized = true;
        }
    }
}
