using System.Collections;
using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PoisonStatusEffect : MonoBehaviour
    {
        private IDamageable target;
        private GameObject damageSource;
        private Coroutine routine;
        private int remainingTicks;
        private float damagePerTick;
        private float tickInterval;

        public void Apply(IDamageable damageable, GameObject source, int tickCount, float tickDamage, float interval)
        {
            if (damageable == null)
                return;

            target = damageable;
            damageSource = source;
            remainingTicks = Mathf.Max(0, tickCount);
            damagePerTick = Mathf.Max(0f, tickDamage);
            tickInterval = Mathf.Max(0.01f, interval);

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(TickRoutine());
        }

        private IEnumerator TickRoutine()
        {
            while (remainingTicks > 0 && target != null && target.IsAlive)
            {
                yield return new WaitForSeconds(tickInterval);

                if (target == null || !target.IsAlive)
                    break;

                target.TakeDamage(damagePerTick, damageSource);
                remainingTicks--;
            }

            routine = null;
            Destroy(this);
        }

        private void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }
    }
}
