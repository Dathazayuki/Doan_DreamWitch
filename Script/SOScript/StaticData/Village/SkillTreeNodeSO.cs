using System.Collections.Generic;
using DreamKnight.Systems.Inventory;
using UnityEngine;

namespace DreamKnight.Systems.SkillTree
{
    public enum SkillTreeEffectType
    {
        None,
        UnlockTransform,
        ComboThirdHitRestoreMana,
        ComboThirdHitExtraDamage,
        DashInvincible,
        CriticalDamagePercent,
        CriticalRatePercent,
        SpellBookSpellDamagePercent
    }

    [CreateAssetMenu(fileName = "SkillTreeNode", menuName = "DreamKnight/Skill Tree/Node")]
    public class SkillTreeNodeSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string nodeId;
        [SerializeField] private string displayName;
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        [Header("Unlock")]
        [SerializeField] private ItemDefinitionSO requiredItem;
        [Min(1)]
        [SerializeField] private int requiredItemQuantity = 1;
        [SerializeField] private List<string> prerequisiteNodeIds = new List<string>();

        [Header("Effect")]
        [SerializeField] private SkillTreeEffectType effectType;
        [SerializeField] private float effectValue;

        public string NodeId => string.IsNullOrWhiteSpace(nodeId) ? name : nodeId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemDefinitionSO RequiredItem => requiredItem;
        public int RequiredItemQuantity => Mathf.Max(1, requiredItemQuantity);
        public IReadOnlyList<string> PrerequisiteNodeIds => prerequisiteNodeIds;
        public SkillTreeEffectType EffectType => effectType;
        public float EffectValue => effectValue;
    }
}
