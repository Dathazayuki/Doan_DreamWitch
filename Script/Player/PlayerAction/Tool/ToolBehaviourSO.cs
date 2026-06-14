using DreamKnight.Systems.Inventory;
using UnityEngine;

namespace DreamKnight.Player
{
    /// <summary>
    /// Abstract base cho mọi hành vi tool (Strategy Pattern).
    /// Mỗi loại tool có 1 class con: KnifeThrowBehaviourSO, PoisonBuffBehaviourSO, ...
    ///
    /// Gắn vào ToolItemSO.behaviour trong Inspector.
    /// </summary>
    public abstract class ToolBehaviourSO : ScriptableObject
    {
        /// <summary>
        /// Thực thi hành vi của tool.
        /// </summary>
        /// <param name="context">Thông tin player/game lúc dùng tool.</param>
        /// <param name="toolAction">Component gọi hành vi, dùng để lấy Transform, PlayerController, ...</param>
        /// <returns>true nếu thực thi thành công.</returns>
        public abstract bool Use(ItemUseContext context, PlayerToolAction toolAction);
    }
}
