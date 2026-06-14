using UnityEngine;

namespace Mv
{
    /// <summary>
    /// Đòn tấn công PunchSide: đánh về 2 phía (area attack).
    ///
    /// Flow:
    ///   [Sign Phase]  → Play PunchSideSign, đứng im, chờ attackSignDuration
    ///   [Attack]      → Play PunchSide, chờ animation xong → chuyển sang AtkAfter
    ///
    /// Vì đây là đòn đánh 2 phía, Em0100 KHÔNG quay mặt theo Player trong lúc đánh
    /// (sign phase vẫn quay mặt để ra hiệu về phía player).
    /// </summary>
    public class Em0100PunchSideState : MvEnemyBase.AsEm_Atk_Base
    {
        public override byte StateId    => (byte)MvEm0100.As.PunchSide;
        public override string StateName => "Em0100PunchSide";

        private enum Phase { Sign, Attack }

        private Phase phase;
        private Animator cachedAnimator;

        public Em0100PunchSideState(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            MvEm0100 owner = Context.Owner as MvEm0100;
            if (owner == null) return;

            phase = Phase.Sign;

            cachedAnimator = owner.GetComponentInChildren<Animator>();

            // Kich hoat dung hitbox trigger 2 phia
            owner.UsePunchSideAttack();

            // Sign phase
            owner.BeginAttackSignIfNeeded();
            PlayAnimation(owner.PunchSideSignStateName);
            owner.StopHorizontalMotion();
            owner.FaceByDeltaX(Context.DeltaX); // quay mat trong sign phase
        }

        public override void Tick()
        {
            MvEm0100 owner = Context.Owner as MvEm0100;
            if (owner == null) return;

            switch (phase)
            {
                case Phase.Sign:   TickSign(owner);   break;
                case Phase.Attack: TickAttack(owner); break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Phase helpers
        // ──────────────────────────────────────────────────────────────

        private void TickSign(MvEm0100 owner)
        {
            // Đứng im trong sign phase, giữ nguyên hướng
            owner.StopHorizontalMotion();

            if (!owner.IsAttackSignElapsed)
                return;

            owner.CancelAttackSign();

            bool started = owner.TryStartAttackAndTrigger();
            if (!started)
            {
                owner.ChangeEnemyState(owner.AtkAfterStateId);
                return;
            }

            // Override animation sang PunchSide
            PlayAnimation(owner.PunchSideStateName);
            phase = Phase.Attack;
        }

        private void TickAttack(MvEm0100 owner)
        {
            // Đứng im, KHÔNG quay mặt (đánh 2 phía nên không cần track player)
            owner.StopHorizontalMotion();

            if (!IsCurrentAnimFinished(owner, owner.PunchSideStateName))
                return;

            owner.ChangeEnemyState(owner.AtkAfterStateId);
        }

        // ──────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────

        private bool IsCurrentAnimFinished(MvEm0100 owner, string stateName, float threshold = 0.95f)
        {
            if (cachedAnimator == null)
                return owner.IsAttackAnimFinished(threshold);

            AnimatorStateInfo info = cachedAnimator.GetCurrentAnimatorStateInfo(0);

            if (!info.IsName(stateName))
                return true;

            float normalized = info.loop ? (info.normalizedTime % 1f) : info.normalizedTime;
            return normalized >= Mathf.Clamp01(threshold);
        }

        private void PlayAnimation(string stateName)
        {
            if (cachedAnimator != null && !string.IsNullOrWhiteSpace(stateName))
                cachedAnimator.Play(stateName, 0, 0f);
        }
    }
}
