using UnityEngine;

namespace Mv
{
    /// <summary>
    /// Atk1 – Cận chiến: kế thừa AsEm_AtkWithSign_Base giống Em0060.
    /// Chỉ thực hiện khi Player đứng gần (absX &lt;= atk1MaxRange).
    /// Có forward step nhỏ về phía Player trong lúc đánh.
    /// Nếu Player bỏ chạy ra ngoài range trong sign phase → cancel, chuyển sang Atk2.
    /// </summary>
    public class Em0180Atk1State : MvEnemyBase.AsEm_AtkWithSign_Base
    {
        public override byte StateId    => (byte)MvEm0180.As.Atk1;
        public override string StateName => "Em0180Atk1State";

        private const float FORWARD_STEP_DURATION = 0.1f;
        private const float FORWARD_STEP_SPEED    = 1.5f;

        private float forwardStepTimer;
        private Rigidbody2D cachedRb;

        public Em0180Atk1State(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            base.Enter();
            forwardStepTimer = 0f;
            cachedRb = Context.Owner != null
                ? Context.Owner.GetComponent<Rigidbody2D>()
                : null;
        }

        public override void Tick()
        {
            MvEm0180 owner = Context.Owner as MvEm0180;
            if (owner == null) return;

            // Xử lý sign → trigger attack → AtkAfter (kế thừa từ AsEm_AtkWithSign_Base)
            base.Tick(); 

            // Forward step nhỏ về phía Player trong giai đoạn đầu
            if (cachedRb != null && forwardStepTimer < FORWARD_STEP_DURATION)
            {
                float dir = Context.Owner.transform.localScale.x >= 0f ? 1f : -1f;

                cachedRb.linearVelocity = new Vector2(
                     dir * FORWARD_STEP_SPEED,
                     cachedRb.linearVelocity.y);
                forwardStepTimer += Time.deltaTime;
            }
        }
    }
}
