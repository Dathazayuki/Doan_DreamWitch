using UnityEngine;

namespace Mv
{
    [CreateAssetMenu(fileName = "MvSo_Em9030", menuName = "DreamKnight/Enemy/Em9030 Config")]
    public class MvSo_Em9030 : ScriptableObject
    {
        [Header("AI Desire Weights")]
        public int Desire_Add_Run = 10;
        public int Desire_Add_Jump = 10;
        public int Desire_Add_Atk = 40;
        public int Desire_Add_SlashVH = 20;
        public int Desire_Add_Provoke = 5;
        public int Desire_Add_ShotA = 25;
        public int Desire_Add_Scythe_Hp50 = 10;
        public int Desire_Add_EmSpawn_Hp30 = 10;

        [Header("Timings")]
        public float Entrance_LookTime = 2f;
        public float IdleWaitTime = 0.6f;
        public float IdleWaitTimeEx = 1.1f;
        public float Provoke_LoopTime = 1.2f;
        public float MagicShotAir_SpritShot_LoopTime = 1.4f;
        public float MagicShotSide0_Laser_LoopTime = 1.4f;
        public float MagicShotSide0_Eruption_LoopTime = 1.4f;
        public float Scythe_LoopTime = 2f;
        public float CounterAtkTimer = 1.2f;
        public int[] KnockOut_LoopNum = new int[] { 3, 2, 1 };

        [Header("Shot Patterns")]
        public int EruptionSideShotCount = 3;
        public float EruptionSideShotSpacingY = 0.35f;

        [Header("Movement")]
        public Vector2 JumpPow = new Vector2(0f, 12f);
        public Vector2 JumpPowMini = new Vector2(0f, 8f);
        public Vector2 JumpPowMiniLong = new Vector2(0f, 10f);
        public Vector2 JumpPowSide = new Vector2(8f, 9f);
        public float MeleeApproachStopDistance = 1.8f;
        public float MeleeApproachVerticalTolerance = 2f;
        public float MeleeApproachTimeout = 4f;
        public float CounterMoveSpeed = 8f;
        public AnimationCurve CounterMoveSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        public LayerMask CounterMovementBlockMask = 0;
        public float CounterMovementProbeDistance = 0.12f;
        public float CounterRepeatedHitInterval = 0.25f;

        [Header("Airborne")]
        public float AirborneGravityScale = 4f;
        public float LandingHeightTolerance = 0.12f;
        public bool SnapToLandingHeightOnLand = true;

        [Header("Damage")]
        public float AtkDmg_Atk1 = 15f;
        public float AtkDmg_Atk2 = 18f;
        public float AtkDmg_Atk3 = 22f;
        public float AtkDmg_SlashV = 25f;
        public float AtkDmg_SlashH = 25f;
        public float AtkDmg_Counter = 30f;
        public float AtkDmg_SpritShot = 10f;
        public float AtkDmg_FlySwordBig = 30f;
        public float AtkDmg_Laser = 25f;
        public float AtkDmg_Eruption = 25f;
        public float AtkDmg_Scythe = 25f;
    }
}
