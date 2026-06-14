using UnityEngine;

namespace Mv
{
    public class Em0060AttackState : MvEnemyBase.AsEm_AtkWithSign_Base
    {
        public override byte StateId => (byte)MvEm0060.As.Attack;

        private const float FORWARD_STEP_DURATION = 0.12f;
        private const float FORWARD_STEP_SPEED = 1.8f;

        private float forwardStepTimer;
        private Rigidbody2D cachedRb;

        public Em0060AttackState(EnemyContext context) : base(context) { }

        public override void Enter()
        {
            base.Enter();
            forwardStepTimer = 0f;
            cachedRb = Context.Owner != null ? Context.Owner.GetComponent<Rigidbody2D>() : null;
        }

        public override void Tick()
        {
            base.Tick();

            if (Context.Owner == null || cachedRb == null)
                return;

            if (forwardStepTimer >= FORWARD_STEP_DURATION)
                return;

            float direction = Context.Owner.transform.localScale.x >= 0f ? 1f : -1f;

            cachedRb.linearVelocity = new Vector2(direction * FORWARD_STEP_SPEED, cachedRb.linearVelocity.y);
            forwardStepTimer += Time.deltaTime;
        }
    }
}
