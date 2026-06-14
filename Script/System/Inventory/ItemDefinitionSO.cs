using UnityEngine;

namespace DreamKnight.Systems.Inventory
{
    public abstract class ItemDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Shop")]
        [SerializeField] private int price = 0;
        [SerializeField] private bool stackable = true;

        [Header("Description")]
        [TextArea(2, 6)]
        [SerializeField] private string description;

        [SerializeField] private ItemCategory category;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int Price => price;
        public bool Stackable => stackable;
        public string Description => description;
        public ItemCategory Category => category;

        public abstract bool Use(ItemUseContext context);

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = name;

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = name;
        }
#endif
    }
}
