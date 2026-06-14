using System;
using System.Collections.Generic;

namespace DreamKnight.Systems.SaveLoad
{
    [Serializable]
    public class WorldPickupSaveData
    {
        public List<string> collectedPickupIds = new List<string>();
    }
}
