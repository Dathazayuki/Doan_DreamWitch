using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    [CreateAssetMenu(fileName = "SkillProgress", menuName = "DreamKnight/Skill/Skill Progress")]
    public class SkillProgressSO : ScriptableObject
    {
        [NonSerialized] private readonly Dictionary<string, int> levelsBySpellId = new Dictionary<string, int>();

        public event Action<string, int> OnSpellLevelChanged;

        public int GetLevel(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
                return 0;

            return levelsBySpellId.TryGetValue(spellId, out int level) ? level : 0;
        }

        public bool IsUnlocked(string spellId)
        {
            return GetLevel(spellId) > 0;
        }

        public void SetLevel(string spellId, int level)
        {
            if (string.IsNullOrWhiteSpace(spellId))
                return;

            int clamped = Mathf.Max(0, level);
            levelsBySpellId[spellId] = clamped;
            OnSpellLevelChanged?.Invoke(spellId, clamped);
        }

        public SkillProgressSaveData CaptureSaveData()
        {
            SkillProgressSaveData saveData = new SkillProgressSaveData();
            foreach (var pair in levelsBySpellId)
            {
                saveData.entries.Add(new SkillProgressEntry
                {
                    spellId = pair.Key,
                    level = pair.Value
                });
            }

            return saveData;
        }

        public void LoadFromSaveData(SkillProgressSaveData saveData)
        {
            levelsBySpellId.Clear();

            if (saveData != null && saveData.entries != null)
            {
                for (int i = 0; i < saveData.entries.Count; i++)
                {
                    SkillProgressEntry entry = saveData.entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.spellId))
                        continue;

                    levelsBySpellId[entry.spellId] = Mathf.Max(0, entry.level);
                }
            }
        }
    }
}
