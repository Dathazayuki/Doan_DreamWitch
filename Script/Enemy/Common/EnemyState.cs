namespace Mv
{
    public abstract class EnemyState
    {
        protected EnemyContext Context { get; }

        public abstract byte StateId { get; }
        public abstract string StateName { get; }
        public virtual bool IsAttackState => false;

        protected EnemyState(EnemyContext context)
        {
            Context = context;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public abstract void Tick();
    }
}
