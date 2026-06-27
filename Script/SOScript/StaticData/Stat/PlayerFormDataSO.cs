using System;
using UnityEngine;

namespace DreamKnight.Player
{
    public enum PlayerFormId
    {
        Human = 0,
        Em0010 = 1,
        Em0020 = 2,
        Em0060 = 3,
        Em0070 = 4,
        Em0100 = 5
    }

    /// <summary>
    /// Cấu hình damage riêng cho từng dạng biến hình.
    /// Nếu không gán, PlayerCombat sẽ dùng thông số mặc định của nó.
    /// </summary>
    [Serializable]
    public class FormCombatProfile
    {
        [Tooltip("Damage cho từng đòn combo (index 0 = đòn 1, 1 = đòn 2, ...). Để trống = dùng mặc định của PlayerCombat.")]
        public float[] comboDamagePerStep = { 20f, 22f, 30f };

        [Tooltip("Damage đòn tấn công lên (Up Attack).")]
        public float upAttackDamage = 25f;

        [Tooltip("Damage đòn tấn công nặng (Heavy Strike). Chỉ áp dụng cho form có cơ chế này.")]
        public float heavyStrikeDamage = 50f;
    }

    /// <summary>
    /// ScriptableObject chứa toàn bộ dữ liệu tĩnh (design-time) cho một dạng biến hình.
    /// Tạo asset: chuột phải → Create → DreamKnight → Form Data
    /// </summary>
    [CreateAssetMenu(fileName = "FormData_Em0000", menuName = "DreamKnight/Form Data")]
    public class PlayerFormDataSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Tên hiển thị trên UI.")]
        public string displayName;

        [Tooltip("ID định danh của form này — phải khớp với PlayerFormId enum.")]
        public PlayerFormId formId;

        [Header("Visuals")]
        [Tooltip("AnimatorController riêng khi đang ở form này.")]
        public RuntimeAnimatorController animatorController;

        [Tooltip("Icon hiển thị trên HUD.")]
        public Sprite formIcon;

        [Header("Stats")]
        [Tooltip("HP tối đa của form này.")]
        public float maxHealth = 100f;

        [Tooltip("Thời gian tối đa chờ animation respawn-gush trước khi force exit.")]
        public float respawnGushTimeout = 0.6f;

        [Header("Prefab")]
        [Tooltip("Prefab chứa toàn bộ cấu trúc vật lý: Collider, Hitbox, WallCheck, GroundCheck...")]
        public GameObject formPrefab;

        [Header("Enemy Source")]
        [Tooltip("Enemy tương ứng để unlock form này. Có thể để null nếu unlock bằng cách khác.")]
        public GameObject enemySourcePrefab;

        [Header("Combat")]
        [Tooltip("Thông số combat riêng cho form này. Để null = dùng thông số mặc định của PlayerCombat.")]
        public FormCombatProfile combatProfile;
    }
}
