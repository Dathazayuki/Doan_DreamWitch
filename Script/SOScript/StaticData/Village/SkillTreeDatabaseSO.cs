using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.SkillTree
{
    [CreateAssetMenu(fileName = "SkillTreeDatabase", menuName = "DreamKnight/Skill Tree/Database")]
    public class SkillTreeDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<SkillTreeNodeSO> nodes = new List<SkillTreeNodeSO>();

        public IReadOnlyList<SkillTreeNodeSO> Nodes => nodes;

        public SkillTreeNodeSO FindById(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                SkillTreeNodeSO node = nodes[i];
                if (node != null && node.NodeId == nodeId)
                    return node;
            }

            return null;
        }
    }
}
