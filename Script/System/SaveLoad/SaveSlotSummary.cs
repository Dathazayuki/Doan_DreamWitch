using System;

namespace DreamKnight.Systems.SaveLoad
{
    [Serializable]
    public class SaveSlotSummary
    {
        public int slotIndex;
        public bool hasSave;
        public string displayName;
        public int gold;
        public string playTimeText;
    }
}
