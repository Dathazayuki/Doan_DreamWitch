using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Zone
{
    [CreateAssetMenu(fileName = "PortalUnlockState", menuName = "DreamKnight/Zone/Portal Unlock State")]
    public class PortalUnlockStateSO : ScriptableObject
    {
        [SerializeField] private List<string> unlockedPortalIds = new List<string>(16);

        private readonly HashSet<string> unlockedSet = new HashSet<string>(StringComparer.Ordinal);
        private bool cacheBuilt;

        public bool IsUnlocked(string portalId)
        {
            if (string.IsNullOrWhiteSpace(portalId))
                return false;

            EnsureCache();
            return unlockedSet.Contains(portalId);
        }

        public bool Unlock(string portalId)
        {
            if (string.IsNullOrWhiteSpace(portalId))
                return false;

            EnsureCache();
            if (!unlockedSet.Add(portalId))
                return false;

            unlockedPortalIds.Add(portalId);
            return true;
        }

        public void ResetState()
        {
            unlockedPortalIds.Clear();
            unlockedSet.Clear();
            cacheBuilt = true;
        }

        public void GetUnlockedPortalIds(List<string> output)
        {
            if (output == null)
                return;

            EnsureCache();
            output.Clear();
            for (int i = 0; i < unlockedPortalIds.Count; i++)
            {
                string portalId = unlockedPortalIds[i];
                if (!string.IsNullOrWhiteSpace(portalId))
                    output.Add(portalId);
            }
        }

        public void LoadUnlockedPortalIds(IEnumerable<string> portalIds)
        {
            unlockedPortalIds.Clear();
            unlockedSet.Clear();

            if (portalIds != null)
            {
                foreach (string portalId in portalIds)
                {
                    if (string.IsNullOrWhiteSpace(portalId))
                        continue;

                    if (!unlockedSet.Add(portalId))
                        continue;

                    unlockedPortalIds.Add(portalId);
                }
            }

            cacheBuilt = true;
        }

        private void OnEnable()
        {
            RebuildCache();
        }

        private void OnValidate()
        {
            RebuildCache();
        }

        private void EnsureCache()
        {
            if (cacheBuilt)
                return;

            RebuildCache();
        }

        private void RebuildCache()
        {
            unlockedSet.Clear();
            for (int i = unlockedPortalIds.Count - 1; i >= 0; i--)
            {
                string portalId = unlockedPortalIds[i];
                if (string.IsNullOrWhiteSpace(portalId) || !unlockedSet.Add(portalId))
                    unlockedPortalIds.RemoveAt(i);
            }

            cacheBuilt = true;
        }
    }
}
