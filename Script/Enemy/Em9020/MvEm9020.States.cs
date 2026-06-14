using System;
using System.Collections.Generic;
using UnityEngine;
using DreamKnight.Interfaces;
using DreamKnight.Player;
using DreamKnight.Systems.Combat;
using Spine;
using Spine.Unity;

namespace Mv
{
    public partial class MvEm9020
    {
        public abstract class MvActState_Em9020 : MvActState_Em
        {
            protected MvEm9020 Golem => Em as MvEm9020;
            protected MvActState_Em9020(EnemyContext context) : base(context) { }
        }

        public abstract class MvActState_Em9020Atk : AsEm_Atk_Base
        {
            protected MvEm9020 Golem => Em as MvEm9020;
            protected MvActState_Em9020Atk(EnemyContext context) : base(context) { }
        }

        public class AsEm9020_Entrance : MvActState_Em9020
        {
            public override byte StateId => (byte)As.Entrance;
            public override string StateName => "AsEm9020_Entrance";
            private float timer;

            public AsEm9020_Entrance(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                Golem.PlayAnimation("Entrance", false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                float duration = Golem.GetAnimationDuration("Entrance", Golem.Entrance_LookTime);
                if (Golem.IsAnimationFinished("Entrance") || timer >= duration)
                {
                    Golem.ReqNextComboAction();
                }
            }
        }

        public abstract class AsEm9020_Hand_AtkD : MvActState_Em9020Atk
        {
            private enum HandPhase
            {
                MoveToTarget,
                Attack,
                ReturnToBody
            }

            protected abstract MvAttack CurrentAttack { get; }
            protected abstract Transform ControlHand { get; }
            protected abstract Transform AimHand { get; }
            private float timer;
            private string animName;
            private HandPhase phase;
            private Vector3 startLocalPosition;
            private bool hasStartPosition;

            protected AsEm9020_Hand_AtkD(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                phase = HandPhase.MoveToTarget;
                hasStartPosition = ControlHand != null;
                if (hasStartPosition)
                    startLocalPosition = ControlHand.localPosition;

                animName = StateId == (byte)As.HandL_AtkD ? "HandL_Attack_D" : "HandR_Attack_D";

                if (ControlHand == null || AimHand == null || Golem.CurrentTarget == null)
                {
                    StartAttack();
                    return;
                }
            }

            public override void Tick()
            {
                switch (phase)
                {
                    case HandPhase.MoveToTarget:
                        TickMoveToTarget();
                        break;
                    case HandPhase.Attack:
                        TickAttack();
                        break;
                    case HandPhase.ReturnToBody:
                        TickReturnToBody();
                        break;
                }
            }

            public override void Exit()
            {
                base.Exit();
                if (hasStartPosition && ControlHand != null)
                    ControlHand.localPosition = startLocalPosition;
            }

            private void TickMoveToTarget()
            {
                if (ControlHand == null || AimHand == null || Golem.CurrentTarget == null)
                {
                    StartAttack();
                    return;
                }

                Vector3 controlPos = ControlHand.position;
                float targetX = Golem.CurrentTarget.position.x;
                float deltaX = targetX - AimHand.position.x;
                float moveX = Mathf.Clamp(deltaX, -Golem.HandAtkD_MoveSpeed * Time.deltaTime, Golem.HandAtkD_MoveSpeed * Time.deltaTime);
                ControlHand.position = new Vector3(controlPos.x + moveX, controlPos.y, controlPos.z);

                if (Mathf.Abs(targetX - AimHand.position.x) <= Golem.HandAtkD_XTolerance)
                    StartAttack();
            }

            private void StartAttack()
            {
                timer = 0f;
                phase = HandPhase.Attack;

                if (CurrentAttack != null)
                {
                    Golem.SetActiveAttack(CurrentAttack);
                }
                Golem.BeginAttackSignIfNeeded();
                Golem.TryStartAttackAndTrigger();

                Golem.PlayAnimation(animName, false);
            }

            private void TickAttack()
            {
                timer += Time.deltaTime;
                float duration = Golem.GetAnimationDuration(animName, 1.5f);
                if (Golem.IsAnimationFinished(animName) || timer >= duration)
                {
                    phase = HandPhase.ReturnToBody;
                }
            }

            private void TickReturnToBody()
            {
                if (!hasStartPosition || ControlHand == null)
                {
                    Golem.ReqNextComboAction();
                    return;
                }

                ControlHand.localPosition = Vector3.MoveTowards(
                    ControlHand.localPosition,
                    startLocalPosition,
                    Golem.HandAtkD_ReturnSpeed * Time.deltaTime
                );

                if ((ControlHand.localPosition - startLocalPosition).sqrMagnitude <= Golem.HandAtkD_XTolerance * Golem.HandAtkD_XTolerance)
                {
                    ControlHand.localPosition = startLocalPosition;
                    Golem.ReqNextComboAction();
                }
            }
        }

        public class AsEm9020_HandL_AtkD : AsEm9020_Hand_AtkD
        {
            public override byte StateId => (byte)As.HandL_AtkD;
            public override string StateName => "AsEm9020_HandL_AtkD";
            protected override MvAttack CurrentAttack => Golem._Atk_HandL_AtkD;
            protected override Transform ControlHand => Golem._UtilBone_Handle_ArmL != null ? Golem._UtilBone_Handle_ArmL : Golem._Bf_HandL;
            protected override Transform AimHand => Golem._Bf_HandL;
            public AsEm9020_HandL_AtkD(EnemyContext context) : base(context) { }
        }

        public class AsEm9020_HandR_AtkD : AsEm9020_Hand_AtkD
        {
            public override byte StateId => (byte)As.HandR_AtkD;
            public override string StateName => "AsEm9020_HandR_AtkD";
            protected override MvAttack CurrentAttack => Golem._Atk_HandR_AtkD;
            protected override Transform ControlHand => Golem._UtilBone_Handle_ArmR != null ? Golem._UtilBone_Handle_ArmR : Golem._Bf_HandR;
            protected override Transform AimHand => Golem._Bf_HandR;
            public AsEm9020_HandR_AtkD(EnemyContext context) : base(context) { }
        }

        public abstract class AsEm9020_Hand_AtkSlap : MvActState_Em9020Atk
        {
            protected abstract MvAttack CurrentAttack { get; }
            protected abstract Transform TransHand { get; }
            private float timer;
            private string animName;

            protected AsEm9020_Hand_AtkSlap(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                if (CurrentAttack != null)
                {
                    Golem.SetActiveAttack(CurrentAttack);
                }
                Golem.BeginAttackSignIfNeeded();
                Golem.TryStartAttackAndTrigger();

                animName = StateId == (byte)As.HandL_Slap ? "HandL_Attack_Slap" : "HandR_Attack_Slap";
                Golem.PlayAnimation(animName, false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                float duration = Golem.GetAnimationDuration(animName, 1.5f);
                if (Golem.IsAnimationFinished(animName) || timer >= duration)
                {
                    Golem.ReqNextComboAction();
                }
            }
        }

        public class AsEm9020_HandL_Slap : AsEm9020_Hand_AtkSlap
        {
            public override byte StateId => (byte)As.HandL_Slap;
            public override string StateName => "AsEm9020_HandL_Slap";
            protected override MvAttack CurrentAttack => Golem._Atk_HandL_Slap;
            protected override Transform TransHand => Golem._Bf_HandL;
            public AsEm9020_HandL_Slap(EnemyContext context) : base(context) { }
        }

        public class AsEm9020_HandR_Slap : AsEm9020_Hand_AtkSlap
        {
            public override byte StateId => (byte)As.HandR_Slap;
            public override string StateName => "AsEm9020_HandR_Slap";
            protected override MvAttack CurrentAttack => Golem._Atk_HandR_Slap;
            protected override Transform TransHand => Golem._Bf_HandR;
            public AsEm9020_HandR_Slap(EnemyContext context) : base(context) { }
        }

        public abstract class AsEm9020_HandLR_AtkD_Base : MvActState_Em9020Atk
        {
            protected abstract void TriggerEffect();
            private float timer;
            private const string AnimName = "HandLR_Attack_D";
            private bool effectTriggered;

            protected AsEm9020_HandLR_AtkD_Base(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                effectTriggered = false;
                if (Golem._Atk_HandLR_AtkD != null)
                {
                    Golem.SetActiveAttack(Golem._Atk_HandLR_AtkD);
                }
                Golem.BeginAttackSignIfNeeded();
                Golem.TryStartAttackAndTrigger();

                Golem.PlayAnimation(AnimName, false);
            }

            public void OnSpineEvent(string eventName)
            {
                if (effectTriggered)
                    return;

                if (eventName == "AtkE" || eventName == "Attack/AtkE")
                {
                    effectTriggered = true;
                    TriggerEffect();
                }
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                float duration = Golem.GetAnimationDuration(AnimName, 2.0f);
                if (Golem.IsAnimationFinished(AnimName) || timer >= duration)
                {
                    Golem.ReqNextComboAction();
                }
            }
        }

        public class AsEm9020_HandLR_AtkD_RockFall : AsEm9020_HandLR_AtkD_Base
        {
            public override byte StateId => (byte)As.HandLR_AtkD;
            public override string StateName => "AsEm9020_HandLR_AtkD_RockFall";
            public AsEm9020_HandLR_AtkD_RockFall(EnemyContext context) : base(context) { }

            protected override void TriggerEffect()
            {
                Golem.SpawnRockFall();
            }
        }

        public class AsEm9020_HandLR_AtkD_SpawnEm : AsEm9020_HandLR_AtkD_Base
        {
            public override byte StateId => (byte)As.HandLR_AtkD_SpawnEm;
            public override string StateName => "AsEm9020_HandLR_AtkD_SpawnEm";
            public AsEm9020_HandLR_AtkD_SpawnEm(EnemyContext context) : base(context) { }

            protected override void TriggerEffect()
            {
                Golem.SpawnMiniGolems();
            }
        }

        public abstract class AsEm9020_StepBase : MvActState_Em9020
        {
            protected abstract float Direction { get; }
            private float timer;
            private Vector3 startPos;
            private Vector3 targetPos;
            private string animName;

            protected AsEm9020_StepBase(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                startPos = Golem.transform.position;

                float stepDist = Golem.Step_Dist;
                if (Golem.CurrentTarget != null)
                {
                    float playerDeltaX = Golem.CurrentTarget.position.x - startPos.x;
                    // Nếu bước đi cùng hướng với Player
                    if (Mathf.Sign(Direction) == Mathf.Sign(playerDeltaX))
                    {
                        float distToPlayerX = Mathf.Abs(playerDeltaX);
                        // Nhảy tiếp cận Player (giữ khoảng cách đệm 3 units, tối đa 12 units)
                        stepDist = Mathf.Clamp(distToPlayerX - 3f, Golem.Step_Dist, 12f);
                    }
                    else
                    {
                        // Nếu bước đi hướng về tâm Arena (quay lại vị trí trung tâm)
                        float centerDeltaX = Golem.SpawnPos.x - startPos.x;
                        if (Mathf.Sign(Direction) == Mathf.Sign(centerDeltaX))
                        {
                            stepDist = Mathf.Clamp(Mathf.Abs(centerDeltaX), Golem.Step_Dist, 12f);
                        }
                    }
                }

                targetPos = startPos + new Vector3(Direction * stepDist, 0f, 0f);

                animName = Direction < 0f ? "Step_L" : "Step_R";
                Golem.PlayAnimation(animName, false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                float duration = Golem.GetAnimationDuration(animName, 1.0f);
                float t = Mathf.Clamp01(timer / duration);
                float evaluatedT = Golem.Step_Curve.Evaluate(t);
                Vector3 newPos = Vector3.Lerp(startPos, targetPos, evaluatedT);

                if (Golem._myRb != null)
                {
                    Golem._myRb.linearVelocity = Vector2.zero;
                    Golem._myRb.MovePosition(newPos);
                }
                else
                {
                    Golem.transform.position = newPos;
                }

                if (Golem.IsAnimationFinished(animName) || timer >= duration)
                {
                    Golem.ReqNextComboAction();
                }
            }
        }

        public class AsEm9020_StepL : AsEm9020_StepBase
        {
            public override byte StateId => (byte)As.StepL;
            public override string StateName => "AsEm9020_StepL";
            protected override float Direction => -1f;
            public AsEm9020_StepL(EnemyContext context) : base(context) { }
        }

        public class AsEm9020_StepR : AsEm9020_StepBase
        {
            public override byte StateId => (byte)As.StepR;
            public override string StateName => "AsEm9020_StepR";
            protected override float Direction => 1f;
            public AsEm9020_StepR(EnemyContext context) : base(context) { }
        }

        public abstract class AsEm9020_LaserBase : MvActState_Em9020
        {
            private float timer;
            private bool loopPlayed;
            private bool endPlayed;
            private float endTimer;

            private float spawnTimer;
            private int spawnedCount;
            private int maxSpawnCount;

            protected abstract bool IsClockwise { get; }

            protected AsEm9020_LaserBase(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                loopPlayed = false;
                endPlayed = false;
                endTimer = 0f;

                spawnTimer = 0f;
                spawnedCount = 0;
                maxSpawnCount = Golem.Laser_Deg > 0f ? Mathf.CeilToInt(360f / Golem.Laser_Deg) : 8;

                Golem.PlayAnimation("Attack_Laser_Start", false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                float startDuration = Golem.GetAnimationDuration("Attack_Laser_Start", 0.5f);
                if (!loopPlayed && (Golem.IsAnimationFinished("Attack_Laser_Start") || timer >= startDuration))
                {
                    loopPlayed = true;
                    Golem.PlayAnimation("Attack_Laser_Loop", true);
                    spawnTimer = 0f;
                }

                if (loopPlayed && !endPlayed)
                {
                    spawnTimer += Time.deltaTime;
                    if (spawnTimer >= Golem.Laser_Interval && spawnedCount < maxSpawnCount)
                    {
                        spawnTimer -= Golem.Laser_Interval;
                        SpawnLaserProjectile(spawnedCount);
                        spawnedCount++;
                    }

                    if (spawnedCount >= maxSpawnCount)
                    {
                        endPlayed = true;
                        endTimer = 0f;
                        Golem.PlayAnimation("Attack_Laser_End", false);
                    }
                }

                if (endPlayed)
                {
                    endTimer += Time.deltaTime;
                    float endDuration = Golem.GetAnimationDuration("Attack_Laser_End", 1.5f);
                    if (Golem.IsAnimationFinished("Attack_Laser_End") || endTimer >= endDuration)
                        Golem.ReqNextComboAction();
                }
            }

            private void SpawnLaserProjectile(int index)
            {
                if (Golem.LaserNormalPrefab == null) return;

                Vector3 spawnPos = Golem._Bf_Handle_Eye != null ? Golem._Bf_Handle_Eye.position : Golem.transform.position;
                float angle = IsClockwise ? (-index * Golem.Laser_Deg) : (index * Golem.Laser_Deg);
                Quaternion baseRot = Golem._Bf_Handle_Eye != null ? Golem._Bf_Handle_Eye.rotation : Golem.transform.rotation;
                Quaternion targetRot = baseRot * Quaternion.Euler(0f, 0f, angle);

                var fireBallComp = Golem.LaserNormalPrefab.GetComponent<DreamKnight.Enemy.FireBall>();
                if (fireBallComp != null)
                {
                    var fireBall = DreamKnight.Enemy.FireBallPoolManager.Instance.Spawn(fireBallComp, spawnPos, targetRot);
                    if (fireBall != null)
                    {
                        Vector2 dir = targetRot * Vector2.right;
                        fireBall.Initialize(Golem.gameObject, fireBallComp.Damage, dir);
                    }
                }
                else
                {
                    UnityEngine.Object.Instantiate(
                        Golem.LaserNormalPrefab,
                        spawnPos,
                        targetRot
                    );
                }
            }
        }

        public class AsEm9020_LaserCW : AsEm9020_LaserBase
        {
            public override byte StateId => (byte)As.LaserCW;
            public override string StateName => "AsEm9020_LaserCW";
            protected override bool IsClockwise => true;
            public AsEm9020_LaserCW(EnemyContext context) : base(context) { }
        }

        public class AsEm9020_LaserCCW : AsEm9020_LaserBase
        {
            public override byte StateId => (byte)As.LaserCCW;
            public override string StateName => "AsEm9020_LaserCCW";
            protected override bool IsClockwise => false;
            public AsEm9020_LaserCCW(EnemyContext context) : base(context) { }
        }

        public abstract class AsEm9020_LaserSuperBase : MvActState_Em9020
        {
            private float timer;
            private bool chargePlayed;
            private bool shotFired;
            private bool endPlayed;
            private float endTimer;
            private GameObject _laserObj;
            private Vector3 handleEyeStartLocalScale;
            private bool hasHandleEyeStartScale;

            protected AsEm9020_LaserSuperBase(EnemyContext context) : base(context) { }

            protected abstract string StartAnimation { get; }
            protected abstract string ChargeAnimation { get; }
            protected abstract string ShotAnimation { get; }
            protected abstract string EndAnimation { get; }
            protected abstract float HandleEyeDirectionX { get; }

            public override void Enter()
            {
                timer = 0f;
                chargePlayed = false;
                shotFired = false;
                endPlayed = false;
                endTimer = 0f;
                _laserObj = null;
                CaptureHandleEyeScale();
                ApplyHandleEyeDirection();
                SetLaserActive(false);
                Golem.PlayAnimation(StartAnimation, false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;

                float startDuration = Golem.GetAnimationDuration(StartAnimation, 0.4f);
                if (!chargePlayed && !shotFired && (Golem.IsAnimationFinished(StartAnimation) || timer >= startDuration))
                {
                    chargePlayed = true;
                    Golem.PlayAnimation(ChargeAnimation, true);
                    ApplyHandleEyeDirection();
                }

                if (timer >= Golem.LaserSuper_LoopTime_Charge && !shotFired)
                {
                    shotFired = true;
                    Golem.PlayAnimation(ShotAnimation, true);
                    ApplyHandleEyeDirection();

                    if (Golem.LaserSuperPrefab != null)
                    {
                        _laserObj = Golem.LaserSuperPrefab;
                        SetLaserActive(true);

                        if (_laserObj != null)
                        {
                            var attacks = _laserObj.GetComponentsInChildren<MvAttack>(true);
                            foreach (var atk in attacks)
                            {
                                if (atk != null)
                                {
                                    atk.SetOwner(Golem);
                                    atk.TryStartAttack();
                                }
                            }
                        }
                    }

                    if (Golem._AtkAlways_LaserSuperHand != null)
                    {
                        Golem.SetActiveAttack(Golem._AtkAlways_LaserSuperHand);
                        Golem.TryStartAttackAndTrigger();
                    }
                }

                if (!endPlayed && timer >= Golem.LaserSuper_LoopTime_Charge + Golem.LaserSuper_LoopTime_Shot)
                {
                    endPlayed = true;
                    endTimer = 0f;
                    Golem.PlayAnimation(EndAnimation, false);

                    if (_laserObj != null)
                    {
                        SetLaserActive(false);
                    }
                    RestoreHandleEyeDirection();
                }

                if (endPlayed)
                {
                    endTimer += Time.deltaTime;
                    float endDuration = Golem.GetAnimationDuration(EndAnimation, 1.5f);
                    if (Golem.IsAnimationFinished(EndAnimation) || endTimer >= endDuration)
                        Golem.ReqNextComboAction();
                }
            }

            private void SetLaserActive(bool active)
            {
                GameObject laserObj = _laserObj != null ? _laserObj : Golem.LaserSuperPrefab;
                if (laserObj == null)
                    return;

                laserObj.SetActive(active);
                if (!active && _laserObj == laserObj)
                    _laserObj = null;
            }

            private void CaptureHandleEyeScale()
            {
                hasHandleEyeStartScale = Golem._Bf_Handle_Eye != null;
                if (hasHandleEyeStartScale)
                    handleEyeStartLocalScale = Golem._Bf_Handle_Eye.localScale;
            }

            private void ApplyHandleEyeDirection()
            {
                if (!hasHandleEyeStartScale || Golem._Bf_Handle_Eye == null)
                    return;

                Vector3 scale = handleEyeStartLocalScale;
                float sign = HandleEyeDirectionX >= 0f ? 1f : -1f;
                scale.x = Mathf.Abs(scale.x) * sign;
                Golem._Bf_Handle_Eye.localScale = scale;
            }

            private void RestoreHandleEyeDirection()
            {
                if (hasHandleEyeStartScale && Golem._Bf_Handle_Eye != null)
                    Golem._Bf_Handle_Eye.localScale = handleEyeStartLocalScale;
            }

            public override void Exit()
            {
                base.Exit();
                SetLaserActive(false);
                RestoreHandleEyeDirection();
            }
        }

        public class AsEm9020_LaserSuperR : AsEm9020_LaserSuperBase
        {
            public override byte StateId => (byte)As.LaserSuperR;
            public override string StateName => "AsEm9020_LaserSuperR";
            protected override string StartAnimation => "AttackR_LaserSuper_Start";
            protected override string ChargeAnimation => "AttackR_LaserSuper_Loop_Charge";
            protected override string ShotAnimation => "AttackR_LaserSuper_Loop_Shot";
            protected override string EndAnimation => "AttackR_LaserSuper_End";
            protected override float HandleEyeDirectionX => 1f;
            public AsEm9020_LaserSuperR(EnemyContext context) : base(context) { }
        }

        public class AsEm9020_LaserSuperL : AsEm9020_LaserSuperBase
        {
            public override byte StateId => (byte)As.LaserSuperL;
            public override string StateName => "AsEm9020_LaserSuperL";
            protected override string StartAnimation => "AttackR_LaserSuper_Start";
            protected override string ChargeAnimation => "AttackR_LaserSuper_Loop_Charge";
            protected override string ShotAnimation => "AttackR_LaserSuper_Loop_Shot";
            protected override string EndAnimation => "AttackR_LaserSuper_End";
            protected override float HandleEyeDirectionX => -1f;
            public AsEm9020_LaserSuperL(EnemyContext context) : base(context) { }
        }

        public class AsEm9020_KnockOut : AsEmBoss_KnockOut
        {
            public override byte StateId => (byte)As.KnockOut;
            public override string StateName => "AsEm9020_KnockOut";
            private float timer;
            private bool loopPlayed;
            private bool endPlayed;
            private float endTimer;
            private float deadHideTimer;
            protected MvEm9020 Golem => Em as MvEm9020;

            public AsEm9020_KnockOut(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                base.Enter();
                timer = 0f;
                loopPlayed = false;
                endPlayed = false;
                endTimer = 0f;
                deadHideTimer = 0f;
                Golem.PlayAnimation("KnockOut_Start", false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;

                float startDuration = Golem.GetAnimationDuration("KnockOut_Start", 0.5f);
                if (!loopPlayed && (Golem.IsAnimationFinished("KnockOut_Start") || timer >= startDuration))
                {
                    loopPlayed = true;
                    Golem.PlayAnimation("KnockOut_Loop", true);
                }

                // Nếu HP <= 0 (đã chết), không phát hoạt ảnh KnockOut_End và ở trong loop mãi mãi
                if (Golem.CurrentHealth <= 0f)
                {
                    deadHideTimer += Time.deltaTime;
                    Golem.StopHorizontalMotion();

                    if (deadHideTimer >= 2f)
                    {
                        Golem.gameObject.SetActive(false);
                    }

                    return;
                }

                float duration = 5.0f;
                if (!endPlayed && timer >= duration)
                {
                    endPlayed = true;
                    endTimer = 0f;
                    Golem.PlayAnimation("KnockOut_End", false);
                }

                if (endPlayed)
                {
                    endTimer += Time.deltaTime;
                    float endDuration = Golem.GetAnimationDuration("KnockOut_End", 1.5f);
                    if (Golem.IsAnimationFinished("KnockOut_End") || endTimer >= endDuration)
                        Golem.ReqNextComboAction();
                }
            }
        }

        public class AsEm9020_Hide : MvActState_Em9020
        {
            public override byte StateId => (byte)As.Hide;
            public override string StateName => "AsEm9020_Hide";
            private float timer;
            private bool loopPlayed;

            public AsEm9020_Hide(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                loopPlayed = false;
                Golem.PlayAnimation("HideStart", false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;

                float startDuration = Golem.GetAnimationDuration("HideStart", 0.5f);
                if (!loopPlayed && (Golem.IsAnimationFinished("HideStart") || timer >= startDuration))
                {
                    loopPlayed = true;
                    Golem.PlayAnimation("HideLoop", true);
                }

                if (timer >= Golem.HideTime)
                {
                    if (Golem._Brain != null)
                    {
                        Golem.transform.position = Golem._Brain.ChoiceHideEndPos();
                    }
                    Golem.ChangeEnemyState((byte)As.HideEnd);
                }
            }
        }

        public class AsEm9020_HideLong : MvActState_Em9020
        {
            public override byte StateId => (byte)As.HideLong;
            public override string StateName => "AsEm9020_HideLong";
            private float timer;
            private bool loopPlayed;

            public AsEm9020_HideLong(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                loopPlayed = false;
                Golem.PlayAnimation("HideStart", false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;

                float startDuration = Golem.GetAnimationDuration("HideStart", 0.5f);
                if (!loopPlayed && (Golem.IsAnimationFinished("HideStart") || timer >= startDuration))
                {
                    loopPlayed = true;
                    Golem.PlayAnimation("HideLoop", true);
                }

                if (timer >= Golem.HideSpawnEmTime)
                {
                    if (Golem._Brain != null)
                    {
                        Golem.transform.position = Golem._Brain.ChoiceHideEndPos();
                    }
                    Golem.ChangeEnemyState((byte)As.HideEnd);
                }
            }
        }

        public class AsEm9020_HideEnd : MvActState_Em9020
        {
            public override byte StateId => (byte)As.HideEnd;
            public override string StateName => "AsEm9020_HideEnd";
            private float timer;

            public AsEm9020_HideEnd(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                Golem.PlayAnimation("HideEnd", false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                float endDuration = Golem.GetAnimationDuration("HideEnd", 1.0f);
                if (Golem.IsAnimationFinished("HideEnd") || timer >= endDuration)
                {
                    Golem.ReqNextComboAction();
                }
            }
        }
    }
}
