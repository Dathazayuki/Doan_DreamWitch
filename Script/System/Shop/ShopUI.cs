using System.Collections.Generic;
using DreamKnight.Systems.Currency;
using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.Systems.Shop
{
    public class ShopUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ShopStateSO shopState;
        [SerializeField] private InventoryStateSO inventoryState;
        [SerializeField] private CurrencyWalletSO currencyWallet;
        [SerializeField] private SpellManager spellManager;

        [Header("View")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private ShopItemView itemViewPrefab;
        [SerializeField] private ShopSkillItemView skillItemViewPrefab;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private GameObject emptyStateObject;

        [Header("Tabs")]
        [SerializeField] private Button facilityTabButton;
        [SerializeField] private Button skillTabButton;
        [SerializeField] private Button toolTabButton;

        [Header("Detail Panel")]
        [SerializeField] private Image selectedItemIcon;
        [SerializeField] private TextMeshProUGUI selectedItemNameText;
        [SerializeField] private TextMeshProUGUI selectedItemPriceText;
        [SerializeField] private TextMeshProUGUI selectedItemStockText;
        [SerializeField] private TextMeshProUGUI selectedItemDescriptionText;
        [SerializeField] private Button buySelectedItemButton;
        [SerializeField] private TextMeshProUGUI buyButtonLabelText;

        [Header("Spell Additional Info")]
        [SerializeField] private TextMeshProUGUI selectedItemLevelText;
        [SerializeField] private TextMeshProUGUI selectedItemCDText;
        [SerializeField] private TextMeshProUGUI selectedItemManaText;
        [SerializeField] private TextMeshProUGUI selectedItemDamageText;

        private readonly List<ShopItemView> spawnedViews = new List<ShopItemView>();
        private readonly List<ShopSkillItemView> spawnedSkillViews = new List<ShopSkillItemView>();
        private ItemDefinitionSO selectedItem;
        private SpellData selectedSpell;
        private ShopTab currentTab = ShopTab.Facility;

        private enum ShopTab
        {
            Facility = 0,
            Skill = 1,
            Tool = 2
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            BindTabButtons();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnbindTabButtons();
        }

        private void Subscribe()
        {
            if (shopState != null)
                shopState.OnShopChanged += Refresh;

            if (inventoryState != null)
                inventoryState.OnInventoryChanged += Refresh;

            if (currencyWallet != null)
                currencyWallet.OnBalanceChanged += HandleGoldChanged;

            if (spellManager != null)
                spellManager.OnSpellProgressChanged += HandleSpellProgressChanged;
        }

        private void ResolveReferences()
        {
            if (spellManager == null)
                spellManager = FindAnyObjectByType<SpellManager>();
        }

        private void Unsubscribe()
        {
            if (shopState != null)
                shopState.OnShopChanged -= Refresh;

            if (inventoryState != null)
                inventoryState.OnInventoryChanged -= Refresh;

            if (currencyWallet != null)
                currencyWallet.OnBalanceChanged -= HandleGoldChanged;

            if (spellManager != null)
                spellManager.OnSpellProgressChanged -= HandleSpellProgressChanged;
        }

        private void HandleGoldChanged(int balance)
        {
            Refresh();
        }

        private void HandleSpellProgressChanged(string spellId, int level)
        {
            Refresh();
        }

        private void BindTabButtons()
        {
            if (facilityTabButton != null)
            {
                facilityTabButton.onClick.RemoveAllListeners();
                facilityTabButton.onClick.AddListener(() => SetTab(ShopTab.Facility));
            }

            if (skillTabButton != null)
            {
                skillTabButton.onClick.RemoveAllListeners();
                skillTabButton.onClick.AddListener(() => SetTab(ShopTab.Skill));
            }

            if (toolTabButton != null)
            {
                toolTabButton.onClick.RemoveAllListeners();
                toolTabButton.onClick.AddListener(() => SetTab(ShopTab.Tool));
            }
        }

        private void UnbindTabButtons()
        {
            if (facilityTabButton != null)
                facilityTabButton.onClick.RemoveAllListeners();

            if (skillTabButton != null)
                skillTabButton.onClick.RemoveAllListeners();

            if (toolTabButton != null)
                toolTabButton.onClick.RemoveAllListeners();
        }

        private void SetTab(ShopTab tab)
        {
            if (currentTab == tab)
                return;

            currentTab = tab;
            selectedItem = null;
            selectedSpell = null;
            Refresh();
        }

        public void Refresh()
        {
            ClearViews();

            if (goldText != null)
                goldText.text = currencyWallet != null ? currencyWallet.Balance.ToString() : "0";

            if (currentTab == ShopTab.Skill)
            {
                RenderSkillTab();
                return;
            }

            if (shopState == null || contentRoot == null || itemViewPrefab == null)
                return;

            IReadOnlyList<ShopStateSO.ShopStock> stock = shopState.Stock;
            if (stock.Count == 0)
            {
                if (emptyStateObject != null)
                    emptyStateObject.SetActive(true);

                selectedItem = null;
                RefreshDetailPanel();
                return;
            }

            if (emptyStateObject != null)
                emptyStateObject.SetActive(false);

            bool selectedStillExists = false;
            for (int i = 0; i < stock.Count; i++)
            {
                ShopStateSO.ShopStock entry = stock[i];
                if (entry == null || entry.item == null || entry.quantity <= 0)
                    continue;

                if (!IsItemVisibleInCurrentTab(entry.item))
                    continue;

                if (entry.item == selectedItem)
                    selectedStillExists = true;
            }

            if (!selectedStillExists)
            {
                selectedItem = null;
                for (int i = 0; i < stock.Count; i++)
                {
                    ShopStateSO.ShopStock entry = stock[i];
                    if (entry != null && entry.item != null && entry.quantity > 0 && IsItemVisibleInCurrentTab(entry.item))
                    {
                        selectedItem = entry.item;
                        break;
                    }
                }
            }

            bool hasVisibleItems = false;
            for (int i = 0; i < stock.Count; i++)
            {
                ShopStateSO.ShopStock entry = stock[i];
                if (entry == null || entry.item == null)
                    continue;

                if (!IsItemVisibleInCurrentTab(entry.item))
                    continue;

                hasVisibleItems = true;

                int price = Mathf.Max(0, entry.item.Price);

                ShopItemView view = Instantiate(itemViewPrefab, contentRoot);
                spawnedViews.Add(view);
                ItemDefinitionSO itemForClick = entry.item;
                int currentGold = currencyWallet != null ? currencyWallet.Balance : 0;
                bool canBuy = currentGold >= price;
                view.Bind(entry.item, entry.quantity, price, entry.item == selectedItem, () => SelectItem(itemForClick), canBuy, () => TryBuy(entry.item));
            }

            if (emptyStateObject != null)
                emptyStateObject.SetActive(!hasVisibleItems);

            RefreshDetailPanel();
        }

        private void TryBuy(ItemDefinitionSO item)
        {
            if (shopState == null || inventoryState == null || currencyWallet == null || item == null)
                return;

            shopState.TryPurchase(item, 1, currencyWallet, inventoryState);
        }

        private void RenderSkillTab()
        {
            selectedItem = null;

            if (spellManager == null || spellManager.SpellDatabase == null || contentRoot == null || skillItemViewPrefab == null)
                return;

            IReadOnlyList<SpellData> spells = spellManager.SpellDatabase.Spells;
            if (spells == null || spells.Count == 0)
            {
                if (emptyStateObject != null)
                    emptyStateObject.SetActive(true);

                selectedSpell = null;
                RefreshDetailPanel();
                return;
            }

            if (emptyStateObject != null)
                emptyStateObject.SetActive(false);

            bool selectedStillExists = false;
            for (int i = 0; i < spells.Count; i++)
            {
                SpellData spell = spells[i];
                if (spell == null)
                    continue;

                if (spell == selectedSpell)
                    selectedStillExists = true;
            }

            if (!selectedStillExists)
            {
                selectedSpell = null;
                for (int i = 0; i < spells.Count; i++)
                {
                    SpellData spell = spells[i];
                    if (spell != null)
                    {
                        selectedSpell = spell;
                        break;
                    }
                }
            }

            bool hasVisibleItems = false;
            for (int i = 0; i < spells.Count; i++)
            {
                SpellData spell = spells[i];
                if (spell == null)
                    continue;

                hasVisibleItems = true;

                int level = spellManager.GetLevel(spell);
                int maxLevel = spell.MaxLevel;
                int price = spellManager.GetNextPrice(spell);
                bool canBuy = spellManager.CanUpgrade(spell);

                ShopSkillItemView view = Instantiate(skillItemViewPrefab, contentRoot);
                spawnedSkillViews.Add(view);
                SpellData spellForClick = spell;
                view.Bind(spell, level, maxLevel, price, spell == selectedSpell, () => SelectSpell(spellForClick), canBuy, () => TryUpgradeSpell(spellForClick));
            }

            if (emptyStateObject != null)
                emptyStateObject.SetActive(!hasVisibleItems);

            RefreshDetailPanel();
        }

        private void TryUpgradeSpell(SpellData spell)
        {
            if (spellManager == null || spell == null)
                return;

            spellManager.TryUnlockOrUpgrade(spell);
        }

        private void SelectItem(ItemDefinitionSO item)
        {
            selectedItem = item;
            selectedSpell = null;
            Refresh();
        }

        private void SelectSpell(SpellData spell)
        {
            selectedSpell = spell;
            selectedItem = null;
            Refresh();
        }

        private void RefreshDetailPanel()
        {
            if (selectedSpell != null)
            {
                if (selectedItemIcon != null)
                    selectedItemIcon.sprite = selectedSpell.icon;

                if (selectedItemNameText != null)
                    selectedItemNameText.text = selectedSpell.spellName;

                int level = spellManager != null ? spellManager.GetLevel(selectedSpell) : 0;
                int maxLevel = spellManager != null ? spellManager.GetMaxLevel(selectedSpell) : 0;
                int price = spellManager != null ? spellManager.GetNextPrice(selectedSpell) : 0;
                bool canBuy = spellManager != null && spellManager.CanUpgrade(selectedSpell);

                if (selectedItemPriceText != null)
                    selectedItemPriceText.text = price > 0 ? price.ToString() : string.Empty;

                if (selectedItemStockText != null)
                {
                    if (maxLevel > 0)
                        selectedItemStockText.text = $"Lv {level}/{maxLevel}";
                    else
                        selectedItemStockText.text = $"Lv {level}";
                }

                if (selectedItemDescriptionText != null)
                    selectedItemDescriptionText.text = selectedSpell.description ?? string.Empty;

                if (buyButtonLabelText != null)
                    buyButtonLabelText.text = level > 0 ? "Upgrade" : "Buy";

                if (buySelectedItemButton != null)
                {
                    buySelectedItemButton.onClick.RemoveAllListeners();
                    buySelectedItemButton.interactable = canBuy;
                    buySelectedItemButton.onClick.AddListener(() => TryUpgradeSpell(selectedSpell));
                }

                // Hiển thị thông số của level hiện tại (nếu chưa học thì hiển thị thông số level 1)
                int displayLevel = level > 0 ? level : 1;
                if (selectedItemLevelText != null) selectedItemLevelText.text = $"Lv: {level}/{maxLevel}";
                if (selectedItemCDText != null) selectedItemCDText.text = $"{selectedSpell.GetCooldown(displayLevel)}s";
                if (selectedItemManaText != null) selectedItemManaText.text = $"{selectedSpell.GetManaCost(displayLevel)}";
                if (selectedItemDamageText != null) selectedItemDamageText.text = $"{selectedSpell.GetDamage(displayLevel)}";

                return;
            }

            if (selectedItemIcon != null)
                selectedItemIcon.sprite = selectedItem != null ? selectedItem.Icon : null;

            if (selectedItemNameText != null)
                selectedItemNameText.text = selectedItem != null ? selectedItem.DisplayName : string.Empty;

            int itemPrice = selectedItem != null ? Mathf.Max(0, selectedItem.Price) : 0;
            int stock = selectedItem != null ? GetSelectedStock() : 0;
            int gold = currencyWallet != null ? currencyWallet.Balance : 0;
            bool canItemBuy = selectedItem != null && stock > 0 && gold >= itemPrice;

            if (selectedItemPriceText != null)
                selectedItemPriceText.text = selectedItem != null ? itemPrice.ToString() : string.Empty;

            if (selectedItemStockText != null)
                selectedItemStockText.text = selectedItem != null ? (stock > 0 ? $"Stock: {stock}" : "Sold out") : string.Empty;

            if (selectedItemDescriptionText != null)
                selectedItemDescriptionText.text = selectedItem != null ? selectedItem.Description : string.Empty;

            if (buyButtonLabelText != null)
                buyButtonLabelText.text = selectedItem != null ? "Buy" : string.Empty;

            if (buySelectedItemButton != null)
            {
                buySelectedItemButton.onClick.RemoveAllListeners();
                buySelectedItemButton.interactable = canItemBuy;
                if (selectedItem != null)
                    buySelectedItemButton.onClick.AddListener(() => TryBuy(selectedItem));
            }

            // Xóa thông số kỹ năng khi đang chọn Item thường
            if (selectedItemLevelText != null) selectedItemLevelText.text = string.Empty;
            if (selectedItemCDText != null) selectedItemCDText.text = string.Empty;
            if (selectedItemManaText != null) selectedItemManaText.text = string.Empty;
            if (selectedItemDamageText != null) selectedItemDamageText.text = string.Empty;
        }

        private bool IsItemVisibleInCurrentTab(ItemDefinitionSO item)
        {
            if (item == null)
                return false;

            switch (currentTab)
            {
                case ShopTab.Facility:
                    return item.Category == ItemCategory.HealingPotion || item.Category == ItemCategory.SpecialPotion;
                case ShopTab.Skill:
                    return false;
                case ShopTab.Tool:
                    return item.Category == ItemCategory.Tool;
                default:
                    return true;
            }
        }

        private int GetSelectedStock()
        {
            if (shopState == null || selectedItem == null)
                return 0;

            return shopState.GetQuantity(selectedItem);
        }

        private void ClearViews()
        {
            for (int i = 0; i < spawnedViews.Count; i++)
            {
                if (spawnedViews[i] != null)
                    Destroy(spawnedViews[i].gameObject);
            }

            spawnedViews.Clear();

            for (int i = 0; i < spawnedSkillViews.Count; i++)
            {
                if (spawnedSkillViews[i] != null)
                    Destroy(spawnedSkillViews[i].gameObject);
            }

            spawnedSkillViews.Clear();
        }
    }
}
