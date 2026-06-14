namespace DreamKnight.Interfaces
{
    /// <summary>
    /// Interface cho các đối tượng có thể bị culling (freeze/sleep) bởi hệ thống Distance/Room Culling.
    /// Cull()   = vô hiệu hóa logic (AI, particles, v.v.)
    /// UnCull() = kích hoạt lại logic
    /// </summary>
    public interface ICullable
    {
        bool IsCulled { get; }
        void Cull();
        void UnCull();
    }
}
