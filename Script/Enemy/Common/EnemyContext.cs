namespace Mv
{
    public class EnemyContext
    {
        public MvEnemyBase Owner { get; }

        public float DeltaX { get; set; }
        public float AbsX { get; set; }
        public float AbsY { get; set; }
        public float EdgeDistanceX { get; set; }
        public bool InAttackRange { get; set; }

        public EnemyContext(MvEnemyBase owner)
        {
            Owner = owner;
        }
    }
}
