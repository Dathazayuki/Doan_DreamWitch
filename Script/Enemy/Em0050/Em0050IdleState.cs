namespace Mv
{
    /// <summary>
    /// Idle: ngay lập tức chuyển sang Run để bắt đầu đi vòng platform.
    /// </summary>
    public class Em0050IdleState : MvEnemyBase.AsEm_Idle
    {
        public Em0050IdleState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0050 owner = Context.Owner as MvEm0050;
            if (owner == null) { base.Tick(); return; }

            // Em0050 luôn patrol – chuyển thẳng sang Run
            owner.ChangeEnemyState(owner.RunStateId);
        }
    }
}
