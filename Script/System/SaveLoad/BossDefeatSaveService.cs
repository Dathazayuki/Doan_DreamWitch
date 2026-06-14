using System;
using System.Collections.Generic;

namespace DreamKnight.Systems.SaveLoad
{
    public static class BossDefeatSaveService
    {
        private static readonly HashSet<string> defeatedBossIds = new HashSet<string>(StringComparer.Ordinal);
        public static event Action OnDefeatStateLoaded;

        public static bool IsDefeated(string bossId)
        {
            return !string.IsNullOrWhiteSpace(bossId) && defeatedBossIds.Contains(bossId);
        }

        public static void MarkDefeated(string bossId)
        {
            if (string.IsNullOrWhiteSpace(bossId))
                return;

            defeatedBossIds.Add(bossId);
        }

        public static void CaptureDefeatedBossIds(List<string> output)
        {
            if (output == null)
                return;

            output.Clear();
            foreach (string bossId in defeatedBossIds)
            {
                if (!string.IsNullOrWhiteSpace(bossId))
                    output.Add(bossId);
            }
        }

        public static void LoadDefeatedBossIds(IEnumerable<string> bossIds)
        {
            defeatedBossIds.Clear();

            if (bossIds != null)
            {
                foreach (string bossId in bossIds)
                {
                    if (!string.IsNullOrWhiteSpace(bossId))
                        defeatedBossIds.Add(bossId);
                }
            }

            OnDefeatStateLoaded?.Invoke();
        }

        public static void Clear()
        {
            defeatedBossIds.Clear();
            OnDefeatStateLoaded?.Invoke();
        }
    }
}
