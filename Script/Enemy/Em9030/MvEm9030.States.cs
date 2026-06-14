using UnityEngine;

namespace Mv
{
    public partial class MvEm9030
    {
        public abstract class MvActState_Em9030 : MvActState_Em
        {
            protected MvEm9030 Boss => Em as MvEm9030;
            protected MvActState_Em9030(EnemyContext context) : base(context) { }
        }

        public abstract class MvActState_Em9030Atk : AsEm_Atk_Base
        {
            protected MvEm9030 Boss => Em as MvEm9030;
            protected MvActState_Em9030Atk(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_Entrance : MvActState_Em9030
        {
            public override byte StateId => (byte)As.Entrance;
            public override string StateName => "AsEm9030_Entrance";
            private float timer;
            private bool endPlayed;

            public AsEm9030_Entrance(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                endPlayed = false;
                Boss.PlayAnimation("Entrance_Loop", true);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (!endPlayed && timer >= Boss.Entrance_LookTime)
                {
                    endPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation("Entrance_End", false);
                }

                if (endPlayed && (Boss.IsAnimationFinished("Entrance_End") || timer >= Boss.GetAnimationDuration("Entrance_End", 1f)))
                    Boss.ReqNextComboAction();
            }
        }

        public class AsEm9030_Wait : MvActState_Em9030
        {
            public override byte StateId => (byte)As.Wait;
            public override string StateName => "AsEm9030_Wait";
            protected virtual float WaitTime => Boss.IdleWaitTime;
            private float timer;

            public AsEm9030_Wait(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                Boss.PlayAnimation("Idle", true);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (timer >= WaitTime)
                    Boss.ReqNextComboAction();
            }
        }

        public class AsEm9030_WaitEx : AsEm9030_Wait
        {
            public override byte StateId => (byte)As.WaitEx;
            public override string StateName => "AsEm9030_WaitEx";
            protected override float WaitTime => Boss.IdleWaitTimeEx;
            public AsEm9030_WaitEx(EnemyContext context) : base(context) { }
        }

        public abstract class AsEm9030_AnimState : MvActState_Em9030
        {
            protected abstract string AnimationName { get; }
            protected virtual bool Loop => false;
            protected virtual float FallbackDuration => 1f;
            private float timer;

            protected AsEm9030_AnimState(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                Boss.PlayAnimation(AnimationName, Loop);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (!Loop && (Boss.IsAnimationFinished(AnimationName) || timer >= Boss.GetAnimationDuration(AnimationName, FallbackDuration)))
                    Boss.ReqNextComboAction();
                else if (Loop && timer >= FallbackDuration)
                    Boss.ReqNextComboAction();
            }
        }

        public abstract class AsEm9030_JumpBase : MvActState_Em9030
        {
            protected abstract Vector2 JumpPower { get; }
            private float timer;
            private bool loopPlayed;

            protected AsEm9030_JumpBase(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                loopPlayed = false;
                Boss.BeginAirborne();
                Boss.PlayAnimation("Jump_Start", false);

                Rigidbody2D rb = Boss.Body;
                if (rb != null)
                {
                    float x = JumpPower.x;
                    if (Boss.CurrentTarget != null)
                        x *= Mathf.Sign(Boss.CurrentTarget.position.x - Boss.transform.position.x);
                    rb.linearVelocity = new Vector2(x, JumpPower.y);
                }
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (!loopPlayed && (Boss.IsAnimationFinished("Jump_Start") || timer >= Boss.GetAnimationDuration("Jump_Start", 0.4f)))
                {
                    loopPlayed = true;
                    Boss.PlayAnimation("Jump_Loop", true);
                }

                Rigidbody2D rb = Boss.Body;
                if (loopPlayed && rb != null && rb.linearVelocity.y <= 0.05f)
                {
                    if (Boss.IsNextComboActionAirborneAttack)
                    {
                        Boss.HoldAirborne();
                        Boss.ReqNextComboAction();
                    }
                    else
                    {
                        Boss.ChangeEnemyState((byte)As.Fall);
                    }
                }
                else if (timer >= 2f)
                {
                    if (Boss.IsNextComboActionAirborneAttack)
                    {
                        Boss.HoldAirborne();
                        Boss.ReqNextComboAction();
                    }
                    else
                    {
                        Boss.ChangeEnemyState((byte)As.Fall);
                    }
                }
            }
        }

        public class AsEm9030_Jump : AsEm9030_JumpBase
        {
            public override byte StateId => (byte)As.Jump;
            public override string StateName => "AsEm9030_Jump";
            protected override Vector2 JumpPower => Boss.JumpPow;
            public AsEm9030_Jump(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_JumpMini : AsEm9030_JumpBase
        {
            public override byte StateId => (byte)As.JumpMini;
            public override string StateName => "AsEm9030_JumpMini";
            protected override Vector2 JumpPower => Boss.JumpPowMini;
            public AsEm9030_JumpMini(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_JumpMiniLong : AsEm9030_JumpMini
        {
            public override byte StateId => (byte)As.JumpMiniLong;
            public override string StateName => "AsEm9030_JumpMiniLong";
            protected override Vector2 JumpPower => Boss.JumpPowMiniLong;
            public AsEm9030_JumpMiniLong(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_JumpSide : AsEm9030_JumpMini
        {
            public override byte StateId => (byte)As.JumpSide;
            public override string StateName => "AsEm9030_JumpSide";
            protected override Vector2 JumpPower => Boss.JumpPowSide;
            public AsEm9030_JumpSide(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_FallLand : AsEm9030_AnimState
        {
            public override byte StateId => (byte)As.FallLand;
            public override string StateName => "AsEm9030_FallLand";
            protected override string AnimationName => "Land";
            protected override float FallbackDuration => 0.6f;
            public AsEm9030_FallLand(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                Boss.FinishAirborne();
                base.Enter();
            }
        }

        public class AsEm9030_Fall : MvActState_Em9030
        {
            public override byte StateId => (byte)As.Fall;
            public override string StateName => "AsEm9030_Fall";
            private float timer;
            public AsEm9030_Fall(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                Boss.StartFallingFromAirborne();
                Boss.PlayAnimation("Fall_Loop", true);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                Rigidbody2D rb = Boss.Body;

                if (rb == null)
                {
                    Boss.ChangeEnemyState((byte)As.FallLand);
                    return;
                }

                bool falling = rb.linearVelocity.y <= 0.05f;
                if ((falling && Boss.HasReachedLandingHeight()) || timer >= 3f)
                    Boss.ChangeEnemyState((byte)As.FallLand);
            }
        }

        public class AsEm9030_ApproachMelee : MvActState_Em9030
        {
            public override byte StateId => (byte)As.ApproachMelee;
            public override string StateName => "AsEm9030_ApproachMelee";
            private float timer;

            public AsEm9030_ApproachMelee(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                Boss.SetRunAnimation(true, false);
                Boss.PlayAnimation("Run", true);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;

                if (Boss.CurrentTarget == null)
                {
                    StopAndContinue();
                    return;
                }

                Vector3 targetPos = Boss.CurrentTarget.position;
                Vector3 bossPos = Boss.transform.position;
                float deltaX = targetPos.x - bossPos.x;
                float absX = Mathf.Abs(deltaX);
                float absY = Mathf.Abs(targetPos.y - bossPos.y);

                if ((absX <= Boss.MeleeApproachStopDistance && absY <= Boss.MeleeApproachVerticalTolerance)
                    || timer >= Boss.MeleeApproachTimeout)
                {
                    StopAndContinue();
                    return;
                }

                Boss.FaceByDeltaX(deltaX);
                Boss.MoveChaseMotion(deltaX);
            }

            public override void Exit()
            {
                Boss.SetRunAnimation(false, true);
                Rigidbody2D rb = Boss.Body;
                if (rb != null)
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            private void StopAndContinue()
            {
                Rigidbody2D rb = Boss.Body;
                if (rb != null)
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                Boss.SetRunAnimation(false, true);
                Boss.ReqNextComboAction();
            }
        }

        public abstract class AsEm9030_MeleeBase : MvActState_Em9030Atk
        {
            protected abstract string AnimationName { get; }
            protected abstract MvAttack Attack { get; }

            private float timer;

            protected AsEm9030_MeleeBase(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                if (Attack != null)
                    Boss.SetActiveAttack(Attack);

                Boss.BeginAttackSignIfNeeded();
                Boss.TryStartAttackAndTrigger();
                Boss.PlayAnimation(AnimationName, false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;

                if (Boss.IsAnimationFinished(AnimationName) || timer >= Boss.GetAnimationDuration(AnimationName, 1.5f))
                    Boss.ReqNextComboAction();
            }

        }

        public class AsEm9030_Atk1 : AsEm9030_MeleeBase
        {
            public override byte StateId => (byte)As.Atk1;
            public override string StateName => "AsEm9030_Atk1";
            protected override string AnimationName => "Attack1";
            protected override MvAttack Attack => Boss.Atk_Atk1;
            public AsEm9030_Atk1(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_Atk2 : AsEm9030_MeleeBase
        {
            public override byte StateId => (byte)As.Atk2;
            public override string StateName => "AsEm9030_Atk2";
            protected override string AnimationName => "Attack2";
            protected override MvAttack Attack => Boss.Atk_Atk2;
            public AsEm9030_Atk2(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_Atk3 : AsEm9030_MeleeBase
        {
            public override byte StateId => (byte)As.Atk3;
            public override string StateName => "AsEm9030_Atk3";
            protected override string AnimationName => "Attack3";
            protected override MvAttack Attack => Boss.Atk_Atk3;
            public AsEm9030_Atk3(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_SlashV : AsEm9030_MeleeBase
        {
            public override byte StateId => (byte)As.SlashV;
            public override string StateName => "AsEm9030_SlashV";
            protected override string AnimationName => "Swing1";
            protected override MvAttack Attack => Boss.Atk_SlashV;
            public AsEm9030_SlashV(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_SlashH : AsEm9030_MeleeBase
        {
            public override byte StateId => (byte)As.SlashH;
            public override string StateName => "AsEm9030_SlashH";
            protected override string AnimationName => "Swing2";
            protected override MvAttack Attack => Boss.Atk_SlashH;
            public AsEm9030_SlashH(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_Provoke : MvActState_Em9030
        {
            public override byte StateId => (byte)As.Provoke;
            public override string StateName => "AsEm9030_Provoke";
            private float timer;
            private bool loopPlayed;
            private bool endPlayed;

            public AsEm9030_Provoke(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                loopPlayed = false;
                endPlayed = false;
                Boss.PlayAnimation("Provoke_Start", false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (!loopPlayed && (Boss.IsAnimationFinished("Provoke_Start") || timer >= Boss.GetAnimationDuration("Provoke_Start", 0.4f)))
                {
                    loopPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation("Provoke_Loop", true);
                }
                else if (loopPlayed && !endPlayed && timer >= Boss.Provoke_LoopTime)
                {
                    endPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation("Provoke_End", false);
                }
                else if (endPlayed && (Boss.IsAnimationFinished("Provoke_End") || timer >= Boss.GetAnimationDuration("Provoke_End", 0.5f)))
                {
                    Boss.ReqNextComboAction();
                }
            }
        }

        public class AsEm9030_Dodge : AsEm9030_AnimState
        {
            public override byte StateId => (byte)As.Dodge;
            public override string StateName => "AsEm9030_Dodge";
            protected override string AnimationName => "Dodge";
            protected override float FallbackDuration => 0.8f;
            public AsEm9030_Dodge(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_Counter : MvActState_Em9030Atk
        {
            public override byte StateId => (byte)As.Counter;
            public override string StateName => "AsEm9030_Counter";
            private float timer;
            private float moveDirection;
            private bool loopPlayed;
            private bool endPlayed;
            private bool counterWindowOpen;

            public AsEm9030_Counter(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                moveDirection = Boss.FacingDirection.x;
                loopPlayed = false;
                endPlayed = false;
                counterWindowOpen = false;
                if (Boss.Atk_Counter != null)
                    Boss.SetActiveAttack(Boss.Atk_Counter);
                Boss.PlayAnimation("Counter_Start", false);
                Boss.BeginAttackSignIfNeeded();
                Boss.TryStartAttackAndTrigger();
                OpenCounterWindow();
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (!endPlayed)
                    ApplyCounterMotion();

                if (!loopPlayed && (Boss.IsAnimationFinished("Counter_Start") || timer >= Boss.GetAnimationDuration("Counter_Start", 0.4f)))
                {
                    loopPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation("Counter_Loop", true);
                }

                if (loopPlayed && !endPlayed && timer >= Boss.CounterAtkTimer)
                {
                    endPlayed = true;
                    timer = 0f;
                    CloseCounterWindow();
                    StopCounterMotion();
                    Boss.PlayAnimation("Counter_End", false);
                }
                else if (endPlayed && (Boss.IsAnimationFinished("Counter_End") || timer >= Boss.GetAnimationDuration("Counter_End", 0.6f)))
                {
                    Boss.ReqNextComboAction();
                }
            }

            public override void Exit()
            {
                CloseCounterWindow();
                StopCounterMotion();
            }

            private void OpenCounterWindow()
            {
                if (counterWindowOpen || Boss.Atk_Counter == null)
                    return;

                counterWindowOpen = true;
                Boss.Atk_Counter.OnMvAnimEvent("AtkS", null);
            }

            private void CloseCounterWindow()
            {
                if (!counterWindowOpen || Boss.Atk_Counter == null)
                    return;

                counterWindowOpen = false;
                Boss.Atk_Counter.OnMvAnimEvent("AtkE", null);
            }

            private void ApplyCounterMotion()
            {
                Rigidbody2D rb = Boss.Body;
                if (rb == null || Boss.CounterMoveSpeed <= 0.01f)
                    return;

                if (Boss.IsCounterMovementBlocked(moveDirection))
                {
                    moveDirection = -moveDirection;
                    Boss.FaceByDeltaX(moveDirection);
                }

                float duration = Mathf.Max(0.01f, Boss.CounterAtkTimer);
                float t = Mathf.Clamp01(timer / duration);
                float curve = Boss.CounterMoveSpeedCurve != null ? Boss.CounterMoveSpeedCurve.Evaluate(t) : 1f;
                rb.linearVelocity = new Vector2(moveDirection * Boss.CounterMoveSpeed * curve, rb.linearVelocity.y);
            }

            private void StopCounterMotion()
            {
                Rigidbody2D rb = Boss.Body;
                if (rb != null)
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        public abstract class AsEm9030_MagicShotBase : MvActState_Em9030
        {
            protected abstract GameObject Prefab { get; }
            protected abstract float Damage { get; }
            protected virtual int ShotCount => 1;

            private float timer;
            private bool shotFired;
            private bool endPlayed;

            protected AsEm9030_MagicShotBase(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                shotFired = false;
                endPlayed = false;
                Boss.PlayAnimation("MagicShot_Start", false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (!shotFired && (Boss.IsAnimationFinished("MagicShot_Start") || timer >= Boss.GetAnimationDuration("MagicShot_Start", 0.35f)))
                {
                    shotFired = true;
                    timer = 0f;
                    Boss.PlayAnimation("MagicShot_Shot", false);
                    FireShots();
                }
                else if (shotFired && !endPlayed && (Boss.IsAnimationFinished("MagicShot_Shot") || timer >= Boss.GetAnimationDuration("MagicShot_Shot", 0.4f)))
                {
                    endPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation("MagicShot_End", false);
                }
                else if (endPlayed && (Boss.IsAnimationFinished("MagicShot_End") || timer >= Boss.GetAnimationDuration("MagicShot_End", 0.6f)))
                {
                    Boss.ReqNextComboAction();
                }
            }

            protected virtual void FireShots()
            {
                Vector2 dir = ResolveDirection();
                for (int i = 0; i < ShotCount; i++)
                    Boss.SpawnShot(Prefab, Boss.MagicHandlePosition, dir, Damage);
            }

            protected virtual Vector2 ResolveDirection()
            {
                if (Boss.CurrentTarget == null)
                    return Boss.FacingDirection;

                Vector2 dir = Boss.CurrentTarget.position - Boss.MagicHandlePosition;
                return dir.sqrMagnitude > 0.0001f ? dir.normalized : Boss.FacingDirection;
            }

            protected Vector2 ResolveHorizontalDirection()
            {
                return Boss.FacingDirection.x >= 0f ? Vector2.right : Vector2.left;
            }
        }

        public class AsEm9030_MagicShotSpritShot : AsEm9030_MagicShotBase
        {
            public override byte StateId => (byte)As.MagicShotSpritShot;
            public override string StateName => "AsEm9030_MagicShotSpritShot";
            protected override GameObject Prefab => Boss.SpritShotPrefab;
            protected override float Damage => Boss.AtkDmg_SpritShot;
            protected override int ShotCount => 3;
            public AsEm9030_MagicShotSpritShot(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_MagicShotFlySwordBig : AsEm9030_MagicShotBase
        {
            public override byte StateId => (byte)As.MagicShotFlySwordBig;
            public override string StateName => "AsEm9030_MagicShotFlySwordBig";
            protected override GameObject Prefab => Boss.FlySwordBigPrefab;
            protected override float Damage => Boss.AtkDmg_FlySwordBig;
            protected override Vector2 ResolveDirection() => ResolveHorizontalDirection();
            public AsEm9030_MagicShotFlySwordBig(EnemyContext context) : base(context) { }
        }

        public abstract class AsEm9030_MagicShotAirBase : MvActState_Em9030
        {
            protected abstract GameObject Prefab { get; }
            protected abstract float LoopTime { get; }
            protected abstract float Damage { get; }
            protected virtual float ShotInterval => 0.3f;
            protected virtual bool FireImmediatelyOnLoop => false;
            protected virtual int MaxShotCount => int.MaxValue;
            protected virtual string StartAnimation => "MagicShotAir_Start";
            protected virtual string LoopAnimation => "MagicShotAir_Loop";
            protected virtual string EndAnimation => "MagicShotAir_End";
            protected virtual bool UsesAirborneHover => true;
            private float timer;
            private float shotTimer;
            protected int shotCount;
            private bool loopPlayed;
            private bool endPlayed;

            protected AsEm9030_MagicShotAirBase(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                shotTimer = 0f;
                shotCount = 0;
                loopPlayed = false;
                endPlayed = false;
                if (UsesAirborneHover)
                    Boss.HoldAirborne();
                Boss.PlayAnimation(StartAnimation, false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (!loopPlayed && (Boss.IsAnimationFinished(StartAnimation) || timer >= Boss.GetAnimationDuration(StartAnimation, 0.4f)))
                {
                    loopPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation(LoopAnimation, true);
                    if (FireImmediatelyOnLoop)
                        FireLoopShot();
                }

                if (loopPlayed && !endPlayed)
                {
                    shotTimer += Time.deltaTime;
                    if (shotCount < MaxShotCount && shotTimer >= ShotInterval)
                    {
                        shotTimer = 0f;
                        FireLoopShot();
                    }

                    if (timer >= LoopTime)
                    {
                        endPlayed = true;
                        timer = 0f;
                        Boss.PlayAnimation(EndAnimation, false);
                    }
                }
                else if (endPlayed && (Boss.IsAnimationFinished(EndAnimation) || timer >= Boss.GetAnimationDuration(EndAnimation, 0.6f)))
                {
                    if (!UsesAirborneHover)
                    {
                        Boss.ReqNextComboAction();
                    }
                    else if (Boss.IsNextComboActionAirborneAttack)
                    {
                        Boss.ReqNextComboAction();
                    }
                    else
                    {
                        Boss.ChangeEnemyState((byte)As.Fall);
                    }
                }
            }

            protected virtual Vector2 ResolveDirection()
            {
                if (Boss.CurrentTarget == null)
                    return Boss.FacingDirection;
                Vector2 dir = Boss.CurrentTarget.position - Boss.MagicHandlePosition;
                return dir.sqrMagnitude > 0.0001f ? dir.normalized : Boss.FacingDirection;
            }

            protected Vector2 ResolveHorizontalDirection()
            {
                return Boss.FacingDirection.x >= 0f ? Vector2.right : Vector2.left;
            }

            protected virtual void FireLoopShot()
            {
                if (shotCount >= MaxShotCount)
                    return;

                shotCount++;
                Boss.SpawnShot(Prefab, Boss.MagicHandlePosition, ResolveDirection(), Damage);
            }
        }

        public class AsEm9030_MagicShotAirSpritShot : AsEm9030_MagicShotAirBase
        {
            public override byte StateId => (byte)As.MagicShotAirSpritShot;
            public override string StateName => "AsEm9030_MagicShotAirSpritShot";
            protected override GameObject Prefab => Boss.AirSpritShotPrefab;
            protected override float LoopTime => Boss.MagicShotAir_SpritShot_LoopTime;
            protected override float Damage => Boss.AtkDmg_SpritShot;
            public AsEm9030_MagicShotAirSpritShot(EnemyContext context) : base(context) { }
        }

        public abstract class AsEm9030_MagicShotSideBase : AsEm9030_MagicShotAirBase
        {
            protected override float ShotInterval => 0.4f;
            protected override string StartAnimation => "MagicShotSide_Start";
            protected override string LoopAnimation => "MagicShotSide_Loop";
            protected override string EndAnimation => "MagicShotSide_End";
            protected override bool UsesAirborneHover => false;
            protected override Vector2 ResolveDirection() => ResolveHorizontalDirection();
            protected AsEm9030_MagicShotSideBase(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_MagicShotSideLaser : AsEm9030_MagicShotSideBase
        {
            public override byte StateId => (byte)As.MagicShotSideLaser;
            public override string StateName => "AsEm9030_MagicShotSideLaser";
            protected override GameObject Prefab => Boss.LaserPrefab;
            protected override float LoopTime => Boss.MagicShotSide0_Laser_LoopTime;
            protected override float Damage => Boss.AtkDmg_Laser;
            protected override bool FireImmediatelyOnLoop => true;
            protected override int MaxShotCount => 1;
            public AsEm9030_MagicShotSideLaser(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_MagicShotSideEruption : AsEm9030_MagicShotSideBase
        {
            public override byte StateId => (byte)As.MagicShotSideEruption;
            public override string StateName => "AsEm9030_MagicShotSideEruption";
            protected override GameObject Prefab => Boss.EruptionPrefab;
            protected override float LoopTime => Boss.MagicShotSide0_Eruption_LoopTime;
            protected override float Damage => Boss.AtkDmg_Eruption;
            public AsEm9030_MagicShotSideEruption(EnemyContext context) : base(context) { }

            protected override void FireLoopShot()
            {
                if (shotCount >= MaxShotCount)
                    return;

                shotCount++;

                int countPerSide = Mathf.Max(1, Boss.EruptionSideShotCount);
                float spacingY = Mathf.Max(0f, Boss.EruptionSideShotSpacingY);
                float centerIndex = (countPerSide - 1) * 0.5f;

                for (int i = 0; i < countPerSide; i++)
                {
                    float offsetY = (i - centerIndex) * spacingY;
                    Vector3 spawnPos = Boss.MagicHandlePosition + new Vector3(0f, offsetY, 0f);
                    Boss.SpawnShot(Prefab, spawnPos, Vector2.left, Damage);
                    Boss.SpawnShot(Prefab, spawnPos, Vector2.right, Damage);
                }
            }
        }

        public class AsEm9030_Scythe : MvActState_Em9030
        {
            public override byte StateId => (byte)As.Scythe;
            public override string StateName => "AsEm9030_Scythe";
            private float timer;
            private bool loopPlayed;
            private bool endPlayed;
            private GameObject scytheObject;

            public AsEm9030_Scythe(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                timer = 0f;
                loopPlayed = false;
                endPlayed = false;
                scytheObject = null;
                Boss.PlayAnimation("ScytheControl_Start", false);
            }

            public override void Exit()
            {
                ReleaseScythe();
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (!loopPlayed && (Boss.IsAnimationFinished("ScytheControl_Start") || timer >= Boss.GetAnimationDuration("ScytheControl_Start", 0.4f)))
                {
                    loopPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation("ScytheControl_Loop", true);
                    scytheObject = Boss.SpawnShot(Boss.ScythePrefab, Boss.MagicHandlePosition, Boss.FacingDirection, Boss.AtkDmg_Scythe);
                }
                else if (loopPlayed && !endPlayed && timer >= Boss.Scythe_LoopTime)
                {
                    endPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation("ScytheControl_End", false);
                }
                else if (endPlayed && (Boss.IsAnimationFinished("ScytheControl_End") || timer >= Boss.GetAnimationDuration("ScytheControl_End", 0.6f)))
                {
                    ReleaseScythe();
                    Boss.ReqNextComboAction();
                }
            }

            private void ReleaseScythe()
            {
                if (scytheObject == null)
                    return;

                Em9030ScytheProjectile scythe = scytheObject.GetComponent<Em9030ScytheProjectile>();
                if (scythe != null)
                    scythe.Finish();
                else
                    Boss.ReleaseLocalShot(scytheObject);

                scytheObject = null;
            }
        }

        public class AsEm9030_EmSpawn : AsEm9030_AnimState
        {
            public override byte StateId => (byte)As.EmSpawn;
            public override string StateName => "AsEm9030_EmSpawn";
            protected override string AnimationName => "EmSpawn";
            protected override float FallbackDuration => 1f;
            public AsEm9030_EmSpawn(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                base.Enter();
                if (Boss.EmSpawnPrefab != null)
                    UnityEngine.Object.Instantiate(Boss.EmSpawnPrefab, Boss.transform.position, Quaternion.identity);
            }
        }

        public class AsEm9030_EmSpawnEx : AsEm9030_EmSpawn
        {
            public override byte StateId => (byte)As.EmSpawnEx;
            public override string StateName => "AsEm9030_EmSpawnEx";
            public AsEm9030_EmSpawnEx(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_Dance : AsEm9030_AnimState
        {
            public override byte StateId => (byte)As.Dance;
            public override string StateName => "AsEm9030_Dance";
            protected override string AnimationName => "Idle";
            protected override bool Loop => true;
            protected override float FallbackDuration => 1.5f;
            public AsEm9030_Dance(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_DanceDown : AsEm9030_AnimState
        {
            public override byte StateId => (byte)As.DanceDown;
            public override string StateName => "AsEm9030_DanceDown";
            protected override string AnimationName => "FakeDeath_Loop";
            protected override bool Loop => true;
            protected override float FallbackDuration => 1.5f;
            public AsEm9030_DanceDown(EnemyContext context) : base(context) { }
        }

        public class AsEm9030_KnockOut : AsEmBoss_KnockOut
        {
            public override byte StateId => (byte)As.KnockOut;
            public override string StateName => "AsEm9030_KnockOut";
            private float timer;
            private bool loopPlayed;
            private bool endPlayed;
            private float deadHideTimer;
            protected MvEm9030 Boss => Em as MvEm9030;

            public AsEm9030_KnockOut(EnemyContext context) : base(context) { }

            public override void Enter()
            {
                base.Enter();
                timer = 0f;
                deadHideTimer = 0f;
                loopPlayed = false;
                endPlayed = false;
                Boss.StartFallingFromAirborne();
                Boss.PlayAnimation("KnockOut_Start", false);
            }

            public override void Tick()
            {
                timer += Time.deltaTime;
                if (Boss.IsAirborneActive && Boss.HasReachedLandingHeight())
                    Boss.FinishAirborne();

                if (!loopPlayed && (Boss.IsAnimationFinished("KnockOut_Start") || timer >= Boss.GetAnimationDuration("KnockOut_Start", 0.5f)))
                {
                    loopPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation("KnockOut_Loop", true);
                }

                if (Boss.CurrentHealth <= 0f)
                {
                    deadHideTimer += Time.deltaTime;
                    Boss.StopHorizontalMotion();
                    if (deadHideTimer >= 2f)
                        Boss.gameObject.SetActive(false);
                    return;
                }

                if (loopPlayed && !endPlayed && timer >= 3f)
                {
                    endPlayed = true;
                    timer = 0f;
                    Boss.PlayAnimation("KnockOut_End", false);
                }
                else if (endPlayed && (Boss.IsAnimationFinished("KnockOut_End") || timer >= Boss.GetAnimationDuration("KnockOut_End", 0.8f)))
                {
                    if (Boss.IsAirborneActive && !Boss.HasReachedLandingHeight())
                        Boss.ChangeEnemyState((byte)As.Fall);
                    else
                        Boss.ReqNextComboAction();
                }
            }
        }
    }
}
