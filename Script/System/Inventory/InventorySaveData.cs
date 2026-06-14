using System;
using System.Collections.Generic;

namespace DreamKnight.Systems.Inventory
{
    [Serializable]
    public class InventorySaveData
    {
        public List<ItemStackSaveData> items = new List<ItemStackSaveData>();
        public List<string> learnedSpellIds = new List<string>();
    }

    [Serializable]
    public class ItemStackSaveData
    {
        public string itemId;
        public int quantity;
    }
}
