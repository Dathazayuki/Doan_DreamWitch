using UnityEngine;

namespace Mv
{
    public class Em0070AtkState : MvEnemyBase.AsEm_AtkWithSign_Base
    {
        public override byte StateId => (byte)MvEnemyBase.AsCommon.Max;
        public override string StateName => "Em0070Atk";

        private string selectedAttackAnim = "Atk";
        private bool startedAttack;

        public string SelectedAttackAnim => selectedAttackAnim;

        public Em0070AtkState(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            startedAttack = false;
            MvEm0070 owner = Context.Owner as MvEm0070;
            if (owner != null)
            {
                selectedAttackAnim = owner.ChooseRandomAttack();
            }
            base.Enter();
        }

        public override void Tick()
        {
            if (Em == null) return;

            if (!startedAttack)
            {
                Em.PlayAttackSignMotion(Context.DeltaX);

                if (!Em.IsAttackSignElapsed)
                    return;

                Em.CancelAttackSign();
                startedAttack = Em.TryStartAttackAndTrigger();
                if (!startedAttack)
                {
                    Em.ChangeEnemyState(Em.AtkAfterStateId);
                    return;
                }

                MvEm0070 owner = Em as MvEm0070;
                if (owner != null && owner.CachedAnimator != null)
                {
                    owner.CachedAnimator.Play(selectedAttackAnim, 0, 0f);
                }

                return;
            }

            Em.PlayAttackMotion(Context.DeltaX);
            if (!Em.IsAttackAnimFinished())
                return;

            Em.ChangeEnemyState(Em.AtkAfterStateId);
        }
    }
}
