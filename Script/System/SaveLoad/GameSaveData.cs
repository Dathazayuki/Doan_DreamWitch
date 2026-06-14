using System;
using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.Shop;
using DreamKnight.Systems.Skill;
using DreamKnight.Systems.Facility;
using DreamKnight.Systems.SkillTree;

namespace DreamKnight.Systems.SaveLoad
{
    [Serializable]
    public class GameSaveData
    {
        public int gold;
        public InventorySaveData inventory = new InventorySaveData();
        public EquipmentSaveData toolEquip = new EquipmentSaveData();
        public EquipmentSaveData healingPotionEquip = new EquipmentSaveData();
        public bool hasShopData;
        public ShopSaveData shop = new ShopSaveData();
        public bool hasSkillProgressData;
        public SkillProgressSaveData skillProgress = new SkillProgressSaveData();
        public FacilityProgressSaveData facilityProgress = new FacilityProgressSaveData();
        public bool hasSkillTreeProgressData;
        public SkillTreeProgressSaveData skillTreeProgress = new SkillTreeProgressSaveData();
        public PortalSaveData portals = new PortalSaveData();
        public DoorSaveData doors = new DoorSaveData();
        public WorldPickupSaveData worldPickups = new WorldPickupSaveData();
        public BossDefeatSaveData bossDefeats = new BossDefeatSaveData();
        // equipped spell id persisted here (empty = none)
        public string equippedSpellId = string.Empty;
        public int slotIndex = -1;
        public string saveDisplayName = string.Empty;
        public long createdUtcTicks;
        public long updatedUtcTicks;
        public float playTimeSeconds;
    }
}
