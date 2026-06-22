using System;
using System.IO;
using UnityEngine;

namespace DreamKnight.Systems.SaveLoad
{
    public static class SaveSlotFileUtility
    {
        private const string SlotFileNameFormat = "save_slot_{0}.json";

        public static string GetSlotSavePath(int slotIndex)
        {
            int displaySlot = Mathf.Max(0, slotIndex) + 1;
            return Path.Combine(Application.persistentDataPath, string.Format(SlotFileNameFormat, displaySlot));
        }

        public static bool HasSlotSave(int slotIndex)
        {
            return File.Exists(GetSlotSavePath(slotIndex));
        }

        public static bool TryLoadSlotData(int slotIndex, out GameSaveData data)
        {
            data = null;
            string path = GetSlotSavePath(slotIndex);
            if (!File.Exists(path))
                return false;

            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<GameSaveData>(json);
            return data != null;
        }

        public static bool TryGetSlotSummary(int slotIndex, out SaveSlotSummary summary)
        {
            summary = new SaveSlotSummary
            {
                slotIndex = slotIndex,
                hasSave = false
            };

            if (!TryLoadSlotData(slotIndex, out GameSaveData data))
                return false;

            summary.hasSave = true;
            summary.displayName = string.IsNullOrWhiteSpace(data.saveDisplayName) ? "New Game" : data.saveDisplayName;
            summary.gold = data.gold;
            summary.playTimeText = FormatPlayTime(data.playTimeSeconds);
            return true;
        }

        public static void CreateNewGameSlot(int slotIndex)
        {
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
        }

        public static void DeleteSlotSave(int slotIndex)
        {
            string path = GetSlotSavePath(slotIndex);
            if (File.Exists(path))
                File.Delete(path);
        }

        private static string FormatPlayTime(float seconds)
        {
            int totalMinutes = Mathf.Max(0, Mathf.FloorToInt(seconds / 60f));
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours}h {minutes:00}m";
        }
    }
}
