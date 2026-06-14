using DreamKnight.Enemy;
using DreamKnight.Player;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0100 : MvEnemyBase
    {
        // ──────────────────────────────────────────────────────────────
        //  State IDs riêng của Em0100 (bắt đầu từ AsCommon.Max)
        // ──────────────────────────────────────────────────────────────
        public enum As : byte
        {
            PunchCombo = (byte)AsCommon.Max, // Chuỗi 3 đòn Punch1→Punch2→Punch3
            PunchSide,                        // Đòn đánh 2 phía PunchSide
            Max
        }

        // ──────────────────────────────────────────────────────────────
        //  Inspector
        // ──────────────────────────────────────────────────────────────
        [Header("Em0100 – Attack Types")]
        [Tooltip("True = ưu tiên PunchCombo nếu trong tầm, False = chỉ dùng PunchSide.")]
        [SerializeField] private bool usePunchComboWhenInRange = true;

        [Header("Attack Components")]
        [Tooltip("MvAttack dùng cho cậuỗi 3 đòn PunchCombo. Gắn BoxCollider2D hitbox phía trước.")]
        [SerializeField] private MvAttack punchComboAttack;
        [Tooltip("MvAttack dùng cho đòn PunchSide (2 phía). Gắn BoxCollider2D hitbox rộng cả 2 phía.")]
        [SerializeField] private MvAttack punchSideAttack;

        [Header("Punch Combo Animator State Names")]
        [SerializeField] private string punchSignStateName  = "PunchSign";
        [SerializeField] private string punch1StateName     = "Punch1";
        [SerializeField] private string punch2StateName     = "Punch2";
        [SerializeField] private string punch3StateName     = "Punch3";

        [Header("PunchSide Animator State Names")]
        [SerializeField] private string punchSideSignStateName = "PunchSideSign";
        [SerializeField] private string punchSideStateName     = "PunchSide";

        [Header("Transform Unlock")]
        [SerializeField] private bool spawnTransformUnlockOnDeath = true;
        [SerializeField] private PlayerFormDataSO unlockFormData;
        [SerializeField] private EnemyTransformUnlockPickup transformUnlockZone;

        // ──────────────────────────────────────────────────────────────
        //  Animation state name properties (cho state classes dùng)
        // ──────────────────────────────────────────────────────────────
        public string PunchSignStateName     => punchSignStateName;
        public string Punch1StateName        => punch1StateName;
        public string Punch2StateName        => punch2StateName;
        public string Punch3StateName        => punch3StateName;
        public string PunchSideSignStateName => punchSideSignStateName;
        public string PunchSideStateName     => punchSideStateName;

        // ──────────────────────────────────────────────────────────────
        //  Base class animator state names
        // ──────────────────────────────────────────────────────────────
        protected override string IdleStateName        => "Idle";
        protected override string RunStateName         => "Run";
        // AttackSignStateName / AttackStateName là của AsEm_AtkWithSign_Base (base check).
        // Em0100 không dùng class đó trực tiếp — override để tránh null warning từ
        // IsAttackAnimFinished() khi nó so sánh tên animator state.
        protected override string AttackSignStateName  => punchSignStateName;
        protected override string AttackStateName      => punch1StateName;
        protected override string HitStateName         => "Hit";
        protected override string DeadStateName        => "Death";

        // ──────────────────────────────────────────────────────────────
        //  Attack decision helpers
        // ──────────────────────────────────────────────────────────────
        /// <summary>
        /// True nếu player trong tầm PunchCombo và option usePunchComboWhenInRange = true.
        /// </summary>
        public bool ShouldUsePunchCombo
        {
            get
            {
                if (!usePunchComboWhenInRange) return false;

                // Sử dụng trực tiếp vùng trigger của punchComboAttack để nhận dạng Player ở gần
                return punchComboAttack != null 
                    && punchComboAttack.HasAttackTrigger 
                    && punchComboAttack.IsPlayerInsideAttackTrigger();
            }
        }

        protected override bool TryEvaluateCustomAttackRange(float absX, float absY, float edgeDistanceX, out bool inAttackRange)
        {
            // Kiểm tra xem player có nằm trong bất kỳ hitbox trigger nào của 2 đòn đánh hay không
            bool insideCombo = punchComboAttack != null && punchComboAttack.HasAttackTrigger && punchComboAttack.IsPlayerInsideAttackTrigger();
            bool insideSide = punchSideAttack != null && punchSideAttack.HasAttackTrigger && punchSideAttack.IsPlayerInsideAttackTrigger();

            inAttackRange = insideCombo || insideSide;
            return true;
        }

        // ──────────────────────────────────────────────────────────────
        //  Lifecycle
        // ──────────────────────────────────────────────────────────────
        protected override void Awake()
        {
            // Resolve attack references trước khi gọi base.Awake()
            // để base không tự dò MvAttack ngẫu nhiên.
            // Nếu chưa gán trên Inspector, tự dò từ GetComponents.
            MvAttack[] allAttacks = GetComponentsInChildren<MvAttack>(true);
            if (punchComboAttack == null && allAttacks.Length > 0)
                punchComboAttack = allAttacks[0];
            if (punchSideAttack == null && allAttacks.Length > 1)
                punchSideAttack = allAttacks[1];

            // Đặt punchComboAttack làm active mặc định (base sẽ dùng đây qua SetActiveAttack)
            if (punchComboAttack != null)
                SetActiveAttack(punchComboAttack);

            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
        }

        // ──────────────────────────────────────────────────────────────
        //  State factory overrides
        // ──────────────────────────────────────────────────────────────
        protected override EnemyState CreateIdleState(EnemyContext context)
            => new Em0100IdleState(context);

        protected override EnemyState CreateRunState(EnemyContext context)
            => new Em0100RunState(context);

        protected override EnemyState CreateAttackState(EnemyContext context)
        {
            // AttackState mặc định của base = PunchCombo (StateId = AsCommon.Max)
            return new Em0100PunchComboState(context);
        }

        protected override EnemyState CreateAtkAfterState(EnemyContext context)
        {
            // Sử dụng Em0100AtkAfterState để sau mỗi đòn attack router đúng về PunchCombo/PunchSide
            return new Em0100AtkAfterState(context);
        }

        protected override void RegisterAdditionalStates(EnemyStateMachine stateMachine, EnemyContext context)
        {
            // PunchSide là attack type thứ 2, dùng khi player đứng xa hơn punchComboMaxRange
            stateMachine?.Register(new Em0100PunchSideState(context));
        }

        // ──────────────────────────────────────────────────────────────
        //  State IDs
        // ──────────────────────────────────────────────────────────────
        public byte PunchComboStateId => (byte)As.PunchCombo;
        public byte PunchSideStateId  => (byte)As.PunchSide;

        // ──────────────────────────────────────────────────────────────
        //  Attack switching helpers — gọi trong Enter() của Attack State
        // ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Kích hoạt MvAttack của cẫuỗi 3 đòn. Gọi trước khi vào Em0100PunchComboState.
        /// </summary>
        public void UsePunchComboAttack()
        {
            if (punchComboAttack != null)
                SetActiveAttack(punchComboAttack);
        }

        /// <summary>
        /// Kích hoạt MvAttack của đòn 2 phía. Gọi trước khi vào Em0100PunchSideState.
        /// </summary>
        public void UsePunchSideAttack()
        {
            if (punchSideAttack != null)
                SetActiveAttack(punchSideAttack);
        }

        // ──────────────────────────────────────────────────────────────
        //  Death / Transform Unlock
        // ──────────────────────────────────────────────────────────────
        protected override void Die()
        {
            base.Die(); // base.Die() đã disable attack hiện tại (active attack)

            // Disable cả component còn lại (không phải active)
            if (punchSideAttack != null && punchSideAttack.enabled)
                punchSideAttack.enabled = false;
            if (punchComboAttack != null && punchComboAttack.enabled)
                punchComboAttack.enabled = false;

            if (spawnTransformUnlockOnDeath)
                ActivateTransformUnlockZone();
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }

        private void ActivateTransformUnlockZone()
        {
            PlayerFormDataSO resolvedForm = ResolveUnlockFormData();
            if (resolvedForm == null)
                return;

            EnemyTransformUnlockPickup zone = ResolveTransformUnlockZone();
            if (zone == null)
            {
                Debug.LogWarning("[MvEm0100] Transform unlock zone not found.", this);
                return;
            }

            zone.Initialize(resolvedForm, gameObject);
            zone.gameObject.SetActive(true);
            EnableTransformUnlockZoneColliders(zone);
        }

        private void EnableTransformUnlockZoneColliders(EnemyTransformUnlockPickup zone)
        {
            if (zone == null)
                return;

            Collider2D[] colliders = zone.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col == null) continue;
                col.enabled   = true;
                col.isTrigger = true;
            }

            int layer = LayerMask.NameToLayer("EnemyCanTranform");
            if (layer >= 0)
                SetLayerRecursively(zone.transform, layer);
        }

        private void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null) return;
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        private EnemyTransformUnlockPickup ResolveTransformUnlockZone()
        {
            if (transformUnlockZone != null)
                return transformUnlockZone;

            transformUnlockZone = GetComponentInChildren<EnemyTransformUnlockPickup>(true);
            if (transformUnlockZone != null)
                return transformUnlockZone;

            Transform layered = FindChildByLayer("EnemyCanTranform");
            if (layered == null) return null;

            transformUnlockZone = layered.GetComponent<EnemyTransformUnlockPickup>();
            if (transformUnlockZone == null)
                transformUnlockZone = layered.gameObject.AddComponent<EnemyTransformUnlockPickup>();

            return transformUnlockZone;
        }

        private Transform FindChildByLayer(string layerName)
        {
            int targetLayer = LayerMask.NameToLayer(layerName);
            if (targetLayer < 0) return null;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].gameObject.layer == targetLayer)
                    return children[i];
            }
            return null;
        }

        private PlayerFormDataSO ResolveUnlockFormData()
        {
            if (unlockFormData != null)
                return unlockFormData;

            PlayerFormConfig config = FindAnyObjectByType<PlayerFormConfig>();
            if (config == null)
                return null;

            for (int i = 0; i < config.forms.Count; i++)
            {
                PlayerFormDataSO entry = config.forms[i];
                if (entry == null || entry.enemySourcePrefab == null)
                    continue;

                if (entry.enemySourcePrefab.GetComponent<MvEm0100>() != null)
                    return entry;
            }

            return null;
        }
    }
}
