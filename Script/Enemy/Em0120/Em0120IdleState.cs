namespace Mv
{
    public class Em0120IdleState : MvEnemyBase.AsEm_Idle_Base
    {
        public override byte StateId => (byte)MvEnemyBase.AsCommon.Idle;
        public override string StateName => "Em0120IdleState";

        public Em0120IdleState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0120 owner = Context.Owner as MvEm0120;
            if (owner == null)
                return;

            if (owner.IsAStarPausedByHit)
            {
                owner.PlayIdleMotion();
                owner.TickFlyMotion(false);
                return;
            }

            owner.ChangeEnemyState(owner.RunStateId);
        }
    }
}
