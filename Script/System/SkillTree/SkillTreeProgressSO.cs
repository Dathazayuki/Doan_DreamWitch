using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.SkillTree
{
    [CreateAssetMenu(fileName = "SkillTreeProgress", menuName = "DreamKnight/Skill Tree/Progress")]
    public class SkillTreeProgressSO : ScriptableObject
    {
        [SerializeField] private List<string> initialUnlockedNodeIds = new List<string>();

        [NonSerialized] private HashSet<string> unlockedNodeIds;

        public event Action<string> OnNodeUnlocked;

        public bool IsUnlocked(string nodeId)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(nodeId) && unlockedNodeIds.Contains(nodeId);
        }

        public bool Unlock(string nodeId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(nodeId) || !unlockedNodeIds.Add(nodeId))
                return false;

            OnNodeUnlocked?.Invoke(nodeId);
            return true;
        }

        public SkillTreeProgressSaveData CaptureSaveData()
        {
            EnsureInitialized();
            return new SkillTreeProgressSaveData
            {
                unlockedNodeIds = new List<string>(unlockedNodeIds)
            };
        }

        public void LoadFromSaveData(SkillTreeProgressSaveData saveData)
        {
            unlockedNodeIds = new HashSet<string>();

            if (saveData != null && saveData.unlockedNodeIds != null)
            {
                for (int i = 0; i < saveData.unlockedNodeIds.Count; i++)
                {
                    string nodeId = saveData.unlockedNodeIds[i];
                    if (!string.IsNullOrWhiteSpace(nodeId))
                        unlockedNodeIds.Add(nodeId);
                }
            }
        }

        public void ResetToInitial()
        {
            unlockedNodeIds = new HashSet<string>();
            if (initialUnlockedNodeIds == null)
                return;

            for (int i = 0; i < initialUnlockedNodeIds.Count; i++)
            {
                string nodeId = initialUnlockedNodeIds[i];
                if (!string.IsNullOrWhiteSpace(nodeId))
                    unlockedNodeIds.Add(nodeId);
            }
        }

        private void EnsureInitialized()
        {
            if (unlockedNodeIds != null)
                return;

            ResetToInitial();
        }
    }
}
