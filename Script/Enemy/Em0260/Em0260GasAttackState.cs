namespace Mv
{
    public class Em0260GasAttackState : MvEnemyBase.AsEm_AtkWithSign_Base
    {
        public override byte StateId => (byte)MvEnemyBase.AsCommon.Max;
        public override string StateName => "Em0260GasAttack";

        private bool startedAttack;

        public Em0260GasAttackState(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            startedAttack = false;
            Context.Owner?.FaceByDeltaX(Context.DeltaX);
            Context.Owner?.BeginAttackSignIfNeeded();
        }

        public override void Tick()
        {
            MvEm0260 owner = Context.Owner as MvEm0260;
            if (owner == null)
                return;

            if (!startedAttack)
            {
                owner.TickGasAttackMotion(Context.DeltaX);

                if (!owner.IsAttackSignElapsed)
                    return;

                owner.CancelAttackSign();
                startedAttack = owner.TryStartAttackAndTrigger();
                if (!startedAttack)
                    owner.ChangeEnemyState(owner.AtkAfterStateId);

                return;
            }

            owner.TickGasAttackMotion(Context.DeltaX);
            if (!owner.IsAttackAnimFinished())
                return;

            owner.ChangeEnemyState(owner.AtkAfterStateId);
        }
    }
}
