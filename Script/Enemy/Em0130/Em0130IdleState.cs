namespace Mv
{
    public class Em0130IdleState : MvEnemyBase.AsEm_Idle
    {
        public Em0130IdleState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0130 owner = Context.Owner as MvEm0130;
            if (owner != null && owner.HasTarget && owner.IsTargetInAttackRange && owner.CanStartAttackNow)
            {
                owner.DecideAndEnterAttack();
                return;
            }

            base.Tick(); // patrol / chase / return-to-origin
        }
    }
}
