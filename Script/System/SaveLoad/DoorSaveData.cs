using System;
using System.Collections.Generic;

namespace DreamKnight.Systems.SaveLoad
{
    [Serializable]
    public class DoorSaveData
    {
        public List<string> unlockedDoorIds = new List<string>();
    }
}
