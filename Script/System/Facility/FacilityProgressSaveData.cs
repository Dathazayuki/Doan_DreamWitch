using System;
using System.Collections.Generic;

namespace DreamKnight.Systems.Facility
{
    [Serializable]
    public class FacilityProgressSaveData
    {
        public List<FacilityProgressEntry> entries = new List<FacilityProgressEntry>();
    }

    [Serializable]
    public class FacilityProgressEntry
    {
        public string upgradeId;
        public int level;
    }
}
