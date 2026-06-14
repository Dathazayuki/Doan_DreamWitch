namespace Mv
{
    public class Em0130RunState : MvEnemyBase.AsEm_Run
    {
        public Em0130RunState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0130 owner = Context.Owner as MvEm0130;
            if (owner != null && owner.HasTarget && owner.IsTargetInAttackRange && owner.CanStartAttackNow)
            {
                owner.DecideAndEnterAttack();
                return;
            }

            base.Tick(); // di chuyển bình thường
        }
    }
}
