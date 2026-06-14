using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Inventory
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "DreamKnight/Inventory/Item Database")]
    public class ItemDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<ItemDefinitionSO> items = new List<ItemDefinitionSO>();

        public IReadOnlyList<ItemDefinitionSO> Items => items;

        public ItemDefinitionSO FindById(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinitionSO item = items[i];
                if (item != null && item.ItemId == itemId)
                    return item;
            }

            return null;
        }
    }
}
