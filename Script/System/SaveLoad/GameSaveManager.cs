using System;
using System.IO;
using DreamKnight.Systems.Currency;
using DreamKnight.Systems.Facility;
using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.Scene;
using DreamKnight.Systems.Shop;
using DreamKnight.Systems.Skill;
using DreamKnight.Systems.SkillTree;
using DreamKnight.Systems.Zone;
using UnityEngine;

namespace DreamKnight.Systems.SaveLoad
{
    [DisallowMultipleComponent]
    public class GameSaveManager : MonoBehaviour
    {
        private static GameSaveManager instance;

        [Header("Save File")]
        [SerializeField] private string saveFileName = "game_save.json";
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool loadOnStart;
        [SerializeField] private bool saveOnApplicationQuit;
        [SerializeField] private int activeSlotIndex = -1;

        [Header("Databases")]
        [SerializeField] private ItemDatabaseSO itemDatabase;
        [SerializeField] private SpellDatabaseSO spellDatabase;

        [Header("Runtime States")]
        [SerializeField] private CurrencyWalletSO currencyWallet;
        [SerializeField] private InventoryStateSO inventoryState;
        [SerializeField] private ToolEquipSO toolEquip;
        [SerializeField] private HealingPotionEquipSO healingPotionEquip;
        [SerializeField] private ShopStateSO shopState;
        [SerializeField] private SkillProgressSO skillProgress;
        [SerializeField] private SpellEquipSO spellEquip;
        [SerializeField] private FacilityManager facilityManager;
        [SerializeField] private SkillTreeManager skillTreeManager;

        private float playTimeBaselineRealtime;
        private int playTimeBaselineSlotIndex = -1;

        public static GameSaveManager Instance => instance;
        public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
        public int ActiveSlotIndex => activeSlotIndex;
        public bool HasSaveFile => File.Exists(SavePath);

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            ResetPlayTimeBaseline(activeSlotIndex);
        }

        private void Start()
        {
            if (SaveSlotRuntimeContext.HasPendingSlot)
            {
                activeSlotIndex = SaveSlotRuntimeContext.PendingSlotIndex;
                if (SaveSlotRuntimeContext.PendingLoadExistingSave)
                {
                    SaveSlotRuntimeContext.Clear();
                    LoadGameFromSlot(activeSlotIndex);
                    return;
                }

                SaveSlotRuntimeContext.Clear();
                ResetPlayTimeBaseline(activeSlotIndex);
            }

            if (loadOnStart)
                LoadGame();
        }

        private void OnApplicationQuit()
        {
            if (saveOnApplicationQuit)
                SaveActiveSlot();
        }

        [ContextMenu("Save Game")]
        public void SaveGame()
        {
            SaveGameToPath(SavePath, activeSlotIndex);
        }

        public void SaveActiveSlot()
        {
            if (activeSlotIndex < 0)
            {
                SaveGame();
                return;
            }

            SaveGameToSlot(activeSlotIndex);
        }

        public void SaveGameToSlot(int slotIndex)
        {
            SaveGameToPath(GetSlotSavePath(slotIndex), slotIndex);
            activeSlotIndex = slotIndex;
        }

        private void SaveGameToPath(string path, int slotIndex)
        {
            GameSaveData oldData = LoadDataFromPath(path);
            GameSaveData saveData = CaptureSaveData();
            saveData.slotIndex = slotIndex;
            if (string.IsNullOrWhiteSpace(saveData.saveDisplayName))
                saveData.saveDisplayName = "New Game";

            long now = DateTime.UtcNow.Ticks;
            if (oldData != null)
            {
                saveData.createdUtcTicks = oldData.createdUtcTicks > 0 ? oldData.createdUtcTicks : now;
                saveData.playTimeSeconds = oldData.playTimeSeconds + ConsumePlayTimeDelta(slotIndex);
            }
            else if (saveData.createdUtcTicks <= 0)
            {
                saveData.createdUtcTicks = now;
                saveData.playTimeSeconds = ConsumePlayTimeDelta(slotIndex);
            }

            saveData.updatedUtcTicks = now;

            string json = JsonUtility.ToJson(saveData, true);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, json);
            Debug.Log($"[GameSaveManager] Saved game to {path}");
        }

        [ContextMenu("Load Game")]
        public void LoadGame()
        {
            if (!HasSaveFile)
            {
                Debug.LogWarning($"[GameSaveManager] Save file not found: {SavePath}");
                return;
            }

            string json = File.ReadAllText(SavePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            if (saveData == null)
            {
                Debug.LogWarning("[GameSaveManager] Failed to parse save data.");
                return;
            }

            ApplySaveData(saveData);
            ResetPlayTimeBaseline(saveData.slotIndex);
            Debug.Log($"[GameSaveManager] Loaded game from {SavePath}");
        }

        public void LoadGameFromSlot(int slotIndex)
        {
            string path = GetSlotSavePath(slotIndex);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[GameSaveManager] Slot save file not found: {path}");
                return;
            }

            string json = File.ReadAllText(path);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            if (saveData == null)
            {
                Debug.LogWarning($"[GameSaveManager] Failed to parse slot save data: {path}");
                return;
            }

            activeSlotIndex = slotIndex;
            ApplySaveData(saveData);
            ResetPlayTimeBaseline(slotIndex);
            Debug.Log($"[GameSaveManager] Loaded slot {slotIndex} from {path}");
        }

        public void CreateNewGameSlot(int slotIndex)
        {
            activeSlotIndex = slotIndex;

            GameSaveData data = new GameSaveData
            {
                slotIndex = slotIndex,
                saveDisplayName = "New Game",
                createdUtcTicks = DateTime.UtcNow.Ticks,
                updatedUtcTicks = DateTime.UtcNow.Ticks
            };

            string path = GetSlotSavePath(slotIndex);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            ResetPlayTimeBaseline(slotIndex);
        }

        private float ConsumePlayTimeDelta(int slotIndex)
        {
            if (playTimeBaselineSlotIndex != slotIndex)
                ResetPlayTimeBaseline(slotIndex);

            float now = Time.unscaledTime;
            float delta = Mathf.Max(0f, now - playTimeBaselineRealtime);
            playTimeBaselineRealtime = now;
            return delta;
        }

        private void ResetPlayTimeBaseline(int slotIndex)
        {
            playTimeBaselineSlotIndex = slotIndex;
            playTimeBaselineRealtime = Time.unscaledTime;
        }

        private GameSaveData LoadDataFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GameSaveData>(json);
        }

        public bool HasSlotSave(int slotIndex)
        {
            return SaveSlotFileUtility.HasSlotSave(slotIndex);
        }

        public string GetSlotSavePath(int slotIndex)
        {
            return SaveSlotFileUtility.GetSlotSavePath(slotIndex);
        }

        public bool TryGetSlotSummary(int slotIndex, out SaveSlotSummary summary)
        {
            return SaveSlotFileUtility.TryGetSlotSummary(slotIndex, out summary);
        }

        [ContextMenu("Delete Save")]
        public void DeleteSave()
        {
            if (HasSaveFile)
                File.Delete(SavePath);
        }

        public GameSaveData CaptureSaveData()
        {
            ResolveSceneReferences();

            GameSaveData data = new GameSaveData();

            if (currencyWallet != null)
                data.gold = currencyWallet.CaptureSaveData();

            if (inventoryState != null)
                data.inventory = inventoryState.CaptureSaveData();

            if (toolEquip != null)
                data.toolEquip = toolEquip.CaptureSaveData();

            if (healingPotionEquip != null)
                data.healingPotionEquip = healingPotionEquip.CaptureSaveData();

            if (shopState != null)
            {
                data.shop = shopState.CaptureSaveData();
                data.hasShopData = true;
            }

            if (skillProgress != null)
            {
                data.skillProgress = skillProgress.CaptureSaveData();
                data.hasSkillProgressData = true;
            }

            if (facilityManager != null)
                data.facilityProgress = facilityManager.CaptureSaveData();

            if (skillTreeManager != null)
            {
                data.skillTreeProgress = skillTreeManager.CaptureSaveData();
                data.hasSkillTreeProgressData = true;
            }

            if (spellEquip != null)
                data.equippedSpellId = spellEquip.CaptureSaveData();

            PortalCheckpointService.CaptureUnlockedPortalIds(data.portals.unlockedPortalIds);
            SceneDoorPortal.CaptureUnlockedDoorIds(data.doors.unlockedDoorIds);
            WorldPickupSaveService.CaptureCollectedPickupIds(data.worldPickups.collectedPickupIds);
            BossDefeatSaveService.CaptureDefeatedBossIds(data.bossDefeats.defeatedBossIds);

            data.slotIndex = activeSlotIndex;
            data.saveDisplayName = "New Game";

            return data;
        }

        private static string FormatPlayTime(float seconds)
        {
            int totalMinutes = Mathf.Max(0, Mathf.FloorToInt(seconds / 60f));
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours}h {minutes:00}m";
        }

        public void ApplySaveData(GameSaveData data)
        {
            if (data == null)
                return;

            ResolveSceneReferences();

            if (currencyWallet != null)
                currencyWallet.LoadFromSaveData(data.gold);

            if (inventoryState != null)
                inventoryState.LoadFromSaveData(data.inventory, itemDatabase);

            if (toolEquip != null)
                toolEquip.LoadFromSaveData(data.toolEquip, itemDatabase);

            if (healingPotionEquip != null)
                healingPotionEquip.LoadFromSaveData(data.healingPotionEquip, itemDatabase);

            if (shopState != null)
            {
                if (data.hasShopData)
                    shopState.LoadFromSaveData(data.shop, itemDatabase);
                else
                    shopState.ResetFromCatalog();
            }

            if (skillProgress != null && data.hasSkillProgressData)
                skillProgress.LoadFromSaveData(data.skillProgress);

            if (facilityManager != null)
                facilityManager.LoadFromSaveData(data.facilityProgress);

            if (skillTreeManager != null && data.hasSkillTreeProgressData)
                skillTreeManager.LoadFromSaveData(data.skillTreeProgress);

            if (spellEquip != null)
                spellEquip.LoadFromSaveData(data.equippedSpellId, spellDatabase);

            PortalCheckpointService.LoadUnlockedPortalIds(data.portals != null ? data.portals.unlockedPortalIds : null);
            SceneDoorPortal.LoadUnlockedDoorIds(data.doors != null ? data.doors.unlockedDoorIds : null);
            WorldPickupSaveService.LoadCollectedPickupIds(data.worldPickups != null ? data.worldPickups.collectedPickupIds : null);
            BossDefeatSaveService.LoadDefeatedBossIds(data.bossDefeats != null ? data.bossDefeats.defeatedBossIds : null);

        }

        private void ResolveSceneReferences()
        {
            if (facilityManager == null)
                facilityManager = FindAnyObjectByType<FacilityManager>();

            if (skillTreeManager == null)
                skillTreeManager = FindAnyObjectByType<SkillTreeManager>();

            if (toolEquip == null && facilityManager != null)
                toolEquip = facilityManager.ToolEquip;
        }
    }
}
