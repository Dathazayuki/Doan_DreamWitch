using DreamKnight.Systems.Inventory;
using UnityEngine;
using Yarn.Unity;

namespace DreamKnight.Systems.Dialogue
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class YarnInventoryBridge : MonoBehaviour
    {
        private static YarnInventoryBridge instance;

        [SerializeField] private InventoryStateSO inventoryState;
        [SerializeField] private ItemDatabaseSO itemDatabase;

        private void Awake()
        {
            instance = this;
        }

        private void OnEnable()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        [YarnFunction("has_item")]
        public static bool HasItem(string itemId)
        {
            if (instance == null)
            {
                ResolveInstance();
                if (instance == null)
                {
                    return false;
                }
            }

            ItemDefinitionSO item = FindItem(itemId);
            int quantity = item != null && instance.inventoryState != null ? instance.inventoryState.GetQuantity(item) : 0;
            return quantity > 0;
        }

        [YarnFunction("item_count")]
        public static int GetItemCount(string itemId)
        {
            if (instance == null)
            {
                ResolveInstance();
                if (instance == null)
                {
                    return 0;
                }
            }

            ItemDefinitionSO item = FindItem(itemId);
            return item != null && instance.inventoryState != null ? instance.inventoryState.GetQuantity(item) : 0;
        }

        private static ItemDefinitionSO FindItem(string itemId)
        {
            if (instance == null || instance.itemDatabase == null || string.IsNullOrWhiteSpace(itemId))
                return null;

            ItemDefinitionSO item = instance.itemDatabase.FindById(itemId);
            return item;
        }

        private static void ResolveInstance()
        {
            if (instance != null)
                return;

            instance = FindAnyObjectByType<YarnInventoryBridge>();
        }
    }
}
