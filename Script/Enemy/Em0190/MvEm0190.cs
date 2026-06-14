using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0190 : MvEnemyBase
    {
        [Header("Ranged Attack")]
        [SerializeField] private float rangedAttackDistance = 10f;
        [SerializeField] private float rangedAttackVerticalTolerance = 3.5f;
        [SerializeField] private bool useCenterDistance = true;

        protected override string IdleStateName => "Idle";
        protected override string RunStateName => "Run";
        protected override string AttackSignStateName => "AtkSign";
        protected override string AttackStateName => "Atk";
        protected override string HitStateName => "Hit";
        protected override string DeadStateName => "Death";
        protected override bool UseAttackTriggerRangeCheck => false;

        protected override bool TryEvaluateCustomAttackRange(float absX, float absY, float edgeDistanceX, out bool inAttackRange)
        {
            float horizontalDistance = useCenterDistance ? absX : edgeDistanceX;
            float maxDistance = Mathf.Max(0.05f, rangedAttackDistance);
            float verticalTolerance = Mathf.Max(0.05f, rangedAttackVerticalTolerance);
            inAttackRange = horizontalDistance <= maxDistance && absY <= verticalTolerance;
            return true;
        }
    }
}
