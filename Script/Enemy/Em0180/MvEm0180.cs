using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0180 : MvEnemyBase
    {
        // ----------------------------------------------------------------
        // State IDs riêng của Em0180 (bắt đầu từ AsCommon.Max)
        // ----------------------------------------------------------------
        public enum As : byte
        {
            Atk1 = (byte)AsCommon.Max,  // Đòn cận chiến khi Player đứng gần
            Atk2,                        // Đòn lao vào khi Player đứng xa
            Max
        }

        // ----------------------------------------------------------------
        // Inspector
        // ----------------------------------------------------------------
        [Header("Em0180 – Dual Attack")]
        [Tooltip("Khoảng cách tối đa để dùng Atk1 (cận chiến). Vượt quá → dùng Atk2")]
        [SerializeField] private float atk1MaxRange = 1.8f;

        [Header("Atk2 – Dash Attack")]
        [Tooltip("Tốc độ lao về phía Player khi thực hiện Atk2")]
        [SerializeField] private float atk2DashSpeed = 6f;
        [Tooltip("Thời gian lao (giây)")]
        [SerializeField] private float atk2DashDuration = 0.35f;

        [Header("Animator State Names")]
        [SerializeField] private string atk1StateName = "Atk1";
        [SerializeField] private string atk2StateName = "Atk2";
        [SerializeField] private string atk2SignStateName = "AtkSign2";

        [Header("Atk2 – Sign Phase")]
        [Tooltip("Thời gian dừng lại ra hiêu trước khi lao (giây). 0 = lao thẳng không sign")]
        [SerializeField] private float atk2SignDuration = 0.4f;

        // ----------------------------------------------------------------
        // Internal
        // ----------------------------------------------------------------
        private Rigidbody2D cachedRb;
        private Collider2D  cachedSelfCol;   // cache để tránh GetComponent mỗi frame

        // Dash state — được kiểm soát từ FixedUpdate để dùng MovePosition()
        private bool  atk2DashActive;
        private float atk2DashDir;

        // Properties cho state dùng
        public float Atk1MaxRange  => Mathf.Max(0.1f, atk1MaxRange);
        public float Atk2DashSpeed => Mathf.Max(0f,   atk2DashSpeed);
        public float Atk2DashDuration => Mathf.Max(0.05f, atk2DashDuration);
        public string Atk1StateName   => atk1StateName;
        public string Atk2StateName   => atk2StateName;
        public string Atk2SignStateName => atk2SignStateName;
        public float  Atk2SignDuration  => Mathf.Max(0f, atk2SignDuration);

        /// <summary>True nếu Player đứng gần đủ để dùng Atk1.</summary>
        /// Được so sánh bằng EDGE distance (giống đơn vị với attackRange của base class).
        public bool IsInAtk1Range => IsTargetInAttackRange
                                  && Context_EdgeDistanceX <= Atk1MaxRange;

        // Cache center distance (dùng để debug / logic khác)
        internal float Context_AbsX => enemyContextAbsX;
        private float enemyContextAbsX;

        // Cache EDGE distance — cùng đơn vị với attackRange của base class
        internal float Context_EdgeDistanceX => enemyContextEdgeX;
        private float enemyContextEdgeX;

        // ----------------------------------------------------------------
        // State factory overrides
        // ----------------------------------------------------------------
        protected override EnemyState CreateIdleState(EnemyContext context)
            => new Em0180IdleState(context);

        protected override EnemyState CreateRunState(EnemyContext context)
            => new Em0180RunState(context);

        // Thêm Atk1 và Atk2 vào state machine
        protected override void RegisterAdditionalStates(EnemyStateMachine stateMachine, EnemyContext context)
        {
            stateMachine?.Register(new Em0180Atk1State(context));
            stateMachine?.Register(new Em0180Atk2State(context));
        }

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------
        protected override void Awake()
        {
            base.Awake();
            cachedRb      = GetComponent<Rigidbody2D>();
            cachedSelfCol = GetComponent<Collider2D>();
        }

        /// <summary>
        /// Override FixedUpdate: xử lý Atk2 dash bằng MovePosition() thay vì linearVelocity.
        /// MovePosition() trên Kinematic body thực hiện sweep-test collision →
        /// tự dừng tại bề mặt collider, không xuyên tường/ground.
        /// </summary>
        protected override void FixedUpdate()
        {
            if (atk2DashActive && cachedRb != null)
            {
                // Tính delta di chuyển ngang theo FixedDeltaTime
                float dx    = atk2DashDir * Atk2DashSpeed * Time.fixedDeltaTime;
                Vector2 next = cachedRb.position + new Vector2(dx, 0f);

                // MovePosition: Unity tự sweep → dừng đúng bề mặt, không xuyên
                cachedRb.MovePosition(next);
            }

            // Sau đó mới gọi gravity của base (không ảnh hưởng horizontal dash)
            base.FixedUpdate();
        }

        protected override void Update()
        {
            base.Update();

            if (HasTarget && CurrentTarget != null)
            {
                float absX = Mathf.Abs(CurrentTarget.position.x - transform.position.x);
                enemyContextAbsX = absX;

                // Edge distance: trừ bán kính 2 collider, cùng đơn vị với attackRange
                Collider2D targetCol = CurrentTarget.GetComponent<Collider2D>();
                float selfHalf   = cachedSelfCol != null ? cachedSelfCol.bounds.extents.x : 0f;
                float targetHalf = targetCol     != null ? targetCol.bounds.extents.x     : 0f;
                enemyContextEdgeX = Mathf.Max(0f, absX - (selfHalf + targetHalf));
            }
            else
            {
                enemyContextAbsX  = float.MaxValue;
                enemyContextEdgeX = float.MaxValue;
            }
        }

        // ----------------------------------------------------------------
        // Public helpers cho state
        // ----------------------------------------------------------------

        /// <summary>Quyết định dùng Atk1 hay Atk2 dựa theo khoảng cách.</summary>
        public void DecideAndEnterAttack()
        {
            if (!IsTargetInAttackRange) return; // Không trong cả 2 tầm thì bỏ qua

            if (IsInAtk1Range)
                ChangeEnemyState((byte)As.Atk1);
            else
                ChangeEnemyState((byte)As.Atk2);
        }

        /// <summary>
        /// Kích hoạt dash — thường được gọi khi bắt đầu Phase.Dash.
        /// Di chuyển thực tế xảy ra trong FixedUpdate bằng MovePosition().
        /// </summary>
        public void StartAtk2Dash(float deltaX)
        {
            atk2DashActive = true;
            atk2DashDir    = Mathf.Abs(deltaX) > 0.01f ? Mathf.Sign(deltaX) : 1f;
        }

        /// <summary>Dừng dash, xóa flag và reset horizontal velocity.</summary>
        public void StopDash()
        {
            atk2DashActive = false;
            if (cachedRb != null)
                cachedRb.linearVelocity = new Vector2(0f, cachedRb.linearVelocity.y);
        }

        /// <summary>
        /// Kiểm tra nhanh xem dash có bị chặn chưa (dùng trong state để chuyển người, không để ngăn vật lý).
        /// Việc ngăn vật lý được xử lý bởi MovePosition() trong FixedUpdate.
        /// </summary>
        public bool IsWallBlockingDash(float deltaX)
        {
            if (cachedRb == null) return false;
            float dir = Mathf.Abs(deltaX) > 0.01f ? Mathf.Sign(deltaX) : 1f;

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask    = Physics2D.GetLayerCollisionMask(gameObject.layer);
            filter.useTriggers  = false;

            RaycastHit2D[] hits = new RaycastHit2D[4];
            int count = cachedRb.Cast(new Vector2(dir, 0f), filter, hits, 0.1f);

            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
                if (hits[i].collider.transform.root == transform.root) continue;
                if (hits[i].distance <= 0.005f) continue;
                return true;
            }
            return false;
        }

        // Expose state IDs
        public byte Atk1StateId => (byte)As.Atk1;
        public byte Atk2StateId => (byte)As.Atk2;
    }
}
