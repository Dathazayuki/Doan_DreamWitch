using System;
using System.Collections.Generic;

namespace DreamKnight.Systems.SaveLoad
{
    public static class WorldPickupSaveService
    {
        private static readonly HashSet<string> collectedPickupIds = new HashSet<string>(StringComparer.Ordinal);
        public static event Action OnCollectedStateLoaded;

        public static bool IsCollected(string pickupId)
        {
            return !string.IsNullOrWhiteSpace(pickupId) && collectedPickupIds.Contains(pickupId);
        }

        public static void MarkCollected(string pickupId)
        {
            if (string.IsNullOrWhiteSpace(pickupId))
                return;

            collectedPickupIds.Add(pickupId);
        }

        public static void CaptureCollectedPickupIds(List<string> output)
        {
            if (output == null)
                return;

            output.Clear();
            foreach (string pickupId in collectedPickupIds)
            {
                if (!string.IsNullOrWhiteSpace(pickupId))
                    output.Add(pickupId);
            }
        }

        public static void LoadCollectedPickupIds(IEnumerable<string> pickupIds)
        {
            collectedPickupIds.Clear();

            if (pickupIds != null)
            {
                foreach (string pickupId in pickupIds)
                {
                    if (!string.IsNullOrWhiteSpace(pickupId))
                        collectedPickupIds.Add(pickupId);
                }
            }

            OnCollectedStateLoaded?.Invoke();
        }

        public static void Clear()
        {
            collectedPickupIds.Clear();
            OnCollectedStateLoaded?.Invoke();
        }
    }
}
