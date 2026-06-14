namespace Mv
{
    public class Em0110RunState : MvEnemyBase.MvActState_Em
    {
        public override byte StateId => (byte)MvEnemyBase.AsCommon.Run;
        public override string StateName => "Em0110RunState";

        public Em0110RunState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0110 owner = Context.Owner as MvEm0110;
            if (owner == null)
                return;

            owner.TickWallPatrolMotion();
        }
    }
}
