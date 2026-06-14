namespace Mv
{
    /// <summary>
    /// Định nghĩa chế độ chiến đấu của Enemy.
    /// <list type="bullet">
    ///   <item><term>World</term><description>Patrol → Detect → Chase → Mất dấu → Return (mặc định)</description></item>
    ///   <item><term>Arena</term><description>Luôn biết vị trí Player, không bao giờ mất dấu, không patrol.</description></item>
    /// </list>
    /// </summary>
    public enum EnemyCombatMode : byte
    {
        /// <summary>Hành vi thế giới bình thường: patrol, phát hiện, đuổi, mất dấu.</summary>
        World = 0,

        /// <summary>Hành vi arena: luôn theo dõi Player, không bao giờ mất target.</summary>
        Arena = 1,
    }
}
