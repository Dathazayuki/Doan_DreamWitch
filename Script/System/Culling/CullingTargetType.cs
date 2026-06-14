namespace DreamKnight.Systems.Culling
{
    /// <summary>
    /// Phân loại đối tượng để CullingManager áp dụng ngưỡng phù hợp.
    /// </summary>
    public enum CullingTargetType : byte
    {
        Enemy      = 0,
        Projectile = 1,
        Vfx        = 2,
    }
}
