namespace Mv
{
    /// <summary>
    /// Idle: kiểm tra tầm tấn công → chọn Atk1 hoặc Atk2, còn lại delegate lên base.
    /// </summary>
    public class Em0180IdleState : MvEnemyBase.AsEm_Idle
    {
        public Em0180IdleState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0180 owner = Context.Owner as MvEm0180;
            if (owner != null && owner.HasTarget && owner.IsTargetInAttackRange && owner.CanStartAttackNow)
            {
                owner.DecideAndEnterAttack();
                return;
            }

            base.Tick(); // patrol / chase / return-to-origin
        }
    }
}
