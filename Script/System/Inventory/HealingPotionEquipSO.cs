using System;
using UnityEngine;
using DreamKnight.Systems.SaveLoad;

namespace DreamKnight.Systems.Inventory
{
    [CreateAssetMenu(fileName = "HealingPotionEquip", menuName = "DreamKnight/Inventory/Healing Potion Equip")]
    public class HealingPotionEquipSO : ScriptableObject
    {
        [SerializeField] private int slotCount = 2;

        [NonSerialized] private ItemDefinitionSO[] runtimeSlots;
        [NonSerialized] private bool runtimeInitialized;

        public event Action OnEquipmentChanged;

        public int SlotCount
        {
            get
            {
                EnsureInitialized();
                return runtimeSlots.Length;
            }
        }

        public ItemDefinitionSO GetSlotItem(int index)
        {
            EnsureInitialized();
            if (index < 0 || index >= runtimeSlots.Length)
                return null;

            return runtimeSlots[index];
        }

        public bool IsEquipped(ItemDefinitionSO item)
        {
            EnsureInitialized();
            if (item == null)
                return false;

            for (int i = 0; i < runtimeSlots.Length; i++)
            {
                if (runtimeSlots[i] == item)
                    return true;
            }

            return false;
        }

        public bool TryEquip(ItemDefinitionSO item)
        {
            if (!IsValidHealingPotion(item))
                return false;

            EnsureInitialized();

            for (int i = 0; i < runtimeSlots.Length; i++)
            {
                if (runtimeSlots[i] == null)
                {
                    runtimeSlots[i] = item;
                    OnEquipmentChanged?.Invoke();
                    GameAutoSave.Request("potion_equip");
                    return true;
                }
            }

            return false;
        }

        public int GetEquippedCount(ItemDefinitionSO item)
        {
            EnsureInitialized();
            if (item == null)
                return 0;

            int count = 0;
            for (int i = 0; i < runtimeSlots.Length; i++)
            {
                if (runtimeSlots[i] == item)
                    count++;
            }

            return count;
        }

        public bool HasFreeSlot()
        {
            EnsureInitialized();
            for (int i = 0; i < runtimeSlots.Length; i++)
            {
                if (runtimeSlots[i] == null)
                    return true;
            }

            return false;
        }

        public bool TryUnequip(ItemDefinitionSO item)
        {
            EnsureInitialized();
            if (item == null)
                return false;

            for (int i = runtimeSlots.Length-1; i >=0 ; i--)
            {
                if (runtimeSlots[i] == item)
                {
                    runtimeSlots[i] = null;
                    OnEquipmentChanged?.Invoke();
                    GameAutoSave.Request("potion_unequip");
                    return true;
                }
            }

            return false;
        }

        public bool TryUnequipAt(int index)
        {
            EnsureInitialized();
            if (index < 0 || index >= runtimeSlots.Length)
                return false;

            if (runtimeSlots[index] == null)
                return false;

            runtimeSlots[index] = null;
            OnEquipmentChanged?.Invoke();
            GameAutoSave.Request("potion_unequip");
            return true;
        }

        public EquipmentSaveData CaptureSaveData()
        {
            EnsureInitialized();
            EquipmentSaveData saveData = new EquipmentSaveData();

            for (int i = 0; i < runtimeSlots.Length; i++)
            {
                ItemDefinitionSO item = runtimeSlots[i];
                saveData.slotItemIds.Add(item != null ? item.ItemId : string.Empty);
            }

            return saveData;
        }

        public void LoadFromSaveData(EquipmentSaveData saveData, ItemDatabaseSO itemDatabase)
        {
            EnsureInitialized();

            int savedSlotCount = saveData != null && saveData.slotItemIds != null ? saveData.slotItemIds.Count : 0;
            int targetSlotCount = Mathf.Max(Mathf.Max(1, slotCount), savedSlotCount);
            runtimeSlots = new ItemDefinitionSO[targetSlotCount];

            if (saveData != null && saveData.slotItemIds != null && itemDatabase != null)
            {
                for (int i = 0; i < saveData.slotItemIds.Count && i < runtimeSlots.Length; i++)
                {
                    string itemId = saveData.slotItemIds[i];
                    if (string.IsNullOrWhiteSpace(itemId))
                        continue;

                    ItemDefinitionSO item = itemDatabase.FindById(itemId);
                    if (IsValidHealingPotion(item))
                        runtimeSlots[i] = item;
                }
            }

            runtimeInitialized = true;
            OnEquipmentChanged?.Invoke();
        }

        private bool IsValidHealingPotion(ItemDefinitionSO item)
        {
            if (item == null)
                return false;

            return item is HealingPotionItemSO;
        }

        private void EnsureInitialized()
        {
            if (runtimeInitialized)
                return;

            int count = Mathf.Max(1, slotCount);
            runtimeSlots = new ItemDefinitionSO[count];
            runtimeInitialized = true;
        }
    }
}
