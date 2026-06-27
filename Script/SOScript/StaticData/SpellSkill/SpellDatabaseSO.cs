using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    [CreateAssetMenu(fileName = "SpellDatabase", menuName = "DreamKnight/Skill/Spell Database")]
    public class SpellDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<SpellData> spells = new List<SpellData>();

        public IReadOnlyList<SpellData> Spells => spells;

        public SpellData FindById(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
                return null;

            for (int i = 0; i < spells.Count; i++)
            {
                SpellData spell = spells[i];
                if (spell != null && spell.spellId == spellId)
                    return spell;
            }

            return null;
        }
    }
}
