using UnityEngine;

namespace DreamKnight.Systems.Inventory
{
    /// <summary>
    /// ScriptableObject representing key/quest items in the inventory.
    /// These items are passive and cannot be used directly by the player from hotbars.
    /// </summary>
    [CreateAssetMenu(fileName = "NewKeyItem", menuName = "DreamKnight/Inventory/Items/Key Item")]
    public class KeyItemSO : ItemDefinitionSO
    {
        public override bool Use(ItemUseContext context)
        {
            // Key items are consumed via interactions, not directly usable from inventory
            return false;
        }
    }
}
