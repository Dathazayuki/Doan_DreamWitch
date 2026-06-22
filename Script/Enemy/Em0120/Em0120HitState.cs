namespace Mv
{
    public class Em0120HitState : MvEnemyBase.AsEm_Common_Hit
    {
        public override string StateName => "Em0120HitState";

        public Em0120HitState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0120 owner = Context.Owner as MvEm0120;
            if (owner == null)
            {
                base.Tick();
                return;
            }

            owner.TickHitStunMotion();
            owner.TickFlyMotion(false);

            if (owner.IsHitStunActive || owner.IsDamageRetreatActive)
                return;

            if (owner.IsAStarPausedByHit)
            {
                owner.ChangeEnemyState(owner.IdleStateId);
                return;
            }

            if (owner.HasTarget && owner.IsTargetInAttackRange && owner.CanStartAttackNow)
                owner.ChangeEnemyState(owner.AttackStateId);
            else if (owner.ShouldUseRunState())
                owner.ChangeEnemyState(owner.RunStateId);
            else
                owner.ChangeEnemyState(owner.IdleStateId);
        }
    }
}
