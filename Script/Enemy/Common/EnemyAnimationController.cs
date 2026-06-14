using UnityEngine;

namespace Mv
{
    public class EnemyAnimationController
    {
        private readonly Animator animator;

        private readonly string runStateName;
        private readonly string idleStateName;
        private readonly string attackSignStateName;
        private readonly string attackStateName;
        private readonly string hitStateName;
        private readonly string deadStateName;

        private int currentFallbackStateHash;

        public EnemyAnimationController(
            Animator animator,
            string idleStateName,
            string runStateName,
            string attackSignStateName,
            string attackStateName,
            string hitStateName,
            string deadStateName)
        {
            this.animator = animator;
            this.idleStateName = idleStateName;
            this.runStateName = runStateName;
            this.attackSignStateName = attackSignStateName;
            this.attackStateName = attackStateName;
            this.hitStateName = hitStateName;
            this.deadStateName = deadStateName;

        }

        public void SetRun(bool value, bool allowIdleFallback)
        {
            if (animator == null) return;

            if (value)
                PlayStateFallback(runStateName, false);
            else if (allowIdleFallback)
                PlayStateFallback(idleStateName, false);
        }

        public void SetDead(bool value)
        {
            if (animator == null) return;
            if (value)
                PlayStateFallback(deadStateName, true);
        }

        public void TriggerAttackSign()
        {
            if (animator == null) return;
            PlayStateFallback(attackSignStateName, true);
        }

        public void TriggerAttack()
        {
            if (animator == null) return;
            PlayStateFallback(attackStateName, true);
        }

        public void TriggerHit()
        {
            if (animator == null) return;
            PlayStateFallback(hitStateName, true);
        }

        private void PlayStateFallback(string stateName, bool force)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return;

            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash)) return;
            if (!force && currentFallbackStateHash == hash) return;

            animator.Play(hash, 0, 0f);
            currentFallbackStateHash = hash;
        }

    }
}
