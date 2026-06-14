namespace Mv
{
    /// <summary>
    /// Idle state của Em0100.
    /// Quyết định dùng PunchCombo hay PunchSide khi vào tầm attack.
    /// </summary>
    public class Em0100IdleState : MvEnemyBase.AsEm_Idle
    {
        public Em0100IdleState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0100 owner = Context.Owner as MvEm0100;
            if (owner == null)
            {
                base.Tick();
                return;
            }

            // Kiểm tra xem có thể tấn công không
            if (owner.HasTarget && owner.IsTargetInAttackRange && owner.CanStartAttackNow)
            {
                // Chọn loại đòn tấn công dựa theo khoảng cách
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
