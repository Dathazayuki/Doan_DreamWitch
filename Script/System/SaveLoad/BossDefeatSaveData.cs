using System;
using System.Collections.Generic;

namespace DreamKnight.Systems.SaveLoad
{
    [Serializable]
    public class BossDefeatSaveData
    {
        public List<string> defeatedBossIds = new List<string>();
    }
}
