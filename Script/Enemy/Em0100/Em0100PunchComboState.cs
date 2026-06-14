using UnityEngine;

namespace Mv
{
    /// <summary>
    /// Đòn tấn công PunchCombo: chuỗi 3 đòn liên tiếp Punch1 → Punch2 → Punch3.
    /// 
    /// Flow:
    ///   [Sign Phase]  → Play PunchSign, đứng im, chờ attackSignDuration (cài trong Inspector của MvEnemyBase)
    ///   [Punch1]      → Play Punch1, chờ animation xong
    ///   [Punch2]      → Play Punch2, chờ animation xong
    ///   [Punch3]      → Play Punch3, chờ animation xong → chuyển sang AtkAfter
    /// </summary>
    public class Em0100PunchComboState : MvEnemyBase.AsEm_Atk_Base
    {
        public override byte StateId    => (byte)MvEm0100.As.PunchCombo;
        public override string StateName => "Em0100PunchCombo";

        private enum Phase { Sign, Punch1, Punch2, Punch3 }

        private Phase phase;
        private Animator cachedAnimator;
        private Rigidbody2D cachedRb;

        // Bước tiến nhỏ về phía Player để đòn đánh trông tự nhiên hơn
        private const float FORWARD_STEP_SPEED    = 1.5f;
        private const float FORWARD_STEP_DURATION = 0.1f;
        private float forwardStepTimer;

        public Em0100PunchComboState(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            MvEm0100 owner = Context.Owner as MvEm0100;
            if (owner == null) return;

            phase          = Phase.Sign;
            forwardStepTimer = 0f;

            cachedAnimator = owner.GetComponentInChildren<Animator>();
            cachedRb       = owner.GetComponent<Rigidbody2D>();

            // Kich hoat dung hitbox trigger cho chuoi 3 don
            owner.UsePunchComboAttack();

            // Bắt đầu sign phase
            owner.BeginAttackSignIfNeeded();
            PlayAnimation(owner.PunchSignStateName);
            owner.StopHorizontalMotion();
            owner.FaceByDeltaX(Context.DeltaX);
        }

        public override void Tick()
        {
            MvEm0100 owner = Context.Owner as MvEm0100;
            if (owner == null) return;

            switch (phase)
            {
                case Phase.Sign:   TickSign(owner);   break;
                case Phase.Punch1: TickPunch(owner, Phase.Punch1, owner.Punch1StateName, Phase.Punch2); break;
                case Phase.Punch2: TickPunch(owner, Phase.Punch2, owner.Punch2StateName, Phase.Punch3); break;
                case Phase.Punch3: TickPunch3(owner); break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Phase helpers
        // ──────────────────────────────────────────────────────────────

        private void TickSign(MvEm0100 owner)
        {
            owner.PlayAttackSignMotion(Context.DeltaX);

            if (!owner.IsAttackSignElapsed)
                return;

            owner.CancelAttackSign();

            // Thử kích hoạt attack (ghi nhận cooldown, trigger hitbox)
            bool started = owner.TryStartAttackAndTrigger();
            if (!started)
            {
                owner.ChangeEnemyState(owner.AtkAfterStateId);
                return;
            }

            // Override animation sang Punch1 vì TryStartAttackAndTrigger play AttackStateName ("Punch1")
            PlayAnimation(owner.Punch1StateName);
            forwardStepTimer = 0f;
            phase = Phase.Punch1;
        }

        private void TickPunch(MvEm0100 owner, Phase currentPhase, string animName, Phase nextPhase)
        {
            owner.PlayAttackMotion(Context.DeltaX);

            // Bước tiến nhỏ ở đầu mỗi đòn
            if (cachedRb != null && forwardStepTimer < FORWARD_STEP_DURATION)
            {
                float dir = owner.transform.localScale.x >= 0f ? 1f : -1f;

                cachedRb.linearVelocity = new Vector2(dir * FORWARD_STEP_SPEED, cachedRb.linearVelocity.y);
                forwardStepTimer += Time.deltaTime;
            }

            // Đợi animation hiện tại xong
            if (!IsCurrentAnimFinished(owner, animName))
                return;

            // Chuyển sang đòn tiếp theo
            forwardStepTimer = 0f;
            phase = nextPhase;

            if (nextPhase == Phase.Punch2)
                PlayAnimation(owner.Punch2StateName);
            else if (nextPhase == Phase.Punch3)
                PlayAnimation(owner.Punch3StateName);
        }

        private void TickPunch3(MvEm0100 owner)
        {
            owner.PlayAttackMotion(Context.DeltaX);

            if (cachedRb != null && forwardStepTimer < FORWARD_STEP_DURATION)
            {
                float dir = owner.transform.localScale.x >= 0f ? 1f : -1f;

                cachedRb.linearVelocity = new Vector2(dir * FORWARD_STEP_SPEED, cachedRb.linearVelocity.y);
                forwardStepTimer += Time.deltaTime;
            }

            if (!IsCurrentAnimFinished(owner, owner.Punch3StateName))
                return;

            // Chuỗi 3 đòn kết thúc → chuyển sang AtkAfter
            owner.ChangeEnemyState(owner.AtkAfterStateId);
        }

        // ──────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra xem animation với tên <paramref name="stateName"/> đã phát xong chưa (>= 95%).
        /// Nếu animator đang ở state khác (đã transition) → cũng coi là xong.
        /// </summary>
        private bool IsCurrentAnimFinished(MvEm0100 owner, string stateName, float threshold = 0.95f)
        {
            if (cachedAnimator == null)
                return owner.IsAttackAnimFinished(threshold);

            AnimatorStateInfo info = cachedAnimator.GetCurrentAnimatorStateInfo(0);

            // Nếu animator đã tự chuyển sang state khác → animation xong
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
