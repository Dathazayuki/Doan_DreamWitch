namespace Mv
{
    /// <summary>
    /// AtkAfter state của Em0100.
    /// Sau khi kết thúc đòn tấn công, quyết định tiếp tục PunchCombo / PunchSide hay
    /// quay về Idle/Run như thông thường.
    /// </summary>
    public class Em0100AtkAfterState : MvEnemyBase.AsEm_Common_AtkAfter
    {
        public Em0100AtkAfterState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0100 owner = Context.Owner as MvEm0100;
            if (owner == null)
            {
                base.Tick();
                return;
            }

            if (owner.IsAttackAnimLocked)
                return;

            if (owner.HasTarget && owner.IsTargetInAttackRange && owner.CanStartAttackNow)
            {
                if (owner.ShouldUsePunchCombo)
                    owner.ChangeEnemyState(owner.PunchComboStateId);
                else
                    owner.ChangeEnemyState(owner.PunchSideStateId);
                return;
            }

            if (owner.ShouldUseRunState())
                owner.ChangeEnemyState(owner.RunStateId);
            else
                owner.ChangeEnemyState(owner.IdleStateId);
        }
    }
}
