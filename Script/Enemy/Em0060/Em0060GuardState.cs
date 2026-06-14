using UnityEngine;

namespace Mv
{
    public class Em0060GuardState : MvEnemyBase.AsEm_Idle_Base
    {
        public override byte StateId => (byte)MvEm0060.As.Guard;
        public override string StateName => "Em0060Guard";

        private float timer;
        private MvEm0060 guardOwner;

        public Em0060GuardState(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            base.Enter();
            timer = 0f;
            guardOwner = Context.Owner as MvEm0060;
            guardOwner?.SetGuarding(true);
            guardOwner?.PlayGuardAnimation();
        }

        public override void Tick()
        {
            if (guardOwner == null)
            {
                Context.Owner?.ChangeEnemyState(Context.Owner.IdleStateId);
                return;
            }

            if (guardOwner.IsGuardBlockRecoilActive)
                guardOwner.TickGuardBlockRecoil();
            else
                guardOwner.StopHorizontalMotion();

            guardOwner.FaceByDeltaX(Context.DeltaX);
            timer += Time.deltaTime;

            if (timer < guardOwner.GuardDuration)
                return;

            if (guardOwner.HasTarget && guardOwner.IsTargetInAttackRange && guardOwner.CanStartAttackNow)
                guardOwner.ChangeEnemyState(guardOwner.AttackStateId);
            else if (guardOwner.ShouldUseRunState())
                guardOwner.ChangeEnemyState(guardOwner.RunStateId);
            else
                guardOwner.ChangeEnemyState(guardOwner.IdleStateId);
        }

        public override void Exit()
        {
            guardOwner?.SetGuarding(false);
            base.Exit();
        }
    }
}
