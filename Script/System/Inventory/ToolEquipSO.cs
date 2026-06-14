using System;
using UnityEngine;
using DreamKnight.Systems.SaveLoad;

namespace DreamKnight.Systems.Inventory
{
    [CreateAssetMenu(fileName = "ToolEquip", menuName = "DreamKnight/Inventory/Tool Equip")]
    public class ToolEquipSO : ScriptableObject
    {
        [Tooltip("Tool mặc định khi khởi động (tùy chọn, để trống nếu không cần)")]
        [SerializeField] private ToolItemSO defaultTool;
        [SerializeField] private int slotCount = 2;
        [NonSerialized] private int facilitySlotBonus;
        public event Action OnEquipmentChanged;

        [NonSerialized] private ItemDefinitionSO[] runtimeSlots;
        [NonSerialized] private bool runtimeInitialized;

        public int SlotCount
        {
            get
            {
                EnsureInitialized();
                return runtimeSlots.Length;
            }
        }

        public int BaseSlotCount => Mathf.Max(1, slotCount);
        public int FacilitySlotBonus => facilitySlotBonus;

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

        public bool TryEquip(ItemDefinitionSO item)
        {
            if (!IsValidTool(item))
                return false;

            EnsureInitialized();
            for (int i = 0; i < runtimeSlots.Length; i++)
            {
                if (runtimeSlots[i] == null)
                {
                    runtimeSlots[i] = item;
                    OnEquipmentChanged?.Invoke();
                    GameAutoSave.Request("tool_equip");
                    return true;
                }
            }

            return false;
        }

        public int TryEquipMultiple(ItemDefinitionSO item, int count)
        {
            if (!IsValidTool(item) || count <= 0)
                return 0;

            EnsureInitialized();
            int equipped = 0;
            for (int i = 0; i < runtimeSlots.Length && equipped < count; i++)
            {
                if (runtimeSlots[i] == null)
                {
                    runtimeSlots[i] = item;
                    equipped++;
                }
            }

            if (equipped > 0)
            {
                OnEquipmentChanged?.Invoke();
                GameAutoSave.Request("tool_equip");
            }

            return equipped;
        }

        public bool TryUnequip(ItemDefinitionSO item)
        {
            EnsureInitialized();
            if (item == null)
                return false;

            for (int i = runtimeSlots.Length - 1; i >= 0; i--)
            {
                if (runtimeSlots[i] == item)
                {
                    runtimeSlots[i] = null;
                    OnEquipmentChanged?.Invoke();
                    GameAutoSave.Request("tool_unequip");
                    return true;
                }
            }

            return false;
        }

        public int TryUnequipAll(ItemDefinitionSO item)
        {
            EnsureInitialized();
            if (item == null)
                return 0;

            int removed = 0;
            for (int i = 0; i < runtimeSlots.Length; i++)
            {
                if (runtimeSlots[i] == item)
                {
                    runtimeSlots[i] = null;
                    removed++;
                }
            }

            if (removed > 0)
            {
                OnEquipmentChanged?.Invoke();
                GameAutoSave.Request("tool_unequip");
            }

            return removed;
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
            GameAutoSave.Request("tool_unequip");
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
            int targetSlotCount = Mathf.Max(GetDesiredSlotCount(), savedSlotCount);
            runtimeSlots = new ItemDefinitionSO[targetSlotCount];

            if (saveData != null && saveData.slotItemIds != null && itemDatabase != null)
            {
                for (int i = 0; i < saveData.slotItemIds.Count && i < runtimeSlots.Length; i++)
                {
                    string itemId = saveData.slotItemIds[i];
                    if (string.IsNullOrWhiteSpace(itemId))
                        continue;

                    ItemDefinitionSO item = itemDatabase.FindById(itemId);
                    if (IsValidTool(item))
                        runtimeSlots[i] = item;
                }
            }

            runtimeInitialized = true;
            OnEquipmentChanged?.Invoke();
        }

        public void SetFacilitySlotBonus(int slotBonus)
        {
            int clampedBonus = Mathf.Max(0, slotBonus);
            if (runtimeInitialized && facilitySlotBonus == clampedBonus)
                return;

            facilitySlotBonus = clampedBonus;
            ResizeRuntimeSlots(GetDesiredSlotCount());
        }

        private bool IsValidTool(ItemDefinitionSO item)
        {
            if (item == null)
                return false;

            return item is ToolItemSO;
        }

        private void EnsureInitialized()
        {
            if (runtimeInitialized)
                return;

            int count = GetDesiredSlotCount();
            runtimeSlots = new ItemDefinitionSO[count];
            runtimeInitialized = true;
        }

        private int GetDesiredSlotCount()
        {
            return Mathf.Max(1, slotCount + facilitySlotBonus);
        }

        private void ResizeRuntimeSlots(int desiredCount)
        {
            EnsureInitialized();

            int occupiedCount = GetOccupiedCount();
            int targetCount = Mathf.Max(1, desiredCount, occupiedCount);
            if (runtimeSlots.Length == targetCount)
                return;

            ItemDefinitionSO[] oldSlots = runtimeSlots;
            ItemDefinitionSO[] newSlots = new ItemDefinitionSO[targetCount];
            int nextIndex = 0;

            for (int i = 0; i < oldSlots.Length && nextIndex < newSlots.Length; i++)
            {
                if (oldSlots[i] == null)
                    continue;

                newSlots[nextIndex] = oldSlots[i];
                nextIndex++;
            }

            runtimeSlots = newSlots;
            runtimeInitialized = true;
            OnEquipmentChanged?.Invoke();
        }

        private int GetOccupiedCount()
        {
            EnsureInitialized();
            int count = 0;
            for (int i = 0; i < runtimeSlots.Length; i++)
            {
                if (runtimeSlots[i] != null)
                    count++;
            }

            return count;
        }

        public bool HasTool()
        {
            EnsureInitialized();
            for (int i = 0; i < runtimeSlots.Length; i++)
            {
                if (runtimeSlots[i] != null)
                    return true;
            }
            return false;
        }

        /// <summary>Tải defaultTool vào slot đầu tiên khi bắt đầu game (gọi từ GameManager hoặc Awake).</summary>
        public void LoadDefault()
        {
            if (defaultTool == null) return;
            EnsureInitialized();
            if (runtimeSlots[0] == null)
                runtimeSlots[0] = defaultTool;
        }
    }
}
