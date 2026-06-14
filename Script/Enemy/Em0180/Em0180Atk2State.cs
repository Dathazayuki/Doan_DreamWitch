using UnityEngine;

namespace Mv
{
    /// <summary>
    /// Atk2 – Dash Attack với Sign phase trước khi lao.
    ///
    /// Flow 3 giai đoạn:
    ///   [Phase 1: SIGN]  → Play AtkSign2, đứng im, chờ atk2SignDuration
    ///   [Phase 2: DASH]  → Lao nhanh về Player, trigger hitbox, chờ atk2DashDuration
    ///   [Phase 3: DONE]  → Dừng lại, chuyển sang AtkAfter
    /// </summary>
    public class Em0180Atk2State : MvEnemyBase.AsEm_Atk_Base
    {
        public override byte StateId    => (byte)MvEm0180.As.Atk2;
        public override string StateName => "Em0180Atk2State";

        private enum Phase { Sign, Dash, Done }

        private Phase phase;
        private float phaseTimer;
        private float deltaXOnEnter;    // Hướng Player lúc bắt đầu, fix cứng suốt Atk2
        private Animator cachedAnimator;

        public Em0180Atk2State(EnemyContext context) : base(context) { }

        // ----------------------------------------------------------------
        // Enter
        // ----------------------------------------------------------------
        public override void Enter()
        {
            MvEm0180 owner = Context.Owner as MvEm0180;
            if (owner == null) return;

            deltaXOnEnter   = Context.DeltaX;
            phaseTimer      = 0f;
            cachedAnimator  = owner.GetComponentInChildren<Animator>();

            // Mặt về phía Player ngay từ đầu
            owner.FaceByDeltaX(deltaXOnEnter);
            owner.StopHorizontalMotion();

            // Nếu sign duration = 0 → bỏ qua phase Sign, lao thẳng
            if (owner.Atk2SignDuration <= 0f)
            {
                EnterDashPhase(owner);
            }
            else
            {
                EnterSignPhase(owner);
            }
        }

        // ----------------------------------------------------------------
        // Tick
        // ----------------------------------------------------------------
        public override void Tick()
        {
            MvEm0180 owner = Context.Owner as MvEm0180;
            if (owner == null) return;

            phaseTimer += Time.deltaTime;

            switch (phase)
            {
                case Phase.Sign:
                    TickSign(owner);
                    break;

                case Phase.Dash:
                    TickDash(owner);
                    break;
            }
        }

        // ----------------------------------------------------------------
        // Exit
        // ----------------------------------------------------------------
        public override void Exit()
        {
            (Context.Owner as MvEm0180)?.StopDash();
        }

        // ================================================================
        // Phase helpers
        // ================================================================

        private void EnterSignPhase(MvEm0180 owner)
        {
            phase      = Phase.Sign;
            phaseTimer = 0f;

            // Dừng lại, play animation AtkSign2
            owner.StopHorizontalMotion();
            PlayAnimation(owner.Atk2SignStateName);
        }

        private void TickSign(MvEm0180 owner)
        {
            // Giữ đứng im trong sign phase
            owner.StopHorizontalMotion();

            // Hết sign duration → chuyển sang Dash
            if (phaseTimer >= owner.Atk2SignDuration)
                EnterDashPhase(owner);
        }

        private void EnterDashPhase(MvEm0180 owner)
        {
            phase      = Phase.Dash;
            phaseTimer = 0f;

            // Play animation Atk2
            PlayAnimation(owner.Atk2StateName);

            // Kích hoạt dash — di chuyển thực tế trong FixedUpdate bằng MovePosition()
            owner.StartAtk2Dash(deltaXOnEnter);

            // Bắt đầu logic trigger gây damage. 
            // Lưu ý: base.TryStartAttackAndTrigger() trong MvEnemyBase 
            // sẽ mặc định ép play animation "Atk1", nên ta cần Play lại "Atk2" ngay sau đó.
            owner.TryStartAttackAndTrigger();
            PlayAnimation(owner.Atk2StateName);
        }

        private void TickDash(MvEm0180 owner)
        {
            if (phaseTimer < owner.Atk2DashDuration)
            {
                // Di chuyển được xử lý trong FixedUpdate bằng MovePosition() — không xâm phạm tường.
                // Ở đây chỉ kiểm tra để chuyển state sớm khi cần.
                if (owner.IsWallBlockingDash(deltaXOnEnter))
                {
                    phase = Phase.Done;
                    owner.StopDash();
                    owner.ChangeEnemyState(owner.AtkAfterStateId);
                    return;
                }
            }
            else
            {
                // Hết thời gian dash → kết thúc
                phase = Phase.Done;
                owner.StopDash();
                owner.ChangeEnemyState(owner.AtkAfterStateId);
            }
        }

        private void PlayAnimation(string stateName)
        {
            if (cachedAnimator != null && !string.IsNullOrWhiteSpace(stateName))
                cachedAnimator.Play(stateName, 0, 0f);
        }
    }
}
