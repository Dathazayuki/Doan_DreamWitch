namespace Mv
{
    public class Em0110IdleState : MvEnemyBase.AsEm_Idle_Base
    {
        public override byte StateId => (byte)MvEnemyBase.AsCommon.Idle;
        public override string StateName => "Em0110IdleState";

        public Em0110IdleState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            if (Context.Owner == null)
                return;

            Context.Owner.ChangeEnemyState(Context.Owner.RunStateId);
        }
    }
}
