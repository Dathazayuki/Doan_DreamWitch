using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Facility
{
    [CreateAssetMenu(fileName = "FacilityProgress", menuName = "DreamKnight/Facility/Progress")]
    public class FacilityProgressSO : ScriptableObject
    {
        [NonSerialized] private readonly Dictionary<string, int> levelsByUpgradeId = new Dictionary<string, int>();

        public event Action<string, int> OnUpgradeLevelChanged;

        public int GetLevel(string upgradeId)
        {
            if (string.IsNullOrWhiteSpace(upgradeId))
                return 0;

            return levelsByUpgradeId.TryGetValue(upgradeId, out int level) ? level : 0;
        }

        public void SetLevel(string upgradeId, int level)
        {
            if (string.IsNullOrWhiteSpace(upgradeId))
                return;

            int clamped = Mathf.Max(0, level);
            if (GetLevel(upgradeId) == clamped)
                return;

            levelsByUpgradeId[upgradeId] = clamped;
            OnUpgradeLevelChanged?.Invoke(upgradeId, clamped);
        }

        public void ResetProgress()
        {
            if (levelsByUpgradeId.Count == 0)
                return;

            levelsByUpgradeId.Clear();
            OnUpgradeLevelChanged?.Invoke(string.Empty, 0);
        }

        public FacilityProgressSaveData CaptureSaveData()
        {
            FacilityProgressSaveData saveData = new FacilityProgressSaveData();

            foreach (KeyValuePair<string, int> pair in levelsByUpgradeId)
            {
                saveData.entries.Add(new FacilityProgressEntry
                {
                    upgradeId = pair.Key,
                    level = pair.Value
                });
            }

            return saveData;
        }

        public void LoadFromSaveData(FacilityProgressSaveData saveData)
        {
            levelsByUpgradeId.Clear();

            if (saveData != null && saveData.entries != null)
            {
                for (int i = 0; i < saveData.entries.Count; i++)
                {
                    FacilityProgressEntry entry = saveData.entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.upgradeId))
                        continue;

                    levelsByUpgradeId[entry.upgradeId] = Mathf.Max(0, entry.level);
                }
            }

            OnUpgradeLevelChanged?.Invoke(string.Empty, 0);
        }
    }
}
