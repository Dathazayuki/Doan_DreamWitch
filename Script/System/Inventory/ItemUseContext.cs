using DreamKnight.Player;
using DreamKnight.Systems.Currency;
using UnityEngine;

namespace DreamKnight.Systems.Inventory
{
    public sealed class ItemUseContext
    {
        public ItemUseContext(GameObject user, InventoryStateSO inventory, CurrencyWalletSO wallet, PlayerStats playerStats)
        {
            User = user;
            Inventory = inventory;
            Wallet = wallet;
            PlayerStats = playerStats;
        }

        public GameObject User { get; }
        public InventoryStateSO Inventory { get; }
        public CurrencyWalletSO Wallet { get; }
        public PlayerStats PlayerStats { get; }
    }
}
