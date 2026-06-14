namespace Mv
{
    public class Em0050RunState : MvEnemyBase.MvActState_Em
    {
        public override byte StateId    => (byte)MvEnemyBase.AsCommon.Run;
        public override string StateName => "Em0050RunState";

        public Em0050RunState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            (Context.Owner as MvEm0050)?.TickWaypointPatrol();
        }
    }
}
