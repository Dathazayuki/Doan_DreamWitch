namespace Mv
{
    public class Em0470IdleState : EnemyState
    {
        private readonly byte stateId;
        private readonly string stateName;

        public override byte StateId => stateId;
        public override string StateName => stateName;

        public Em0470IdleState(EnemyContext context, byte id, string name) : base(context)
        {
            stateId = id;
            stateName = name;
        }

        public override void Tick()
        {
            MvEm0470 owner = Context.Owner as MvEm0470;
            owner?.TickStationaryTurret();
        }
    }
}
