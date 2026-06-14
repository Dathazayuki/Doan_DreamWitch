using UnityEngine;

namespace DreamKnight.Interfaces
{
    /// <summary>
    /// Interface cho các đối tượng có thể nhận damage
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        float CurrentHealth { get; }
        void TakeDamage(float damage, GameObject damageSource = null);
    }
}
