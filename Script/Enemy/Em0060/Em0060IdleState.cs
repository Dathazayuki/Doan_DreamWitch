namespace Mv
{
    public class Em0060IdleState : MvEnemyBase.AsEm_Idle
    {
        public Em0060IdleState(EnemyContext context) : base(context) { }

        public override void Tick()
        {
            MvEm0060 owner = Context.Owner as MvEm0060;
            if (owner != null && owner.CanEnterGuard())
            {
                owner.ChangeEnemyState((byte)MvEm0060.As.Guard);
                return;
            }

            base.Tick();
        }
    }
}
