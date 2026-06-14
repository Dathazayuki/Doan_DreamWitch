using System;
using System.Collections;
using System.Collections.Generic;
using DreamKnight.Systems.Combat;
using DreamKnight.Player;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public partial class MvEm9030 : MvEnemyBase
    {
        private static readonly System.Reflection.FieldInfo MvAttackDamageField =
            typeof(MvAttack).GetField("damage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public enum As : byte
        {
            Entrance = (byte)AsCommon.Max,
            Wait,
            WaitEx,
            Jump,
            JumpMini,
            JumpMiniLong,
            JumpSide,
            FallLand,
            Fall,
            ApproachMelee,
            Atk1,
            Atk2,
            Atk3,
            SlashV,
            SlashH,
            Provoke,
            Dodge,
            Counter,
            MagicShotSpritShot,
            MagicShotFlySwordBig,
            MagicShotAirSpritShot,
            MagicShotSideLaser,
            MagicShotSideEruption,
            Scythe,
            EmSpawn,
            EmSpawnEx,
            Dance,
            DanceDown,
            KnockOut,
            Max
        }

        [Header("Config")]
        [SerializeField] private MvSo_Em9030 config;

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

        [Header("Handles")]
        [SerializeField] private Transform _Handle_Magic;
        [SerializeField] private Transform _Handle_DustShockWave;

        [Header("Melee Attacks")]
        [SerializeField] private MvAttack _Atk_Atk1;
        [SerializeField] private MvAttack _Atk_Atk2;
        [SerializeField] private MvAttack _Atk_Atk3;
        [SerializeField] private MvAttack _Atk_SlashV;
        [SerializeField] private MvAttack _Atk_SlashH;
        [SerializeField] private MvAttack _Atk_Counter;

        [Header("Melee VFX Prefabs")]
        [SerializeField] private GameObject vfxAtk1;
        [SerializeField] private GameObject vfxAtk2;
        [SerializeField] private GameObject vfxAtk3;
        [SerializeField] private GameObject vfxSlashV;
        [SerializeField] private GameObject vfxSlashH;
        [SerializeField] private Transform meleeVfxSpawnPoint;

        [Header("Shot Prefabs")]
        [SerializeField] private GameObject spritShotPrefab;
        [SerializeField] private GameObject airSpritShotPrefab;
        [SerializeField] private GameObject flySwordBigPrefab;
        [SerializeField] private GameObject laserPrefab;
        [SerializeField] private GameObject eruptionPrefab;
        [SerializeField] private GameObject scythePrefab;
        [SerializeField] private GameObject emSpawnPrefab;

        [Header("Local Shot Pool")]
        [SerializeField] private bool useLocalShotPool = true;
        [SerializeField] private int defaultShotPoolMaxSize = 32;
        [SerializeField] private float defaultShotAutoReleaseTime = 5f;
        [SerializeField] private ShotPoolSetting[] localShotPools;

        private MvEmBrain_Em9030 _Brain;
        private Animator _myAnimator;
        private SkeletonAnimation _skeletonAnimation;
        private Rigidbody2D _myRb;
        private bool _hasPlayedEntrance;
        private bool _ko70Triggered;
        private bool _ko40Triggered;
        private bool _ko10Triggered;
        private Vector3 _spawnPos;
        private float _defaultGravityScale;
        private float _landingHeightY;
        private bool _airborneActive;
        private readonly RaycastHit2D[] _counterMovementHits = new RaycastHit2D[8];
        private readonly Dictionary<GameObject, LocalShotPool> localShotPoolByPrefab = new Dictionary<GameObject, LocalShotPool>();
        private readonly Dictionary<GameObject, LocalShotPool> localShotPoolByInstance = new Dictionary<GameObject, LocalShotPool>();

        public new Transform CurrentTarget => base.CurrentTarget;
        public Vector3 SpawnPos => _spawnPos;

        protected override bool CanFlip => true;

        protected override void Awake()
        {
            _spawnPos = transform.position;
            base.Awake();
            _myRb = GetComponent<Rigidbody2D>();
            if (_myRb != null)
                _defaultGravityScale = _myRb.gravityScale;
            ApplyConfig();
            DisableRetreat();
            DisablePatrol();
            DisableKnockback();
            InitSpine();
            ConfigureAttackFallbacks();
            ConfigureAttackDamages();
            RegisterMeleeVfxWindowEvents();
            InitializeLocalShotPools();
            _Brain = new MvEmBrain_Em9030();
            _Brain.Setup(this);
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
                    _Brain.ReqCombo();
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsAlive && _myRb != null && CurrentState != null && !IsMotionState(CurrentState.StateId))
                _myRb.linearVelocity = new Vector2(0f, _myRb.linearVelocity.y);
        }

        public override void TakeDamage(float damage, GameObject damageSource = null)
        {
            TakeDamage(damage, damageSource, null);
        }

        public override void TakeDamage(float damage, GameObject damageSource = null, Vector3? damageTextWorldPosition = null)
        {
            if (!IsAlive) return;

            float prevHpRate = CurrentHealth / MaxHealth;
            SetBaseHealth(Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, damage)));
            RaiseBaseHealthChanged();
            DamageTextService.ShowEnemyDamage(damage, damageTextWorldPosition ?? ResolveDamageTextPosition());

            float hpRate = CurrentHealth / MaxHealth;
            if (prevHpRate >= 0.5f && hpRate < 0.5f) _Brain?.TrgHp_50Per();
            if (prevHpRate >= 0.3f && hpRate < 0.3f) _Brain?.TrgHp_30Per();
            if (prevHpRate >= 0.1f && hpRate < 0.1f) _Brain?.TrgHp_10Per();

            if (hpRate <= 0.1f && !_ko10Triggered)
            {
                _ko70Triggered = true;
                _ko40Triggered = true;
                _ko10Triggered = true;
                if (!IsKnockedOut) _Brain?.TriggerKnockOut();
            }
            else if (hpRate <= 0.4f && !_ko40Triggered)
            {
                _ko70Triggered = true;
                _ko40Triggered = true;
                if (!IsKnockedOut) _Brain?.TriggerKnockOut();
            }
            else if (hpRate <= 0.7f && !_ko70Triggered)
            {
                _ko70Triggered = true;
                if (!IsKnockedOut) _Brain?.TriggerKnockOut();
            }

            if (CurrentHealth <= 0f)
                Die();
        }

        public bool IsKnockedOut => CurrentState != null && CurrentState.StateId == (byte)As.KnockOut;

        protected override EnemyState CreateDeadState(EnemyContext context)
        {
            return new AsEm9030_KnockOut(context);
        }

        protected override bool TryEvaluateCustomAttackRange(float absX, float absY, float edgeDistanceX, out bool inAttackRange)
        {
            inAttackRange = edgeDistanceX <= Mathf.Max(0.1f, MeleeApproachStopDistance)
                && absY <= Mathf.Max(0f, MeleeApproachVerticalTolerance);
            return true;
        }

        protected override void RegisterAdditionalStates(EnemyStateMachine stateMachine, EnemyContext context)
        {
            stateMachine.Register(new AsEm9030_Entrance(context));
            stateMachine.Register(new AsEm9030_Wait(context));
            stateMachine.Register(new AsEm9030_WaitEx(context));
            stateMachine.Register(new AsEm9030_Jump(context));
            stateMachine.Register(new AsEm9030_JumpMini(context));
            stateMachine.Register(new AsEm9030_JumpMiniLong(context));
            stateMachine.Register(new AsEm9030_JumpSide(context));
            stateMachine.Register(new AsEm9030_FallLand(context));
            stateMachine.Register(new AsEm9030_Fall(context));
            stateMachine.Register(new AsEm9030_ApproachMelee(context));
            stateMachine.Register(new AsEm9030_Atk1(context));
            stateMachine.Register(new AsEm9030_Atk2(context));
            stateMachine.Register(new AsEm9030_Atk3(context));
            stateMachine.Register(new AsEm9030_SlashV(context));
            stateMachine.Register(new AsEm9030_SlashH(context));
            stateMachine.Register(new AsEm9030_Provoke(context));
            stateMachine.Register(new AsEm9030_Dodge(context));
            stateMachine.Register(new AsEm9030_Counter(context));
            stateMachine.Register(new AsEm9030_MagicShotSpritShot(context));
            stateMachine.Register(new AsEm9030_MagicShotFlySwordBig(context));
            stateMachine.Register(new AsEm9030_MagicShotAirSpritShot(context));
            stateMachine.Register(new AsEm9030_MagicShotSideLaser(context));
            stateMachine.Register(new AsEm9030_MagicShotSideEruption(context));
            stateMachine.Register(new AsEm9030_Scythe(context));
            stateMachine.Register(new AsEm9030_EmSpawn(context));
            stateMachine.Register(new AsEm9030_EmSpawnEx(context));
            stateMachine.Register(new AsEm9030_Dance(context));
            stateMachine.Register(new AsEm9030_DanceDown(context));
            stateMachine.Register(new AsEm9030_KnockOut(context));
        }

        public void ReqNextComboAction()
        {
            if (_Brain != null)
                _Brain.ReqCombo();
            else
                ChangeEnemyState(IdleStateId);
        }

        public void PlayAnimation(string animName, bool loop = false)
        {
            if (_myAnimator != null)
            {
                _myAnimator.Play(animName);
                return;
            }

            if (_skeletonAnimation == null || string.IsNullOrWhiteSpace(animName))
                return;

            try
            {
                _skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
            }
            catch (Exception e)
            {
                Debug.LogError("Error playing Spine animation: " + e.Message);
            }
        }

        public bool IsAnimationFinished(string animName)
        {
            if (_skeletonAnimation != null)
            {
                TrackEntry track = _skeletonAnimation.AnimationState.GetCurrent(0);
                if (track == null) return false;
                if (!string.IsNullOrWhiteSpace(animName) && track.Animation != null && track.Animation.Name != animName)
                    return false;
                return track.IsComplete;
            }

            if (_myAnimator != null)
            {
                AnimatorStateInfo info = _myAnimator.GetCurrentAnimatorStateInfo(0);
                if (!string.IsNullOrWhiteSpace(animName) && !info.IsName(animName))
                    return false;
                return !info.loop && info.normalizedTime >= 1f;
            }

            return false;
        }

        public float GetAnimationDuration(string animName, float fallback)
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

        public GameObject SpawnShot(GameObject prefab, Vector3 pos, Vector2 dir, float damage)
        {
            if (prefab == null)
                return null;

            Quaternion rot = dir.sqrMagnitude > 0.0001f
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg)
                : Quaternion.identity;

            if (useLocalShotPool)
            {
                GameObject shotObject = SpawnLocalShot(prefab, pos, rot);
                if (shotObject == null)
                    return null;

                IEm9030Projectile projectile = shotObject.GetComponent<IEm9030Projectile>();
                if (projectile != null)
                {
                    Vector3 targetPosition = CurrentTarget != null ? CurrentTarget.position : pos + (Vector3)dir;
                    projectile.Initialize(this, gameObject, damage, dir, targetPosition);
                }

                return shotObject;
            }

            return Instantiate(prefab, pos, rot);
        }

        private void InitializeLocalShotPools()
        {
            if (!useLocalShotPool)
                return;

            localShotPoolByPrefab.Clear();
            localShotPoolByInstance.Clear();

            RegisterLocalShotPool(spritShotPrefab);
            RegisterLocalShotPool(airSpritShotPrefab);
            RegisterLocalShotPool(flySwordBigPrefab);
            RegisterLocalShotPool(laserPrefab);
            RegisterLocalShotPool(eruptionPrefab);
            RegisterLocalShotPool(scythePrefab);

            if (localShotPools == null)
                return;

            for (int i = 0; i < localShotPools.Length; i++)
            {
                ShotPoolSetting setting = localShotPools[i];
                if (setting == null || setting.Prefab == null)
                    continue;

                LocalShotPool pool = RegisterLocalShotPool(setting.Prefab, setting);
                PrewarmLocalShotPool(pool, setting.PrewarmCount);
            }
        }

        private LocalShotPool RegisterLocalShotPool(GameObject prefab, ShotPoolSetting setting = null)
        {
            if (prefab == null)
                return null;

            if (localShotPoolByPrefab.TryGetValue(prefab, out LocalShotPool existing))
            {
                if (setting != null)
                    existing.Apply(setting, defaultShotPoolMaxSize, defaultShotAutoReleaseTime);
                return existing;
            }

            LocalShotPool pool = new LocalShotPool(prefab, transform, setting, defaultShotPoolMaxSize, defaultShotAutoReleaseTime);
            localShotPoolByPrefab[prefab] = pool;
            return pool;
        }

        private void PrewarmLocalShotPool(LocalShotPool pool, int count)
        {
            if (pool == null || count <= 0)
                return;

            int target = Mathf.Min(count, pool.MaxPoolSize);
            while (pool.TotalCount < target)
            {
                GameObject instance = CreateLocalShotInstance(pool);
                if (instance == null)
                    return;

                ReleaseLocalShot(instance);
            }
        }

        private GameObject SpawnLocalShot(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            LocalShotPool pool = RegisterLocalShotPool(prefab);
            if (pool == null)
                return null;

            GameObject instance = null;
            while (pool.Inactive.Count > 0 && instance == null)
                instance = pool.Inactive.Dequeue();

            if (instance == null)
                instance = CreateLocalShotInstance(pool);

            if (instance == null)
                return null;

            instance.transform.SetParent(null, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            Em9030LocalShotAutoRelease autoRelease = instance.GetComponent<Em9030LocalShotAutoRelease>();
            if (autoRelease == null)
                autoRelease = instance.AddComponent<Em9030LocalShotAutoRelease>();
            autoRelease.Initialize(this, pool.AutoReleaseTime);

            return instance;
        }

        private GameObject CreateLocalShotInstance(LocalShotPool pool)
        {
            if (pool == null || pool.Prefab == null)
                return null;

            GameObject instance = Instantiate(pool.Prefab);
            pool.TotalCount++;
            localShotPoolByInstance[instance] = pool;
            return instance;
        }

        internal void ReleaseLocalShot(GameObject instance)
        {
            if (instance == null)
                return;

            if (!localShotPoolByInstance.TryGetValue(instance, out LocalShotPool pool) || pool == null)
            {
                Destroy(instance);
                return;
            }

            Em9030LocalShotAutoRelease autoRelease = instance.GetComponent<Em9030LocalShotAutoRelease>();
            if (autoRelease != null)
                autoRelease.StopTimer();

            if (pool.Inactive.Count >= pool.MaxPoolSize)
            {
                localShotPoolByInstance.Remove(instance);
                pool.TotalCount = Mathf.Max(0, pool.TotalCount - 1);
                Destroy(instance);
                return;
            }

            instance.transform.SetParent(pool.Root, false);
            instance.SetActive(false);
            pool.Inactive.Enqueue(instance);
        }

        private void ClearLocalShotPools()
        {
            foreach (KeyValuePair<GameObject, LocalShotPool> pair in localShotPoolByInstance)
            {
                if (pair.Key != null)
                    Destroy(pair.Key);
            }

            foreach (KeyValuePair<GameObject, LocalShotPool> pair in localShotPoolByPrefab)
            {
                LocalShotPool pool = pair.Value;
                if (pool == null || pool.Root == null)
                    continue;

                Destroy(pool.Root.gameObject);
            }

            localShotPoolByPrefab.Clear();
            localShotPoolByInstance.Clear();
        }

        public Vector3 MagicHandlePosition => _Handle_Magic != null ? _Handle_Magic.position : transform.position;
        public Vector3 ShockWaveHandlePosition => _Handle_DustShockWave != null ? _Handle_DustShockWave.position : transform.position;
        public Vector2 FacingDirection => transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
        internal Rigidbody2D Body => _myRb;
        internal bool IsAirborneActive => _airborneActive;
        internal bool IsNextComboActionAirborneAttack => _Brain != null && _Brain.IsNextComboActionAirborneAttack();

        public GameObject SpritShotPrefab => spritShotPrefab;
        public GameObject AirSpritShotPrefab => airSpritShotPrefab != null ? airSpritShotPrefab : spritShotPrefab;
        public GameObject FlySwordBigPrefab => flySwordBigPrefab;
        public GameObject LaserPrefab => laserPrefab;
        public GameObject EruptionPrefab => eruptionPrefab;
        public GameObject ScythePrefab => scythePrefab;
        public GameObject EmSpawnPrefab => emSpawnPrefab;

        private bool IsMotionState(byte stateId)
        {
            return stateId == (byte)As.Jump
                || stateId == (byte)As.JumpMini
                || stateId == (byte)As.JumpMiniLong
                || stateId == (byte)As.JumpSide
                || stateId == (byte)As.Fall
                || stateId == (byte)As.ApproachMelee
                || stateId == (byte)As.SlashV
                || stateId == (byte)As.SlashH
                || stateId == (byte)As.Counter
                || stateId == (byte)As.Atk1
                || stateId == (byte)As.Atk2
                || stateId == (byte)As.Atk3;
        }

        private void InitSpine()
        {
            _myAnimator = GetComponentInChildren<Animator>();
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
            if (_skeletonAnimation != null)
                _skeletonAnimation.AnimationState.Event += OnSpineEvent;
        }

        private void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
        {
            if (e != null && e.Data != null && ActiveAttack != null)
                ActiveAttack.OnMvAnimEvent(e.Data.Name, null);
        }

        private void OnDestroy()
        {
            UnregisterMeleeVfxWindowEvents();
            if (_skeletonAnimation != null)
                _skeletonAnimation.AnimationState.Event -= OnSpineEvent;
            ClearLocalShotPools();
        }

        private void ConfigureAttackFallbacks()
        {
            MvAttack[] attacks = GetComponentsInChildren<MvAttack>(true);
            foreach (var atk in attacks)
            {
                if (atk == null) continue;
                atk.SetOwner(this);
                atk.SetAllowAutoHitFallback(false);
                atk.SetCloseAttackWindowImmediatelyOnEnd(true);
                if (atk == _Atk_Counter)
                {
                    atk.SetRequireAnimEventAtkS(false);
                    atk.SetAllowRepeatedHitsDuringWindow(true);
                    atk.SetAttackWindowTickInterval(CounterRepeatedHitInterval);
                }
            }
        }

        private void ApplyConfig()
        {
            if (config == null)
                return;

            Desire_Add_Run = config.Desire_Add_Run;
            Desire_Add_Jump = config.Desire_Add_Jump;
            Desire_Add_Atk = config.Desire_Add_Atk;
            Desire_Add_SlashVH = config.Desire_Add_SlashVH;
            Desire_Add_Provoke = config.Desire_Add_Provoke;
            Desire_Add_ShotA = config.Desire_Add_ShotA;
            Desire_Add_Scythe_Hp50 = config.Desire_Add_Scythe_Hp50;
            Desire_Add_EmSpawn_Hp30 = config.Desire_Add_EmSpawn_Hp30;

            Entrance_LookTime = config.Entrance_LookTime;
            IdleWaitTime = config.IdleWaitTime;
            IdleWaitTimeEx = config.IdleWaitTimeEx;
            Provoke_LoopTime = config.Provoke_LoopTime;
            MagicShotAir_SpritShot_LoopTime = config.MagicShotAir_SpritShot_LoopTime;
            MagicShotSide0_Laser_LoopTime = config.MagicShotSide0_Laser_LoopTime;
            MagicShotSide0_Eruption_LoopTime = config.MagicShotSide0_Eruption_LoopTime;
            Scythe_LoopTime = config.Scythe_LoopTime;
            CounterAtkTimer = config.CounterAtkTimer;
            KnockOut_LoopNum = config.KnockOut_LoopNum != null ? (int[])config.KnockOut_LoopNum.Clone() : null;
            EruptionSideShotCount = config.EruptionSideShotCount;
            EruptionSideShotSpacingY = config.EruptionSideShotSpacingY;

            JumpPow = config.JumpPow;
            JumpPowMini = config.JumpPowMini;
            JumpPowMiniLong = config.JumpPowMiniLong;
            JumpPowSide = config.JumpPowSide;
            MeleeApproachStopDistance = config.MeleeApproachStopDistance;
            MeleeApproachVerticalTolerance = config.MeleeApproachVerticalTolerance;
            MeleeApproachTimeout = config.MeleeApproachTimeout;
            CounterMoveSpeed = config.CounterMoveSpeed;
            CounterMoveSpeedCurve = config.CounterMoveSpeedCurve;
            CounterMovementBlockMask = config.CounterMovementBlockMask;
            CounterMovementProbeDistance = config.CounterMovementProbeDistance;
            CounterRepeatedHitInterval = config.CounterRepeatedHitInterval;

            AirborneGravityScale = config.AirborneGravityScale;
            LandingHeightTolerance = config.LandingHeightTolerance;
            SnapToLandingHeightOnLand = config.SnapToLandingHeightOnLand;

            AtkDmg_Atk1 = config.AtkDmg_Atk1;
            AtkDmg_Atk2 = config.AtkDmg_Atk2;
            AtkDmg_Atk3 = config.AtkDmg_Atk3;
            AtkDmg_SlashV = config.AtkDmg_SlashV;
            AtkDmg_SlashH = config.AtkDmg_SlashH;
            AtkDmg_Counter = config.AtkDmg_Counter;
            AtkDmg_SpritShot = config.AtkDmg_SpritShot;
            AtkDmg_FlySwordBig = config.AtkDmg_FlySwordBig;
            AtkDmg_Laser = config.AtkDmg_Laser;
            AtkDmg_Eruption = config.AtkDmg_Eruption;
            AtkDmg_Scythe = config.AtkDmg_Scythe;
        }

        internal void BeginAirborne()
        {
            _landingHeightY = transform.position.y;
            _airborneActive = true;

            if (_myRb != null && _myRb.gravityScale <= 0.01f)
                _myRb.gravityScale = Mathf.Max(0.01f, AirborneGravityScale);
        }

        internal void HoldAirborne()
        {
            _airborneActive = true;

            if (_myRb == null)
                return;

            _myRb.linearVelocity = Vector2.zero;
            _myRb.gravityScale = 0f;
        }

        internal void StartFallingFromAirborne()
        {
            if (_myRb == null)
                return;

            _airborneActive = true;
            if (_myRb.gravityScale <= 0.01f)
                _myRb.gravityScale = Mathf.Max(0.01f, AirborneGravityScale);
        }

        internal bool HasReachedLandingHeight()
        {
            return transform.position.y <= _landingHeightY + Mathf.Max(0f, LandingHeightTolerance);
        }

        internal void FinishAirborne()
        {
            if (_myRb != null)
            {
                _myRb.linearVelocity = Vector2.zero;
                if (_airborneActive)
                    _myRb.gravityScale = _defaultGravityScale;
            }

            if (SnapToLandingHeightOnLand)
                transform.position = new Vector3(transform.position.x, _landingHeightY, transform.position.z);

            _airborneActive = false;
        }

        internal bool IsCounterMovementBlocked(float direction)
        {
            if (_myRb == null || Mathf.Abs(direction) < 0.001f)
                return false;

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = CounterMovementBlockMask;
            filter.useTriggers = false;

            float distance = Mathf.Max(0.01f, CounterMovementProbeDistance);
            int count = _myRb.Cast(new Vector2(Mathf.Sign(direction), 0f), filter, _counterMovementHits, distance);
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _counterMovementHits[i];
                if (hit.collider == null)
                    continue;

                if (hit.collider.transform.root == transform.root)
                    continue;

                return true;
            }

            return false;
        }

        private void ConfigureAttackDamages()
        {
            ApplyAttackDamage(_Atk_Atk1, AtkDmg_Atk1);
            ApplyAttackDamage(_Atk_Atk2, AtkDmg_Atk2);
            ApplyAttackDamage(_Atk_Atk3, AtkDmg_Atk3);
            ApplyAttackDamage(_Atk_SlashV, AtkDmg_SlashV);
            ApplyAttackDamage(_Atk_SlashH, AtkDmg_SlashH);
            ApplyAttackDamage(_Atk_Counter, AtkDmg_Counter);
        }

        private void ApplyAttackDamage(MvAttack attack, float damage)
        {
            if (attack == null || MvAttackDamageField == null)
                return;

            MvAttackDamageField.SetValue(attack, Mathf.Max(0f, damage));
        }

        internal void SpawnMeleeVfx(GameObject prefab, MvAttack attack)
        {
            if (prefab == null)
                return;

            Transform spawnPoint = meleeVfxSpawnPoint != null ? meleeVfxSpawnPoint : (attack != null ? attack.transform : transform);
            VfxPoolManager.Instance?.Spawn(prefab, spawnPoint.position, spawnPoint.rotation, null);
        }

        private void RegisterMeleeVfxWindowEvents()
        {
            RegisterMeleeVfxWindowEvent(_Atk_Atk1);
            RegisterMeleeVfxWindowEvent(_Atk_Atk2);
            RegisterMeleeVfxWindowEvent(_Atk_Atk3);
            RegisterMeleeVfxWindowEvent(_Atk_SlashV);
            RegisterMeleeVfxWindowEvent(_Atk_SlashH);
        }

        private void UnregisterMeleeVfxWindowEvents()
        {
            UnregisterMeleeVfxWindowEvent(_Atk_Atk1);
            UnregisterMeleeVfxWindowEvent(_Atk_Atk2);
            UnregisterMeleeVfxWindowEvent(_Atk_Atk3);
            UnregisterMeleeVfxWindowEvent(_Atk_SlashV);
            UnregisterMeleeVfxWindowEvent(_Atk_SlashH);
        }

        private void RegisterMeleeVfxWindowEvent(MvAttack attack)
        {
            if (attack != null)
                attack.AttackWindowStarted += OnMeleeAttackWindowStarted;
        }

        private void UnregisterMeleeVfxWindowEvent(MvAttack attack)
        {
            if (attack != null)
                attack.AttackWindowStarted -= OnMeleeAttackWindowStarted;
        }

        private void OnMeleeAttackWindowStarted(MvAttack attack)
        {
            SpawnMeleeVfx(ResolveMeleeVfxPrefab(attack), attack);
        }

        private GameObject ResolveMeleeVfxPrefab(MvAttack attack)
        {
            if (attack == _Atk_Atk1) return vfxAtk1;
            if (attack == _Atk_Atk2) return vfxAtk2;
            if (attack == _Atk_Atk3) return vfxAtk3;
            if (attack == _Atk_SlashV) return vfxSlashV;
            if (attack == _Atk_SlashH) return vfxSlashH;
            return null;
        }

        private Vector3 ResolveDamageTextPosition()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) return col.bounds.center;
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            return sr != null ? sr.bounds.center : transform.position;
        }

        private void SetBaseHealth(float value)
        {
            var field = typeof(MvEnemyBase).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(this, value);
        }

        private void RaiseBaseHealthChanged()
        {
            var eventField = typeof(MvEnemyBase).GetField("OnHealthChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var onHealthChanged = eventField?.GetValue(this) as Action<float, float>;
            onHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void DisableRetreat()
        {
            var field = typeof(MvEnemyBase).GetField("retreatDuringAttackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(this, false);
        }

        private void DisablePatrol()
        {
            var field = typeof(MvEnemyBase).GetField("usePatrol", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(this, false);
        }

        private void DisableKnockback()
        {
            var field = typeof(MvEnemyBase).GetField("knockbackForceX", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(this, 0f);
        }

        internal MvAttack Atk_Atk1 => _Atk_Atk1;
        internal MvAttack Atk_Atk2 => _Atk_Atk2;
        internal MvAttack Atk_Atk3 => _Atk_Atk3;
        internal MvAttack Atk_SlashV => _Atk_SlashV;
        internal MvAttack Atk_SlashH => _Atk_SlashH;
        internal MvAttack Atk_Counter => _Atk_Counter;

        [Serializable]
        private class ShotPoolSetting
        {
            [SerializeField] private GameObject prefab;
            [SerializeField] private int prewarmCount = 0;
            [SerializeField] private int maxPoolSize = 32;
            [SerializeField] private float autoReleaseTime = 5f;

            public GameObject Prefab => prefab;
            public int PrewarmCount => Mathf.Max(0, prewarmCount);
            public int MaxPoolSize => Mathf.Max(1, maxPoolSize);
            public float AutoReleaseTime => Mathf.Max(0.01f, autoReleaseTime);
        }

        private sealed class LocalShotPool
        {
            public readonly GameObject Prefab;
            public readonly Queue<GameObject> Inactive = new Queue<GameObject>();
            public readonly Transform Root;
            public int MaxPoolSize { get; private set; }
            public float AutoReleaseTime { get; private set; }
            public int TotalCount;

            public LocalShotPool(GameObject prefab, Transform ownerRoot, ShotPoolSetting setting, int defaultMaxPoolSize, float defaultAutoReleaseTime)
            {
                Prefab = prefab;
                GameObject rootGo = new GameObject($"[Pool] Em9030Shot_{prefab.name}");
                Root = rootGo.transform;
                Root.SetParent(ownerRoot, false);
                Apply(setting, defaultMaxPoolSize, defaultAutoReleaseTime);
            }

            public void Apply(ShotPoolSetting setting, int defaultMaxPoolSize, float defaultAutoReleaseTime)
            {
                MaxPoolSize = setting != null ? setting.MaxPoolSize : Mathf.Max(1, defaultMaxPoolSize);
                AutoReleaseTime = setting != null ? setting.AutoReleaseTime : Mathf.Max(0.01f, defaultAutoReleaseTime);
            }
        }
    }

    [DisallowMultipleComponent]
    internal sealed class Em9030LocalShotAutoRelease : MonoBehaviour
    {
        private MvEm9030 owner;
        private Coroutine routine;

        public void Initialize(MvEm9030 poolOwner, float autoReleaseTime)
        {
            owner = poolOwner;
            StopTimer();
            routine = StartCoroutine(ReleaseAfterDelay(Mathf.Max(0.01f, autoReleaseTime)));
        }

        public void StopTimer()
        {
            if (routine == null)
                return;

            StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator ReleaseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            routine = null;

            if (owner != null)
                owner.ReleaseLocalShot(gameObject);
            else
                gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            StopTimer();
        }
    }
}
