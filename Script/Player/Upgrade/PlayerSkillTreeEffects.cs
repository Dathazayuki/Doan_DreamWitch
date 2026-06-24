using DreamKnight.Systems.SkillTree;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerSkillTreeEffects : MonoBehaviour
    {
        [Header("Node 7 Spell Book")]
        [SerializeField] private GameObject spellBookObject;

        private SkillTreeManager skillTreeManager;

        private void OnEnable()
        {
            ResolveManager();
            Subscribe();
            RefreshSpellBookObject();
        }

        private void Start()
        {
            ResolveManager();
            Subscribe();
            RefreshSpellBookObject();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (skillTreeManager != null)
                skillTreeManager.OnNodeUnlocked -= HandleNodeUnlocked;
            if (skillTreeManager != null)
                skillTreeManager.OnProgressChanged -= RefreshSpellBookObject;

            if (skillTreeManager != null)
            {
                skillTreeManager.OnNodeUnlocked += HandleNodeUnlocked;
                skillTreeManager.OnProgressChanged += RefreshSpellBookObject;
            }
        }

        private void Unsubscribe()
        {
            if (skillTreeManager != null)
            {
                skillTreeManager.OnNodeUnlocked -= HandleNodeUnlocked;
                skillTreeManager.OnProgressChanged -= RefreshSpellBookObject;
            }
        }

        private void ResolveManager()
        {
            if (skillTreeManager == null)
                skillTreeManager = SkillTreeManager.Instance;
        }

        private void HandleNodeUnlocked(SkillTreeNodeSO node)
        {
            if (node != null && node.EffectType == SkillTreeEffectType.SpellBookSpellDamagePercent)
                RefreshSpellBookObject();
        }

        private void RefreshSpellBookObject()
        {
            if (spellBookObject == null)
                return;

            bool unlocked = skillTreeManager != null && skillTreeManager.IsSpellBookSpellDamageUnlocked();
            if (spellBookObject.activeSelf != unlocked)
                spellBookObject.SetActive(unlocked);
        }
    }
}
