using DreamKnight.Player;
using UnityEngine;

namespace DreamKnight.Systems.Inventory
{
    /// <summary>
    /// ScriptableObject đại diện cho bất kỳ tool nào trong inventory.
    /// Hành vi cụ thể được uỷ quyền hoàn toàn cho ToolBehaviourSO (Strategy Pattern).
    ///
    /// Cách tạo: Create → DreamKnight/Inventory/Items/Tool Item
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewToolItem",
        menuName  = "DreamKnight/Inventory/Items/Tool Item")]
    public class ToolItemSO : ItemDefinitionSO
    {
        [Header("Behaviour")]
        [Tooltip("ScriptableObject chứa logic dùng tool (KnifeThrowBehaviourSO, ...)")]
        [SerializeField] private ToolBehaviourSO behaviour;

        [Header("Usage")]
        [SerializeField] private int usesPerPurchase = 1;

        public ToolBehaviourSO Behaviour    => behaviour;
        public int UsesPerPurchase          => usesPerPurchase;

        public override bool Use(ItemUseContext context)
        {
            // Không dùng overload này — cần PlayerToolAction để lấy Transform/PlayerController
            // PlayerToolAction sẽ gọi Use(context, toolAction) trực tiếp trên behaviour
            Debug.LogWarning($"[ToolItemSO] '{name}': Dùng Use(context, toolAction) thay vì Use(context).");
            return false;
        }

        /// <summary>
        /// Thực thi tool thông qua behaviour.
        /// Gọi từ PlayerToolAction.TryUse().
        /// </summary>
        public bool Use(ItemUseContext context, PlayerToolAction toolAction)
        {
            if (behaviour == null)
            {
                Debug.LogWarning($"[ToolItemSO] '{name}' chưa gán ToolBehaviour!");
                return false;
            }

            return behaviour.Use(context, toolAction);
        }
    }
}
