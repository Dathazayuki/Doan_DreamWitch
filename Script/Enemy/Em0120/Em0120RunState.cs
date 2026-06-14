namespace Mv
{
    public class Em0120RunState : MvEnemyBase.MvActState_Em
    {
        public override byte StateId => (byte)MvEnemyBase.AsCommon.Run;
        public override string StateName => "Em0120RunState";

        public Em0120RunState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0120 owner = Context.Owner as MvEm0120;
            if (owner == null)
                return;

            owner.TickFlyMotion();
        }
    }
}
