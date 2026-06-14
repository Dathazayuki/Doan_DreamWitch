using System;
using System.Collections.Generic;
using UnityEngine;
using DreamKnight.Systems.Currency;
using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.SaveLoad;

namespace DreamKnight.Systems.Shop
{
    [CreateAssetMenu(fileName = "ShopState", menuName = "DreamKnight/Shop/Shop State")]
    public class ShopStateSO : ScriptableObject
    {
        [Serializable]
        public class ShopStock
        {
            public ItemDefinitionSO item;
            public int quantity;
        }

        [SerializeField] private ShopCatalogSO catalog;

        [NonSerialized] private readonly List<ShopStock> runtimeStock = new List<ShopStock>();
        [NonSerialized] private bool runtimeInitialized;

        public event Action OnShopChanged;
        public event Action<ItemDefinitionSO, int> OnStockChanged;

        public IReadOnlyList<ShopStock> Stock
        {
            get
            {
                EnsureInitialized();
                return runtimeStock;
            }
        }

        public void ResetFromCatalog()
        {
            runtimeStock.Clear();

            if (catalog != null)
            {
                IReadOnlyList<ShopCatalogSO.ShopEntry> entries = catalog.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    ShopCatalogSO.ShopEntry entry = entries[i];
                    if (entry == null || entry.item == null || entry.quantity <= 0)
                        continue;

                    runtimeStock.Add(new ShopStock
                    {
                        item = entry.item,
                        quantity = entry.quantity
                    });
                }
            }

            runtimeInitialized = true;
            OnShopChanged?.Invoke();
        }

        public int GetQuantity(ItemDefinitionSO item)
        {
            EnsureInitialized();
            if (item == null)
                return 0;

            for (int i = 0; i < runtimeStock.Count; i++)
            {
                ShopStock stock = runtimeStock[i];
                if (stock.item == item)
                    return stock.quantity;
            }

            return 0;
        }

        public bool TryConsume(ItemDefinitionSO item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;

            EnsureInitialized();
            ShopStock stock = GetStock(item);
            if (stock == null || stock.quantity < quantity)
                return false;

            stock.quantity -= quantity;
            RaiseChanged(item, stock.quantity);
            return true;
        }

        public void Restock(ItemDefinitionSO item, int quantity)
        {
            if (item == null || quantity <= 0)
                return;

            EnsureInitialized();
            ShopStock stock = GetOrCreateStock(item);
            stock.quantity += quantity;
            RaiseChanged(item, stock.quantity);
        }

        public bool TryPurchase(ItemDefinitionSO item, int quantity, CurrencyWalletSO wallet, InventoryStateSO inventory)
        {
            if (item == null || quantity <= 0 || wallet == null || inventory == null)
                return false;

            EnsureInitialized();

            ShopStock stock = GetStock(item);
            if (stock == null || stock.quantity < quantity)
                return false;

            int unitPrice = Mathf.Max(0, item.Price);
            int totalPrice = unitPrice * quantity;
            if (!wallet.Spend(totalPrice))
                return false;

            stock.quantity -= quantity;
            inventory.AddItem(item, quantity);
            RaiseChanged(item, stock.quantity);
            GameAutoSave.Request("shop_purchase");
            return true;
        }

        public ShopSaveData CaptureSaveData()
        {
            EnsureInitialized();
            ShopSaveData saveData = new ShopSaveData();

            for (int i = 0; i < runtimeStock.Count; i++)
            {
                ShopStock stock = runtimeStock[i];
                if (stock.item == null)
                    continue;

                saveData.stock.Add(new ShopStockSaveData
                {
                    itemId = stock.item.ItemId,
                    quantity = stock.quantity
                });
            }

            return saveData;
        }

        public void LoadFromSaveData(ShopSaveData saveData, ItemDatabaseSO itemDatabase)
        {
            EnsureInitialized();
            runtimeStock.Clear();

            if (saveData != null && saveData.stock != null && itemDatabase != null)
            {
                for (int i = 0; i < saveData.stock.Count; i++)
                {
                    ShopStockSaveData stockData = saveData.stock[i];
                    if (stockData == null || string.IsNullOrWhiteSpace(stockData.itemId) || stockData.quantity < 0)
                        continue;

                    ItemDefinitionSO item = itemDatabase.FindById(stockData.itemId);
                    if (item == null)
                        continue;

                    runtimeStock.Add(new ShopStock
                    {
                        item = item,
                        quantity = stockData.quantity
                    });
                }
            }

            OnShopChanged?.Invoke();
        }

        private ShopStock GetStock(ItemDefinitionSO item)
        {
            for (int i = 0; i < runtimeStock.Count; i++)
            {
                if (runtimeStock[i].item == item)
                    return runtimeStock[i];
            }

            return null;
        }

        private ShopStock GetOrCreateStock(ItemDefinitionSO item)
        {
            ShopStock stock = GetStock(item);
            if (stock != null)
                return stock;

            stock = new ShopStock
            {
                item = item,
                quantity = 0
            };
            runtimeStock.Add(stock);
            return stock;
        }

        private void RaiseChanged(ItemDefinitionSO item, int quantity)
        {
            OnShopChanged?.Invoke();
            OnStockChanged?.Invoke(item, quantity);
        }

        private void EnsureInitialized()
        {
            if (runtimeInitialized)
                return;

            ResetFromCatalog();
        }
    }
}
