using UnityEngine;

namespace Mv
{
    public class Em0010AttackState : MvEnemyBase.AsEm_Atk_Base
    {
        public override byte StateId => (byte)MvEm0010.As.Attack;
        public override string StateName => "Em0010Attack";

        private Rigidbody2D cachedRb;
        private bool startedAttack;
        private bool isLeaping;
        private float leapTimer;

        public Em0010AttackState(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            startedAttack = false;
            isLeaping = false;
            Context.Owner?.FaceByDeltaX(Context.DeltaX);
            Context.Owner?.BeginAttackSignIfNeeded();
        }

        public override void Tick()
        {
            if (Context.Owner == null)
                return;

            if (!startedAttack)
            {
                Context.Owner.PlayAttackSignMotion(Context.DeltaX);

                if (!Context.Owner.IsAttackSignElapsed)
                    return;

                Context.Owner.CancelAttackSign();
                startedAttack = Context.Owner.TryStartAttackAndTrigger();
                if (!startedAttack)
                {
                    Context.Owner.ChangeEnemyState(Context.Owner.AtkAfterStateId);
                    return;
                }
            }

            float direction = Context.Owner.transform.localScale.x >= 0f ? 1f : -1f;

            if (!isLeaping)
            {
                isLeaping = true;
                MvEm0010 owner = Context.Owner as MvEm0010;
                float upSpeed = owner != null ? owner.AttackLeapUpSpeed : 0f;
                owner?.StartLeapDash(direction, upSpeed);
            }

            Context.Owner.PlayAttackMotion(Context.DeltaX);

            if (!Context.Owner.IsAttackAnimFinished())
                return;

            Context.Owner.ChangeEnemyState(Context.Owner.AtkAfterStateId);
        }

    }
}
