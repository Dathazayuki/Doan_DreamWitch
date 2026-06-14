using UnityEngine;

namespace Mv
{
    /// <summary>
    /// Atk1 – Giai đoạn 1 của chuỗi liên hoàn.
    /// Hoàn thành xong (IsAttackAnimLocked = false) sẽ tự động chuyển sang Atk2 thay vì kết thúc.
    /// </summary>
    public class Em0130Atk1State : MvEnemyBase.AsEm_Atk_Base
    {
        public override byte StateId    => (byte)MvEm0130.As.Atk1;
        public override string StateName => "Em0130Atk1State";

        private const float FORWARD_STEP_DURATION = 0.1f;
        private const float FORWARD_STEP_SPEED    = 1.5f;

        private float forwardStepTimer;
        private Rigidbody2D cachedRb;
        private bool startedAttack;

        public Em0130Atk1State(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            startedAttack = false;
            forwardStepTimer = 0f;
            Context.Owner?.FaceByDeltaX(Context.DeltaX);
            Context.Owner?.BeginAttackSignIfNeeded();

            cachedRb = Context.Owner != null
                ? Context.Owner.GetComponent<Rigidbody2D>()
                : null;
        }

        public override void Tick()
        {
            MvEm0130 owner = Context.Owner as MvEm0130;
            if (owner == null) return;

            // --- XỬ LÝ SIGN PHASE (Nếu có) ---
            if (!startedAttack)
            {
                owner.PlayAttackSignMotion(Context.DeltaX);

                if (!owner.IsAttackSignElapsed)
                    return;

                owner.CancelAttackSign();
                startedAttack = owner.TryStartAttackAndTrigger();
                
                // Tránh lỗi trigger không chơi được
                if (!startedAttack)
                {
                    owner.ChangeEnemyState(owner.Atk2StateId);
                    return;
                }
            }

            // --- XỬ LÝ ATTACK ACTION PHASE ---
            owner.PlayAttackMotion(Context.DeltaX);

            // Forward step nhỏ về phía Player trong giai đoạn đầu
            if (cachedRb != null && forwardStepTimer < FORWARD_STEP_DURATION)
            {
                float dir = owner.transform.localScale.x >= 0f ? 1f : -1f;

                cachedRb.linearVelocity = new Vector2(
                    dir * FORWARD_STEP_SPEED,
                    cachedRb.linearVelocity.y);
                forwardStepTimer += Time.deltaTime;
            }

            if (!owner.IsAttackAnimFinished())
                return;

            // *** KHÁC BIỆT CỐT LÕI: Thay vì qua AtkAfterState, chuyển tiếp sang Atk2 (Dash Attack) ***
            owner.ChangeEnemyState(owner.Atk2StateId);
        }
    }
}
