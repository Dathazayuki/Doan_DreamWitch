using UnityEngine;

namespace DreamKnight.Systems.Skill
{
    public interface ISpellCast
    {
        // Called once when the spell is used.
        // 'caster' is the GameObject that used the spell, 'levelData' contains runtime values.
        void Cast(GameObject caster, SpellLevelData levelData);
    }
}
