using System.Collections.Generic;
using UnityEngine;
using DreamKnight.Systems.Inventory;

namespace DreamKnight.Systems.Shop
{
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "DreamKnight/Shop/Shop Catalog")]
    public class ShopCatalogSO : ScriptableObject
    {
        [System.Serializable]
        public class ShopEntry
        {
            public ItemDefinitionSO item;
            [Min(0)] public int quantity = 1;
            [Min(0)] public int overridePrice = -1;
        }

        [SerializeField] private List<ShopEntry> entries = new List<ShopEntry>();

        public IReadOnlyList<ShopEntry> Entries => entries;
    }
}
