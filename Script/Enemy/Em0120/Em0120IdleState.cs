namespace Mv
{
    public class Em0120IdleState : MvEnemyBase.AsEm_Idle_Base
    {
        public override byte StateId => (byte)MvEnemyBase.AsCommon.Idle;
        public override string StateName => "Em0120IdleState";

        public Em0120IdleState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            if (Context.Owner == null)
                return;

            Context.Owner.ChangeEnemyState(Context.Owner.RunStateId);
        }
    }
}
