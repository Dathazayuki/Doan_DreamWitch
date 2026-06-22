using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DreamKnight.Interfaces;
using DreamKnight.Player;
using DreamKnight.Systems.Combat;
using Spine;
using Spine.Unity;

namespace Mv
{
    [DisallowMultipleComponent]
    public partial class MvEm9020 : MvEnemyBase
    {
        // ──────────────────────────────────────────────────────────────
        //  State IDs của Boss Golem (Em9020)
        // ──────────────────────────────────────────────────────────────
        public enum As : byte
        {
            Entrance = (byte)AsCommon.Max, // 17
            HandL_AtkD,                    // 18
            HandR_AtkD,                    // 19
            HandL_Slap,                    // 20
            HandR_Slap,                    // 21
            HandLR_AtkD,                   // 22
            HandLR_AtkD_SpawnEm,           // 23
            LaserCW,                       // 24
            LaserCCW,                      // 25
            LaserSuperR,                   // 26
            LaserSuperL,                   // 27
            KnockOut,                      // 28
            StepL,                         // 29
            StepR,                         // 30
            Hide,                          // 31
            HideLong,                      // 32
            HideEnd,                       // 33
            Max
        }

        // ──────────────────────────────────────────────────────────────
        //  AI Desire Configuration
        // ──────────────────────────────────────────────────────────────
        [Header("AI Desire Weights")]
        public int Desire_Add_Atk = 40;
        public int Desire_Add_AtkSuper = 15;
        public int Desire_Add_Laser = 20;
        public int Desire_Add_LaserSuper = 5;
        public int Desire_Add_Wander = 10;
        public int Desire_Add_Step = 10;
        public int Desire_Add_LaserSuper_Hp50Per = 10;
        public int Desire_Add_LaserSuper_Hp10Per = 20;
        public int Desire_Add_SpawnEm_HpTh = 15;

        // ──────────────────────────────────────────────────────────────
        //  Timings & Distances
        // ──────────────────────────────────────────────────────────────
        [Header("Golem Timings & Settings")]
        public float Entrance_LookTime = 2.0f;
        public float Step_Dist = 5.0f;
        public AnimationCurve Step_Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float SmoothTime = 0.2f;
        public float HandleArmWaitTime = 1.0f;
        public float HandleOutSideTh = 7.0f;
        public float HandAtkD_MoveSpeed = 12f;
        public float HandAtkD_ReturnSpeed = 18f;
        public float HandAtkD_XTolerance = 0.1f;
        public float Hand1_RockFall_HpRate = 0.5f;
        public float HideTime = 3.0f;
        public float HideSpawnEmTime = 5.0f;
        public float HideNameTime = 1.0f;
        public float EnvFade_WaitTime = 1.0f;
        public float EnvFade_ItpTime = 1.0f;
        public float[] KnockOut_DmgVal = new float[] { 100f, 150f, 200f };
        public float KnockOut_DmgVal_SuperLaser = 300f;
        public int[] KnockOut_LoopNum = new int[] { 3, 2, 1 };
        public float Laser_Interval = 1f;
        public float Laser_Deg = 45f;
        public float Laser_LoopTime = 2.0f;
        public float Laser_FxMuzzle_Deg = 0f;
        public float LaserSuper_OfsX = 8f;
        public float LaserSuper_LoopTime_Charge = 1.5f;
        public float LaserSuper_LoopTime_Shot = 3.0f;
        public Vector3 CamLookTgt_Ofs = new Vector3(0f, 2f, 0f);
        private const float CeilingPosY = 16f;

        [Header("Death Blink")]
        [SerializeField] private float deathBlinkDuration = 1.2f;
        [SerializeField] private float deathBlinkInterval = 0.08f;

        // ──────────────────────────────────────────────────────────────
        //  Prefabs & Transforms
        // ──────────────────────────────────────────────────────────────
        [Header("VFX & Spawn Prefabs")]
        [SerializeField] private GameObject rockFallPrefab;
        [SerializeField] private GameObject spawnEmPrefab; // MiniGolem
        [SerializeField] private GameObject laserNormalPrefab;
        [SerializeField] private GameObject laserSuperPrefab;

        public GameObject LaserNormalPrefab => laserNormalPrefab;
        public GameObject LaserSuperPrefab => laserSuperPrefab;

        [Header("Bone References")]
        [SerializeField] private Transform _UtilBone_Handle_ArmL;
        [SerializeField] private Transform _UtilBone_Handle_ArmR;
        [SerializeField] private Transform _Bf_Handle_Eye;
        [SerializeField] private Transform _Bf_HandL;
        [SerializeField] private Transform _Bf_HandR;
        [SerializeField] private Transform _Bf_FxL;
        [SerializeField] private Transform _Bf_FxC;
        [SerializeField] private Transform _Bf_FxR;

        [Header("Attack Assets")]
        [SerializeField] private MvAttack _Atk_HandL_AtkD;
        [SerializeField] private MvAttack _Atk_HandR_AtkD;
        [SerializeField] private MvAttack _Atk_HandL_Slap;
        [SerializeField] private MvAttack _Atk_HandR_Slap;
        [SerializeField] private MvAttack _Atk_HandLR_AtkD;

        [Header("Passive/Always Attack Assets")]
        [SerializeField] private MvAttack _AtkAlways_HandR;
        [SerializeField] private MvAttack _AtkAlways_HandL;
        [SerializeField] private MvAttack _AtkAlways_HandR_AtkD;
        [SerializeField] private MvAttack _AtkAlways_HandL_AtkD;
        [SerializeField] private MvAttack _AtkAlways_HandR_Slap;
        [SerializeField] private MvAttack _AtkAlways_HandL_Slap;
        [SerializeField] private MvAttack _AtkAlways_HandLR_AtkD_L;
        [SerializeField] private MvAttack _AtkAlways_HandLR_AtkD_R;
        [SerializeField] private MvAttack _AtkAlways_LaserSuperHand;

        // ──────────────────────────────────────────────────────────────
        //  AI Brain & Properties
        // ──────────────────────────────────────────────────────────────
        public MvEmBrain_Em9020 _Brain;
        private bool _hasPlayedEntrance = false;
        private bool _ko70Triggered = false;
        private bool _ko40Triggered = false;
        private bool _ko10Triggered = false;
        private Vector3 _spawnPos;
        public Vector3 SpawnPos => _spawnPos;

        // Spine Detection
        private Animator _myAnimator;
        private SkeletonAnimation _skeletonAnimation;
        private Renderer[] deathBlinkRenderers;
        private bool deathBlinkStarted;
        private Coroutine deathBlinkCoroutine;

        // Physics
        public Rigidbody2D _myRb;

        // Shadow property to expose target to brain classes
        public new Transform CurrentTarget => base.CurrentTarget;

        // ──────────────────────────────────────────────────────────────
        //  Lifecycle
        // ──────────────────────────────────────────────────────────────
        protected override void Awake()
        {
            _spawnPos = transform.position;
            base.Awake();
            _myRb = GetComponent<Rigidbody2D>();
            DisableRetreat();
            DisablePatrol();
            DisableKnockback();
            InitSpine();
            CacheDeathBlinkRenderers();
            ConfigureAttackFallbacks();
            _Brain = new MvEmBrain_Em9020();
            _Brain.Setup(this);
        }

        private void ConfigureAttackFallbacks()
        {
            MvAttack[] attacks = GetComponentsInChildren<MvAttack>(true);
            foreach (var atk in attacks)
            {
                if (atk != null)
                {
                    atk.SetAllowAutoHitFallback(true);
                    atk.SetAutoHitDelay(0.05f);
                }
            }
        }

        private void CacheDeathBlinkRenderers()
        {
            deathBlinkRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public void StartDeathBlinkAndHide()
        {
            if (deathBlinkStarted)
                return;

            deathBlinkStarted = true;
            if (deathBlinkCoroutine != null)
                StopCoroutine(deathBlinkCoroutine);

            deathBlinkCoroutine = StartCoroutine(DeathBlinkAndHideRoutine());
        }

        private IEnumerator DeathBlinkAndHideRoutine()
        {
            if (deathBlinkRenderers == null || deathBlinkRenderers.Length == 0)
                CacheDeathBlinkRenderers();

            float duration = Mathf.Max(0f, deathBlinkDuration);
            float interval = Mathf.Max(0.01f, deathBlinkInterval);
            float timer = 0f;
            bool visible = true;

            while (timer < duration)
            {
                visible = !visible;
                SetDeathBlinkVisible(visible);
                yield return new WaitForSeconds(interval);
                timer += interval;
            }

            SetDeathBlinkVisible(false);
            gameObject.SetActive(false);
        }

        private void SetDeathBlinkVisible(bool visible)
        {
            if (deathBlinkRenderers == null)
                return;

            for (int i = 0; i < deathBlinkRenderers.Length; i++)
            {
                if (deathBlinkRenderers[i] != null)
                    deathBlinkRenderers[i].enabled = visible;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (IsAlive && CurrentState != null && (CurrentState.StateId == IdleStateId || CurrentState.StateId == AtkAfterStateId))
            {
                if (!_hasPlayedEntrance)
                {
                    _hasPlayedEntrance = true;
                    ChangeEnemyState((byte)As.Entrance);
                    return;
                }

                if (_Brain != null && _Brain.IsComboFinished)
                {
                    _Brain.ReqCombo();
                }
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            // Force horizontal velocity to zero if not stepping/teleporting,
            // preventing any residual physics slide or push back.
            if (IsAlive && _myRb != null && CurrentState != null)
            {
                byte stateId = CurrentState.StateId;
                if (stateId != (byte)As.StepL && stateId != (byte)As.StepR)
                {
                    _myRb.linearVelocity = new Vector2(0f, _myRb.linearVelocity.y);
                }
            }
        }

        private Vector3 ResolveGolemDamageTextPosition()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                return col.bounds.center;

            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                return sr.bounds.center;

            return transform.position;
        }

        protected override bool CanFlip => false;

        // We override TakeDamage to bypass the normal Hit/Knockback state transition,
        // since Boss Golem is immune to hitstun/stagger and has no Hit animation.
        public override void TakeDamage(float damage, GameObject damageSource = null)
        {
            TakeDamage(damage, damageSource, null);
        }

        public override void TakeDamage(float damage, GameObject damageSource = null, Vector3? damageTextWorldPosition = null)
        {
            if (!IsAlive) return;

            // Deduct health
            float prevHpRate = CurrentHealth / MaxHealth;

            var healthField = typeof(MvEnemyBase).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (healthField != null)
            {
                float newHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, damage));
                healthField.SetValue(this, newHealth);
            }

            var eventField = typeof(MvEnemyBase).GetField("OnHealthChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (eventField != null)
            {
                var onHealthChanged = eventField.GetValue(this) as Action<float, float>;
                onHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            }

            // Show damage text
            DamageTextService.ShowEnemyDamage(damage, damageTextWorldPosition ?? ResolveGolemDamageTextPosition());

            float currentHpRate = CurrentHealth / MaxHealth;

            // HP trigger threshold calls
            if (prevHpRate >= 0.5f && currentHpRate < 0.5f)
            {
                _Brain?.TrgHp_50Per();
            }
            if (prevHpRate >= 0.3f && currentHpRate < 0.3f)
            {
                _Brain?.TrgHp_30Per();
            }
            if (prevHpRate >= 0.1f && currentHpRate < 0.1f)
            {
                _Brain?.TrgHp_10Per();
            }

            // --- KO thresholds (every 30% drop) ---
            if (currentHpRate <= 0.1f && !_ko10Triggered)
            {
                _ko70Triggered = true;
                _ko40Triggered = true;
                _ko10Triggered = true;
                if (!IsKnockedOut)
                    _Brain?.TriggerKnockOut();
            }
            else if (currentHpRate <= 0.4f && !_ko40Triggered)
            {
                _ko70Triggered = true;
                _ko40Triggered = true;
                if (!IsKnockedOut)
                    _Brain?.TriggerKnockOut();
            }
            else if (currentHpRate <= 0.7f && !_ko70Triggered)
            {
                _ko70Triggered = true;
                if (!IsKnockedOut)
                    _Brain?.TriggerKnockOut();
            }

            if (CurrentHealth <= 0f)
            {
                var deadField = typeof(MvEnemyBase).GetField("isDead", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (deadField != null)
                {
                    deadField.SetValue(this, true);
                }

                Die();
                return;
            }
        }

        public bool IsKnockedOut => CurrentState != null && CurrentState.StateId == (byte)As.KnockOut;

        protected override EnemyState CreateDeadState(EnemyContext context)
        {
            return new AsEm9020_KnockOut(context);
        }

        public void ReqNextComboAction()
        {
            if (_Brain != null)
            {
                _Brain.ReqCombo();
            }
            else
            {
                ChangeEnemyState(IdleStateId);
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Mob Behavior Disabler (Forces Golem to stay in Idle/Combo loop)
        // ──────────────────────────────────────────────────────────────
        protected override bool TryEvaluateCustomAttackRange(float absX, float absY, float edgeDistanceX, out bool inAttackRange)
        {
            // Forces MvEnemyBase to believe Golem is always in attack range,
            // preventing it from triggering RunState.
            inAttackRange = true;
            return true;
        }

        private void DisableRetreat()
        {
            var field = typeof(MvEnemyBase).GetField("retreatDuringAttackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(this, false);
            }
        }

        private void DisablePatrol()
        {
            var field = typeof(MvEnemyBase).GetField("usePatrol", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(this, false);
            }
        }

        private void DisableKnockback()
        {
            var field = typeof(MvEnemyBase).GetField("knockbackForceX", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(this, 0f);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // ──────────────────────────────────────────────────────────────
        //  Spine Playback Helpers
        // ──────────────────────────────────────────────────────────────
        private void InitSpine()
        {
            _myAnimator = GetComponentInChildren<Animator>();
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
            if (_skeletonAnimation != null)
            {
                _skeletonAnimation.AnimationState.Event += OnSpineEvent;
            }
        }

        private void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
        {
            if (e != null && e.Data != null)
            {
                if (ActiveAttack != null)
                {
                    ActiveAttack.OnMvAnimEvent(e.Data.Name, null);
                }

                if (CurrentState is AsEm9020_HandLR_AtkD_Base handLRState
                    && trackEntry != null
                    && trackEntry.Animation != null
                    && trackEntry.Animation.Name == "HandLR_Attack_D")
                {
                    handLRState.OnSpineEvent(e.Data.Name);
                }
            }
        }

        private void OnDestroy()
        {
            if (_skeletonAnimation != null)
            {
                _skeletonAnimation.AnimationState.Event -= OnSpineEvent;
            }
        }

        public void PlayAnimation(string animName, bool loop = false)
        {
            if (_myAnimator != null)
            {
                _myAnimator.Play(animName);
            }
            else if (_skeletonAnimation != null)
            {
                try
                {
                    _skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error playing Spine animation: " + e.Message);
                }
            }
        }

        private bool IsAnimationFinished(string animName)
        {
            if (_skeletonAnimation != null)
            {
                TrackEntry track = _skeletonAnimation.AnimationState.GetCurrent(0);
                if (track == null)
                    return false;

                if (!string.IsNullOrWhiteSpace(animName) && track.Animation != null && track.Animation.Name != animName)
                    return false;

                return track.IsComplete;
            }

            if (_myAnimator != null)
            {
                AnimatorStateInfo info = _myAnimator.GetCurrentAnimatorStateInfo(0);
                if (!string.IsNullOrWhiteSpace(animName) && !info.IsName(animName))
                    return false;

                if (info.loop)
                    return false;

                return info.normalizedTime >= 1f;
            }

            return false;
        }

        private float GetAnimationDuration(string animName, float fallback)
        {
            if (_skeletonAnimation != null && _skeletonAnimation.Skeleton != null && !string.IsNullOrWhiteSpace(animName))
            {
                Spine.Animation animation = _skeletonAnimation.Skeleton.Data.FindAnimation(animName);
                if (animation != null && animation.Duration > 0f)
                    return animation.Duration;
            }

            if (_myAnimator != null && !string.IsNullOrWhiteSpace(animName))
            {
                AnimatorStateInfo info = _myAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(animName) && info.length > 0f)
                    return info.length;
            }

            return Mathf.Max(0.01f, fallback);
        }

        // ──────────────────────────────────────────────────────────────
        //  Custom Spawning Methods
        // ──────────────────────────────────────────────────────────────
        public void SpawnRockFall()
        {
            if (rockFallPrefab != null && CurrentTarget != null)
            {
                Vector3 spawnPos = new Vector3(CurrentTarget.position.x, transform.position.y + CeilingPosY, 0f);
                Instantiate(rockFallPrefab, spawnPos, Quaternion.identity);
            }
        }

        public void SpawnMiniGolems()
        {
            if (spawnEmPrefab != null)
            {
                if (_Bf_FxL != null) Instantiate(spawnEmPrefab, _Bf_FxL.position, Quaternion.identity);
                if (_Bf_FxR != null) Instantiate(spawnEmPrefab, _Bf_FxR.position, Quaternion.identity);
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  State Registration
        // ──────────────────────────────────────────────────────────────
        protected override void RegisterAdditionalStates(EnemyStateMachine stateMachine, EnemyContext context)
        {
            stateMachine.Register(new AsEm9020_Entrance(context));
            stateMachine.Register(new AsEm9020_HandL_AtkD(context));
            stateMachine.Register(new AsEm9020_HandR_AtkD(context));
            stateMachine.Register(new AsEm9020_HandL_Slap(context));
            stateMachine.Register(new AsEm9020_HandR_Slap(context));
            stateMachine.Register(new AsEm9020_HandLR_AtkD_RockFall(context));
            stateMachine.Register(new AsEm9020_HandLR_AtkD_SpawnEm(context));
            stateMachine.Register(new AsEm9020_LaserCW(context));
            stateMachine.Register(new AsEm9020_LaserCCW(context));
            stateMachine.Register(new AsEm9020_LaserSuperR(context));
            stateMachine.Register(new AsEm9020_LaserSuperL(context));
            stateMachine.Register(new AsEm9020_KnockOut(context));
            stateMachine.Register(new AsEm9020_StepL(context));
            stateMachine.Register(new AsEm9020_StepR(context));
            stateMachine.Register(new AsEm9020_Hide(context));
            stateMachine.Register(new AsEm9020_HideLong(context));
            stateMachine.Register(new AsEm9020_HideEnd(context));
        }

        // ──────────────────────────────────────────────────────────────
        //  Nested State Classes
        // ──────────────────────────────────────────────────────────────

    }
}
