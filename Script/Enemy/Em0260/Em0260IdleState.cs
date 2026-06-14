namespace Mv
{
    public class Em0260IdleState : MvEnemyBase.AsEm_Idle
    {
        public Em0260IdleState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0260 owner = Context.Owner as MvEm0260;
            if (owner == null)
            {
                base.Tick();
                return;
            }

            if (owner.IsAttackAnimLocked)
            {
                owner.ChangeEnemyState(owner.AttackStateId);
                return;
            }

            if (owner.HasTarget && owner.IsTargetInAttackRange && owner.CanStartAttackNow)
            {
                owner.ChangeEnemyState(owner.AttackStateId);
                return;
            }

            owner.TickHoverIdle();
            if (owner.ShouldUseRunState())
                owner.ChangeEnemyState(owner.RunStateId);
        }
    }
}
