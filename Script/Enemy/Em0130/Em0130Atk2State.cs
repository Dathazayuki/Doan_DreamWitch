using UnityEngine;

namespace Mv
{
    /// <summary>
    /// Atk2 – Giai đoạn 2 của chuỗi liên hoàn (Dash Attack sau khi Atk1 kết thúc).
    /// </summary>
    public class Em0130Atk2State : MvEnemyBase.AsEm_Atk_Base
    {
        public override byte StateId    => (byte)MvEm0130.As.Atk2;
        public override string StateName => "Em0130Atk2State";

        private enum Phase { Dash, Done }

        private Phase phase;
        private float phaseTimer;
        private float deltaXOnEnter;    // Hướng Player lúc bắt đầu, fix cứng suốt Atk2
        private Animator cachedAnimator;

        public Em0130Atk2State(EnemyContext context) : base(context) { }

        // ----------------------------------------------------------------
        // Enter
        // ----------------------------------------------------------------
        public override void Enter()
        {
            MvEm0130 owner = Context.Owner as MvEm0130;
            if (owner == null) return;

            deltaXOnEnter   = Context.DeltaX;
            phaseTimer      = 0f;
            cachedAnimator  = owner.GetComponentInChildren<Animator>();

            // 面 về phía Player ngay từ đầu
            owner.FaceByDeltaX(deltaXOnEnter);
            owner.StopHorizontalMotion();

            // Atk1 -> qua thẳng Atk2 Dash (không cần Sign)
            EnterDashPhase(owner);
        }

        // ----------------------------------------------------------------
        // Tick
        // ----------------------------------------------------------------
        public override void Tick()
        {
            MvEm0130 owner = Context.Owner as MvEm0130;
            if (owner == null) return;

            phaseTimer += Time.deltaTime;

            switch (phase)
            {
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
            (Context.Owner as MvEm0130)?.StopDash();
        }

        // ================================================================
        // Phase helpers
        // ================================================================

        private void EnterDashPhase(MvEm0130 owner)
        {
            phase      = Phase.Dash;
            phaseTimer = 0f;

            owner.StartAtk2Dash(deltaXOnEnter);

            owner.TryStartAttackAndTrigger();
            PlayAnimation(owner.Atk2StateName);
        }

        private void TickDash(MvEm0130 owner)
        {
            if (phaseTimer < owner.Atk2DashDuration)
            {
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
