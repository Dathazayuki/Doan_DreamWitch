namespace Mv
{
    public class Em0470ShootState : MvEnemyBase.AsEm_Atk_Base
    {
        public override byte StateId => (byte)MvEnemyBase.AsCommon.Max;
        public override string StateName => "Em0470Shoot";

        private bool shot;

        public Em0470ShootState(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            shot = false;
            Context.Owner?.BeginAttackSignIfNeeded();
        }

        public override void Tick()
        {
            MvEm0470 owner = Context.Owner as MvEm0470;
            if (owner == null)
                return;

            if (!shot)
            {
                owner.PlayAttackSignMotion(Context.DeltaX);

                if (!owner.IsAttackSignElapsed)
                    return;

                owner.CancelAttackSign();
                owner.TriggerAttackAnimationOnly();
                owner.ShootFireBall();
                owner.BeginShootCooldown();
                shot = true;
                return;
            }

            owner.PlayAttackMotion(Context.DeltaX);
            if (!owner.IsAttackAnimFinished())
                return;

            owner.ChangeEnemyState(owner.IdleStateId);
        }
    }
}
