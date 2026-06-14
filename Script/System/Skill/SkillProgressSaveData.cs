using System;
using System.Collections.Generic;

namespace DreamKnight.Systems.Skill
{
    [Serializable]
    public class SkillProgressSaveData
    {
        public List<SkillProgressEntry> entries = new List<SkillProgressEntry>();
    }

    [Serializable]
    public class SkillProgressEntry
    {
        public string spellId;
        public int level;
    }
}
