namespace Mv
{
    /// <summary>
    /// Run: chase/patrol bình thường, nhưng khi vào tầm → chọn Atk1 hoặc Atk2.
    /// </summary>
    public class Em0180RunState : MvEnemyBase.AsEm_Run
    {
        public Em0180RunState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0180 owner = Context.Owner as MvEm0180;
            if (owner != null && owner.HasTarget && owner.IsTargetInAttackRange && owner.CanStartAttackNow)
            {
                owner.DecideAndEnterAttack();
                return;
            }

            base.Tick(); // di chuyển bình thường
        }
    }
}
