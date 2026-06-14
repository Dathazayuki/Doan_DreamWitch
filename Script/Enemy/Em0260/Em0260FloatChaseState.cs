namespace Mv
{
    public class Em0260FloatChaseState : MvEnemyBase.AsEm_Run
    {
        public Em0260FloatChaseState(EnemyContext context) : base(context) { }

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

            if (!owner.ShouldUseRunState())
            {
                owner.ChangeEnemyState(owner.IdleStateId);
                return;
            }

            owner.TickFloatChase(Context.DeltaX);
        }
    }
}
