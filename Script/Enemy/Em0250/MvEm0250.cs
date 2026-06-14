using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0250 : MvEnemyBase, IMvAnimEventLiteListener
    {
        [Header("Em0250 Gas VFX")]
        [SerializeField] private GameObject gasVfxObject;
        [SerializeField] private bool disableGasVfxOnAwake = true;

        [Header("Em0250 Attack Range Fallback")]
        [SerializeField] private float fallbackGasRadiusX = 1.6f;
        [SerializeField] private float fallbackGasRadiusY = 1.2f;

        protected override string IdleStateName => "Idle";
        protected override string RunStateName => "Run";
        protected override string AttackSignStateName => "AtkSign";
        protected override string AttackStateName => "Atk";
        protected override string HitStateName => "Hit";
        protected override string DeadStateName => "Death";

        protected override EnemyState CreateAttackState(EnemyContext context) => new Em0250GasAttackState(context);

        protected override void Awake()
        {
            base.Awake();

            if (ActiveAttack != null)
            {
                ActiveAttack.SetCloseAttackWindowImmediatelyOnEnd(true);
                ActiveAttack.SetAllowAutoHitFallback(false);
            }

            if (disableGasVfxOnAwake)
                SetGasVfxActive(false);
        }

        private void OnDisable()
        {
            SetGasVfxActive(false);
        }

        protected override bool TryEvaluateCustomAttackRange(float absX, float absY, float edgeDistanceX, out bool inAttackRange)
        {
            if (ActiveAttack != null && ActiveAttack.HasAttackTrigger)
            {
                inAttackRange = ActiveAttack.IsPlayerInsideAttackTrigger();
                return true;
            }

            float radiusX = Mathf.Max(0.05f, fallbackGasRadiusX);
            float radiusY = Mathf.Max(0.05f, fallbackGasRadiusY);
            float normalizedX = absX / radiusX;
            float normalizedY = absY / radiusY;
            inAttackRange = normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
            return true;
        }

        public void TickGasAttackMotion(float deltaX)
        {
            FaceByDeltaX(deltaX);
            SetRunAnimation(false, false);
            StopHorizontalMotion();
        }

        public void OnMvAnimEvent(string eventName, MvAnimEventLite source)
        {
            if (eventName == "AtkS" || eventName == "Attack/AtkS")
            {
                SetGasVfxActive(true);
                return;
            }

            if (eventName == "AtkE" || eventName == "Attack/AtkE" || eventName == "CancelE")
            {
                SetGasVfxActive(false);
            }
        }

        private void SetGasVfxActive(bool active)
        {
            if (gasVfxObject != null && gasVfxObject.activeSelf != active)
                gasVfxObject.SetActive(active);
        }

        protected override void Die()
        {
            SetGasVfxActive(false);
            base.Die();
        }
    }
}
