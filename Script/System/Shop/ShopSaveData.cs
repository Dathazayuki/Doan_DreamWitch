using System;
using System.Collections.Generic;

namespace DreamKnight.Systems.Shop
{
    [Serializable]
    public class ShopSaveData
    {
        public List<ShopStockSaveData> stock = new List<ShopStockSaveData>();
    }

    [Serializable]
    public class ShopStockSaveData
    {
        public string itemId;
        public int quantity;
    }
}
