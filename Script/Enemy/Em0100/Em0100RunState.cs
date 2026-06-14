namespace Mv
{
    /// <summary>
    /// Run state của Em0100.
    /// Quyết định dùng PunchCombo hay PunchSide khi vào tầm attack trong lúc đang đuổi.
    /// </summary>
    public class Em0100RunState : MvEnemyBase.AsEm_Run
    {
        public Em0100RunState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0100 owner = Context.Owner as MvEm0100;
            if (owner == null)
            {
                base.Tick();
                return;
            }

            // Khi đang trong tầm và có thể attack → chọn đòn phù hợp
            if (owner.HasTarget && owner.IsTargetInAttackRange && owner.CanStartAttackNow)
            {
                if (owner.ShouldUsePunchCombo)
                    owner.ChangeEnemyState(owner.PunchComboStateId);
                else
                    owner.ChangeEnemyState(owner.PunchSideStateId);
                return;
            }

            base.Tick();
        }
    }
}
