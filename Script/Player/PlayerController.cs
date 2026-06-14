using UnityEngine;
using System.Collections.Generic;
using DreamKnight.Interfaces;
using DreamKnight.Player.States;
using System.Collections;
using DreamKnight.Systems.Zone;
using DreamKnight.Systems.Scene;
using DreamKnight.Systems.SkillTree;
using DreamKnight.UI;
using UnityEngine.SceneManagement;

namespace DreamKnight.Player
{
    /// <summary>
    /// Controller chính của Player - kết nối tất cả các component
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerCollision))]
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerDeathSequence))]
    [RequireComponent(typeof(PlayerHitFeedback))]
    [RequireComponent(typeof(PlayerVisualFeedback))]
    [RequireComponent(typeof(PersistentPlayerRoot))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public enum PlayerForm
        {
            Human = 0,
            Transformed = 1
        }

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject swordObject;
        [SerializeField] private Transform respawnPoint;

        [Header("Transform Forms")]
        [SerializeField] private PlayerFormConfig playerFormConfig;

        [Header("Transform Corpse")]
        [SerializeField] private bool showCorpseOnRevert = true;
        [SerializeField] private bool requireCorpseProximityToTransform = true;

        [Header("Trap Respawn Sequence")]
        [SerializeField] private float trapHitWaitDuration = 0.18f;
        [SerializeField] private float trapRespawnStartDuration = 0.28f;
        [SerializeField] private float trapCrouchEndDuration = 0.16f;

        [Header("Shrine Pray Sequence")]
        [SerializeField] private float prayLoopHoldDuration = 0.08f;

        private bool hasRespawnOverride;
        private Vector3 respawnOverridePosition;
        private Coroutine trapRespawnCoroutine;
        private Coroutine transformIntroCoroutine;

        private float defaultFixedDeltaTime;

        // Components
        private PlayerInput playerInput;
        private PlayerMovement playerMovement;
        private PlayerStats playerStats;
        private PlayerCollision playerCollision;
        private PlayerCombat playerCombat;
        private PlayerDeathSequence playerDeathSequence;
        private PlayerHitFeedback playerHitFeedback;
        private PlayerVisualFeedback playerVisualFeedback;
        private PlayerAnimationController animationController;
        private PlayerFormManager playerFormManager;
        public PlayerFormManager FormManager => playerFormManager;
        private int aliveLayer = -1;
        private PlayerForm currentForm = PlayerForm.Human;
        private RuntimeAnimatorController humanAnimatorController;
        private float humanHealthBeforeTransform;
        private float humanMaxHealthBeforeTransform;
        private readonly Dictionary<Enemy.EnemyTransformUnlockPickup, GameObject> corpseByTrigger
            = new Dictionary<Enemy.EnemyTransformUnlockPickup, GameObject>();
        private readonly HashSet<Enemy.EnemyTransformUnlockPickup> nearbyTransformTriggers
            = new HashSet<Enemy.EnemyTransformUnlockPickup>();
        private readonly HashSet<Enemy.EnemyTransformUnlockPickup> disabledTransformTriggers
            = new HashSet<Enemy.EnemyTransformUnlockPickup>();
        private Enemy.EnemyTransformUnlockPickup lastTouchedTransformTrigger;
        private Enemy.EnemyTransformUnlockPickup activeTransformTrigger;
        private GameObject activeTransformCorpse;

        // State Machine
        private PlayerStateMachine stateMachine;

        // Human States (always available)
        public IdleState IdleState { get; private set; }
        public MoveState MoveState { get; private set; }
        public JumpState JumpState { get; private set; }
        public DashState DashState { get; private set; }
        public WallClimbState WallClimbState { get; private set; }
        public CliffClimbState CliffClimbState { get; private set; }
        public AttackState AttackState { get; private set; }
        public CrouchState CrouchState { get; private set; }
        public LadderClimbState LadderClimbState { get; private set; }
        public HitState HitState { get; private set; }
        public DeathState DeathState { get; private set; }
        public PrayState PrayState { get; private set; }
        public PickUpState PickUpState { get; private set; }

        // Form-specific states are created on-demand via FormStateFactory
        private FormStateFactory formStateFactory;

        // Properties
        public PlayerInput Input => playerInput;
        public PlayerMovement Movement => playerMovement;
        public PlayerStats Stats => playerStats;
        public PlayerCombat Combat => playerCombat;
        public Animator Animator => animator;
        public PlayerAnimationController AnimationController => animationController;
        public PlayerStateMachine StateMachine => stateMachine;
        public PlayerDeathSequence DeathSequence => playerDeathSequence;
        public bool IsDeathSequencePlaying => playerDeathSequence != null && playerDeathSequence.IsPlaying;
        public bool IsTrapRespawnInProgress => trapRespawnCoroutine != null;
        public bool IsShrinePrayInProgress => stateMachine != null && stateMachine.CurrentState == PrayState;
        public float PrayLoopHoldDuration => prayLoopHoldDuration;
        public PlayerForm CurrentForm => currentForm;
        public bool IsTransformed => currentForm != PlayerForm.Human;
        public PlayerFormDataSO CurrentFormEntry => (IsTransformed && playerFormConfig != null) ? playerFormConfig.GetUnlockedForm() : null;
        
        /// <summary>
        /// Get the current form ID (for animation selection).
        /// </summary>
        public PlayerFormId CurrentFormId
        {
            get
            {
                if (!IsTransformed || playerFormConfig == null)
                    return PlayerFormId.Human;
                
                var entry = playerFormConfig.GetUnlockedForm();
                return entry?.formId ?? PlayerFormId.Human;
            }
        }

        // IDamageable Implementation
        public bool IsAlive => playerStats.IsAlive;
        public float CurrentHealth => playerStats.CurrentHealth;

        public Collider2D ActiveCollider
        {
            get
            {
                if (playerFormManager != null && playerFormManager.ActiveBodyCollider != null)
                {
                    return playerFormManager.ActiveBodyCollider;
                }
                return GetComponentInChildren<Collider2D>();
            }
        }

        private void Awake()
        {
            if (GetComponent<PersistentPlayerRoot>() == null)
                gameObject.AddComponent<PersistentPlayerRoot>();

            defaultFixedDeltaTime = Time.fixedDeltaTime;
            aliveLayer = gameObject.layer;
            InitializeComponents();
            InitializeStates();

            humanHealthBeforeTransform = playerStats != null ? playerStats.CurrentHealth : 0f;
            humanMaxHealthBeforeTransform = playerStats != null ? playerStats.MaxHealth : 0f;
        }

        private void Start()
        {
            // Khởi động State Machine với Idle State
            stateMachine.Initialize(IdleState);

            // Subscribe to events
            playerStats.OnDeath += HandleDeath;

            // Push stats ngay khi bắt đầu để UI cập nhật tức thì khi load game/đổi slot
            playerStats.NotifyStatsChanged();
        }

        private void Update()
        {
            if (!playerStats.IsAlive) return;
            if (trapRespawnCoroutine != null) return;

            HandleTransformInput();

            stateMachine.Update();
        }

        private void FixedUpdate()
        {
            if (!playerStats.IsAlive) return;
            if (trapRespawnCoroutine != null) return;

            stateMachine.FixedUpdate();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (playerStats != null)
            {
                playerStats.OnDeath -= HandleDeath;
            }

            playerDeathSequence?.StopAndResetTimeScale();
        }

        #region Initialization

        private void InitializeComponents()
        {
            playerInput = GetComponent<PlayerInput>();
            playerMovement = GetComponent<PlayerMovement>();
            playerStats = GetComponent<PlayerStats>();
            playerCollision = GetComponent<PlayerCollision>();
            playerCombat = GetComponent<PlayerCombat>();
            playerDeathSequence = GetComponent<PlayerDeathSequence>();
            playerHitFeedback = GetComponent<PlayerHitFeedback>();
            playerVisualFeedback = GetComponent<PlayerVisualFeedback>();
            playerFormManager = GetComponent<PlayerFormManager>();

            // Initialize form state factory
            formStateFactory = new FormStateFactory(this);

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                humanAnimatorController = animator.runtimeAnimatorController;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (swordObject == null)
            {
                Transform swordTransform = transform.Find("Sword");
                if (swordTransform != null)
                    swordObject = swordTransform.gameObject;
            }

            animationController = new PlayerAnimationController(animator);
            playerCollision?.Initialize(this, playerMovement, playerStats, spriteRenderer);
            playerCombat?.Initialize(this, playerMovement);
            playerDeathSequence?.Initialize(this, animationController, defaultFixedDeltaTime);
            playerHitFeedback?.Initialize(playerDeathSequence != null ? playerDeathSequence.CameraReference : null);
            playerVisualFeedback?.Initialize(spriteRenderer);

            // Gán body collider ban đầu từ HumanForm
            SyncPlayerMovementCollider();
        }

        private void InitializeStates()
        {
            stateMachine = new PlayerStateMachine();

            // Khởi tạo human states (luôn cần)
            IdleState = new IdleState(this);
            MoveState = new MoveState(this);
            JumpState = new JumpState(this);
            DashState = new DashState(this);
            WallClimbState = new WallClimbState(this);
            CliffClimbState = new CliffClimbState(this);
            AttackState = new AttackState(this);
            CrouchState = new CrouchState(this);
            LadderClimbState = new LadderClimbState(this);
            HitState = new HitState(this);
            DeathState = new DeathState(this);
            PrayState = new PrayState(this);
            PickUpState = new PickUpState(this);
            
            // Form-specific states được tạo on-demand khi form được chọn (thông qua FormStateFactory)
        }

        private void SyncPlayerMovementCollider()
        {
            if (playerFormManager != null && playerFormManager.ActiveBodyRef != null)
                playerMovement?.ApplyActiveFormCollider(playerFormManager.ActiveBodyRef);
        }

        #endregion

        #region IDamageable Implementation

        public void TakeDamage(float damage, GameObject damageSource = null)
        {
            if (playerCollision == null)
                return;

            playerCollision.ReceiveDamage(damage, damageSource);
        }

        #endregion

        #region Event Handlers

        private void HandleDeath()
        {
            Debug.Log("Player has died!");

            if (stateMachine.CurrentState != DeathState)
                stateMachine.ChangeState(DeathState);
        }

        public void DisablePlayerInputForDeath() => playerInput.DisableInput();

        public void StopPlayerMovementForDeath() => playerMovement.StopMovement();

        public void ApplyDeadBodyLayer() => SetLayerToDeadBody();

        private void SetLayerToDeadBody()
        {
            int deadBodyLayer = LayerMask.NameToLayer("DeadBody");
            if (deadBodyLayer < 0)
            {
                Debug.LogWarning("Layer 'DeadBody' does not exist. Player layer was not changed on death.");
                return;
            }

            SetLayerRecursively(transform, deadBodyLayer);
        }

        private void RestoreAliveLayer()
        {
            if (aliveLayer < 0)
                return;

            SetLayerRecursively(transform, aliveLayer);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null) return;
            // Giải quyết layer cần bỏ qua 1 lần duy nhất, rồi truyền vào đệ quy.
            int preservedLayer = LayerMask.NameToLayer("PlayerHitbox");
            if (preservedLayer < 0) preservedLayer = LayerMask.NameToLayer("PlayerHitBox");
            
            SetLayerRecursivelyInternal(root, layer, preservedLayer);
        }

        private static void SetLayerRecursivelyInternal(Transform root, int layer, int preservedLayer)
        {
            if (root == null) return;

            // Bỏ qua object đang ở layer được bảo vệ (PlayerHitbox, ...)
            // và toàn bộ cây con của nó.
            if (preservedLayer >= 0 && root.gameObject.layer == preservedLayer)
                return;

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursivelyInternal(root.GetChild(i), layer, preservedLayer);
        }

        #endregion

        #region Public Methods

        public void PlayHitCameraShake()
        {
            playerHitFeedback?.PlayHitCameraShake();
        }

        /// <summary>
        /// Get the appropriate Idle state for the current form.
        /// Sử dụng FormStateFactory để lấy state on-demand thay vì hardcode.
        /// </summary>
        public PlayerState GetIdleStateForCurrentForm()
        {
            var entry = CurrentFormEntry;
            return entry != null ? (formStateFactory?.GetIdleState(entry.formId) ?? IdleState) : IdleState;
        }

        /// <summary>
        /// Get the appropriate Hit state for the current form.
        /// Sử dụng FormStateFactory để lấy state on-demand thay vì hardcode.
        /// </summary>
        public PlayerState GetHitStateForCurrentForm()
        {
            var entry = CurrentFormEntry;
            return entry != null ? (formStateFactory?.GetHitState(entry.formId) ?? HitState) : HitState;
        }

        /// <summary>
        /// Get form-specific Idle state by formId (for internal form state transitions)
        /// </summary>
        public PlayerState GetFormIdleState(PlayerFormId formId)
        {
            return formStateFactory?.GetIdleState(formId) ?? IdleState;
        }

        /// <summary>
        /// Get form-specific Hit state by formId (for internal form state transitions)
        /// </summary>
        public PlayerState GetFormHitState(PlayerFormId formId)
        {
            return formStateFactory?.GetHitState(formId) ?? HitState;
        }

        /// <summary>
        /// Get form-specific Move state by formId (for internal form state transitions)
        /// </summary>
        public PlayerState GetFormMoveState(PlayerFormId formId)
        {
            return formStateFactory?.GetMoveState(formId) ?? MoveState;
        }

        /// <summary>
        /// Get form-specific Jump state by formId (for internal form state transitions)
        /// </summary>
        public PlayerState GetFormJumpState(PlayerFormId formId)
        {
            return formStateFactory?.GetJumpState(formId) ?? JumpState;
        }

        /// <summary>
        /// Get form-specific Crouch state by formId (for internal form state transitions)
        /// </summary>
        public PlayerState GetFormCrouchState(PlayerFormId formId)
        {
            return formStateFactory?.GetCrouchState(formId) ?? CrouchState;
        }

        /// <summary>
        /// Get form-specific Guard state by formId (for internal form state transitions)
        /// </summary>
        public PlayerState GetFormGuardState(PlayerFormId formId)
        {
            return formStateFactory?.GetGuardState(formId) ?? CrouchState;
        }

        /// <summary>
        /// Get form-specific Attack state by formId (for internal form state transitions)
        /// </summary>
        public PlayerState GetFormAttackState(PlayerFormId formId)
        {
            return formStateFactory?.GetAttackState(formId) ?? AttackState;
        }

        /// <summary>
        /// Get form-specific Dash state by formId (for internal form state transitions)
        /// </summary>
        public PlayerState GetFormDashState(PlayerFormId formId)
        {
            return formStateFactory?.GetDashState(formId) ?? DashState;
        }

        /// <summary>
        /// Respawn player tại vị trí checkpoint
        /// </summary>
        public void Respawn(Vector3 spawnPosition)
        {
            transform.position = spawnPosition;
            playerStats.ReviveToFullHealth();
            RestoreAliveLayer();
            playerCollision?.CancelInvincibility();
            Mv.MvAttack.BroadcastPlayerRevived();
            playerInput.EnableInput();
            playerDeathSequence?.StopAndResetTimeScale();
            PlayerState idleState = GetIdleStateForCurrentForm();
            stateMachine.ChangeState(idleState);
        }

        public Vector3 GetRespawnPosition()
        {
            if (hasRespawnOverride)
                return respawnOverridePosition;

            return respawnPoint != null ? respawnPoint.position : transform.position;
        }

        public void SetRespawnPosition(Vector3 position)
        {
            respawnOverridePosition = position;
            hasRespawnOverride = true;
        }

        public void SetRespawnPoint(Transform point)
        {
            if (point == null)
                return;

            respawnPoint = point;
            hasRespawnOverride = false;
        }

        public void TriggerTrapRespawn(float damage, GameObject damageSource = null)
        {
            if (trapRespawnCoroutine != null)
                return;

            // Trong cửa sổ spawn immunity: bỏ qua hoàn toàn trap respawn
            if (playerStats != null && playerStats.IsSpawnImmune)
            {
                return;
            }

            trapRespawnCoroutine = StartCoroutine(TrapRespawnRoutine(Mathf.Max(0f, damage), damageSource));
        }

        public bool TryStartShrinePraySequence(System.Action onPrayLoop)
        {
            return TryStartShrinePraySequence(onPrayLoop, null);
        }
        public bool TryStartShrinePraySequence(System.Action onPrayLoop, System.Func<bool> isLoopComplete)
        {
            if (!IsAlive || IsDeathSequencePlaying || trapRespawnCoroutine != null || (stateMachine != null && stateMachine.CurrentState == PrayState))
                return false;

            PrayState.Configure(onPrayLoop, isLoopComplete);
            stateMachine.ChangeState(PrayState);
            return true;
        }

        public bool TryStartPickUpSequence(System.Action onPickUpComplete, System.Action onPickUpCancelled = null)
        {
            if (!IsAlive || IsDeathSequencePlaying || trapRespawnCoroutine != null || IsTransformed || (stateMachine != null && stateMachine.CurrentState == PickUpState))
                return false;

            PickUpState.Configure(onPickUpComplete, onPickUpCancelled);
            stateMachine.ChangeState(PickUpState);
            return true;
        }

        private IEnumerator TrapRespawnRoutine(float damage, GameObject damageSource)
        {
            playerInput?.DisableInput();
            playerMovement?.StopMovement();

            if (damage > 0f)
                TakeDamage(damage, damageSource);

            // Nếu trap gây chết, ưu tiên luồng Death Respawn và không chạy Trap Respawn.
            if (!IsAlive)
            {
                trapRespawnCoroutine = null;
                yield break;
            }

            float waitHit = Mathf.Max(0f, trapHitWaitDuration);
            if (waitHit > 0f)
                yield return new WaitForSeconds(waitHit);
            RestoreAliveLayer();

            transform.position = GetRespawnPosition();
            playerMovement?.SetVelocity(Vector2.zero);

            bool playedRespawnGush = false;
            if (IsTransformed && playerFormConfig != null)
            {
                var entry = playerFormConfig.GetUnlockedForm();
                if (entry != null)
                {
                    string respawnGushAnim = FormAnimationHelper.GetRespawnGushAnimation(entry.formId);
                    if (!string.IsNullOrWhiteSpace(respawnGushAnim))
                    {
                        animationController?.ForcePlayAnimation(respawnGushAnim);
                        yield return WaitForAnimationFinished(respawnGushAnim, Mathf.Max(0.05f, entry.respawnGushTimeout));
                        playedRespawnGush = true;
                    }
                }
            }

            // Fallback to human-style respawn if no respawn gush animation
            if (!playedRespawnGush)
            {
                yield return PlayDefaultRespawnSequence();
            }

            if (stateMachine != null)
            {
                PlayerState idleState = GetIdleStateForCurrentForm();
                stateMachine.ChangeState(idleState);
            }

            string idleAnim = PlayerAnimationController.IDLE;
            if (IsTransformed && playerFormConfig != null)
            {
                var entry = playerFormConfig.GetUnlockedForm();
                if (entry != null)
                {
                    string formIdleAnim = FormAnimationHelper.GetIdleAnimation(entry.formId);
                    if (!string.IsNullOrWhiteSpace(formIdleAnim))
                        idleAnim = formIdleAnim;
                }
            }
            animationController?.CrossFadeAnimation(idleAnim, 0.05f);

            playerInput?.EnableInput();
            trapRespawnCoroutine = null;
        }



        public IEnumerator PrepareRespawnFromDeathSequenceRoutine()
        {
            StopPlayerMovementForDeath();

            string targetSceneName;
            Vector3 targetPosition;
            ResolveDeathRespawnTarget(out targetSceneName, out targetPosition);

            string activeSceneName = SceneManager.GetActiveScene().name;
            bool needsSceneLoad = !string.IsNullOrWhiteSpace(targetSceneName) &&
                                  !string.Equals(activeSceneName, targetSceneName, System.StringComparison.Ordinal);

            if (needsSceneLoad)
            {
                GlobalLoadingOverlay.Instance?.Show();

                AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
                while (loadOp != null && !loadOp.isDone)
                    yield return null;

                yield return null;

                // After loading scene, auto-register any shrine found (layer: GodStatue)
                RespawnShrine shrine = FindAnyObjectByType<RespawnShrine>(FindObjectsInactive.Include);
                if (shrine != null)
                {
                    Vector3 shrinePos = shrine.GetRespawnPosition();
                    string shrineScene = SceneManager.GetActiveScene().name;
                    RespawnShrineService.RegisterShrine(shrinePos, shrineScene);
                }

                ResolveDeathRespawnTarget(out targetSceneName, out targetPosition);

                if (GlobalLoadingOverlay.Instance != null)
                    yield return GlobalLoadingOverlay.Instance.HoldAndFadeOutRealtime(0.08f, 0.22f);
            }

            transform.position = targetPosition;
            playerStats.ReviveToFullHealth();
            RestoreAliveLayer();
            playerCollision?.CancelInvincibility();
            Mv.MvAttack.BroadcastPlayerRevived();
            SetSwordVisible(true);
        }

        private void ResolveDeathRespawnTarget(out string sceneName, out Vector3 position)
        {
            sceneName = SceneManager.GetActiveScene().name;

            // Always prioritize registered shrine if available
            if (RespawnShrineService.HasRegisteredShrine)
            {
                position = RespawnShrineService.GetShrinePosition();
                sceneName = RespawnShrineService.GetShrineSceneName();
                return;
            }

            // Try to find shrine (layer: GodStatue) in any scene
            RespawnShrine shrine = FindAnyObjectByType<RespawnShrine>(FindObjectsInactive.Include);
            if (shrine != null)
            {
                position = shrine.GetRespawnPosition();
                sceneName = shrine.gameObject.scene.name;
                return;
            }

            // Fallback: use current respawn point in current scene
            position = GetRespawnPosition();
        }

        public void CompleteRespawnFromDeathSequence()
        {
            playerCollision?.CancelInvincibility();
            Mv.MvAttack.BroadcastPlayerRevived();
            playerInput.EnableInput();
            PlayerState idleState = GetIdleStateForCurrentForm();
            stateMachine.ChangeState(idleState);
        }

        /// <summary>
        /// Abandon: quay về vị trí Shrine đã đăng ký (hoặc Church mặc định).
        /// PHẢI chạy trên PlayerController vì nó là DontDestroyOnLoad.
        /// Nếu chạy trên MenuTabSystemController (scene object), coroutine bị kill khi LoadScene.
        /// </summary>
        public void AbandonToShrine(string fallbackSceneName)
        {
            StartCoroutine(AbandonToShrineRoutine(fallbackSceneName));
        }

        private IEnumerator AbandonToShrineRoutine(string fallbackSceneName)
        {
            // Disable input ngay lập tức để tránh Player di chuyển trong lúc load
            playerInput?.DisableInput();

            // B1: Xác định scene + vị trí Shrine mục tiêu
            string targetScene;
            Vector3 targetPosition;

            ResolveDeathRespawnTarget(out targetScene, out targetPosition);

            // Nếu không có shrine nào → fallback về scene Church mặc định
            if (string.IsNullOrWhiteSpace(targetScene))
                targetScene = fallbackSceneName;

            // B2: Load scene nếu cần (coroutine sống sót vì chạy trên PersistentPlayerRoot)
            string activeScene = SceneManager.GetActiveScene().name;
            bool needsSceneLoad = !string.Equals(activeScene, targetScene, System.StringComparison.Ordinal);

            if (needsSceneLoad)
            {
                GlobalLoadingOverlay.Instance?.Show();

                AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
                while (loadOp != null && !loadOp.isDone)
                    yield return null;

                yield return null; // Cho scene objects khởi tạo xong

                // Sau khi load xong: auto-register shrine trong scene mới (nếu chưa có)
                RespawnShrine shrine = FindAnyObjectByType<RespawnShrine>(FindObjectsInactive.Include);
                if (shrine != null)
                {
                    targetPosition = shrine.GetRespawnPosition();
                    RespawnShrineService.RegisterShrine(targetPosition, SceneManager.GetActiveScene().name);
                }
                else
                {
                    // Nếu shrine không tìm được, dùng lại vị trí đã resolve từ trước
                    ResolveDeathRespawnTarget(out _, out targetPosition);
                }

                if (GlobalLoadingOverlay.Instance != null)
                    yield return GlobalLoadingOverlay.Instance.HoldAndFadeOutRealtime(0.08f, 0.22f);
            }

            // B3: Teleport + hồi HP + enable input
            transform.position = targetPosition;
            if (playerMovement != null)
                playerMovement.SetVelocity(Vector2.zero);

            playerStats?.ReviveToFullHealth();
            RestoreAliveLayer();
            SetSwordVisible(true);

            playerInput?.EnableInput();
            if (stateMachine != null && IdleState != null)
                stateMachine.ChangeState(IdleState);
        }

        /// <summary>
        /// Set sprite alpha (dùng cho visual effects như dash)
        /// </summary>
        public void SetSpriteAlpha(float alpha)
        {
            playerVisualFeedback?.SetSpriteAlpha(alpha);
        }

        public void SetSwordVisible(bool isVisible)
        {
            if (swordObject == null)
                return;

            if (swordObject.activeSelf == isVisible)
                return;

            swordObject.SetActive(isVisible);
        }

        public void ToggleTransformForm()
        {
            if (playerFormConfig == null)
            {
                Debug.LogWarning("[PlayerController] PlayerFormConfig is not assigned.");
                return;
            }

            if (!IsTransformSkillUnlocked())
            {
                Debug.LogWarning("[PlayerController] Transform is locked by SkillTree.");
                return;
            }

            if (currentForm == PlayerForm.Human)
            {
                if (!CanTransformFromCorpse())
                {
                    Debug.LogWarning("[PlayerController] Must be touching the enemy corpse to transform.");
                    return;
                }

                SelectActiveTransformTrigger();
                if (!PrepareTransformFromTrigger())
                    return;
                SwitchToSelectedForm();
            }
            else
            {
                SwitchToHuman();
            }
        }

        public void RegisterTransformCorpse(GameObject corpse, Enemy.EnemyTransformUnlockPickup trigger)
        {
            if (corpse == null || trigger == null)
                return;

            corpseByTrigger[trigger] = corpse;
            disabledTransformTriggers.Remove(trigger);
        }

        public void SetTransformCorpseProximity(bool isTouching, Enemy.EnemyTransformUnlockPickup source)
        {
            if (source == null)
                return;

            if (isTouching && !IsTransformSkillUnlocked())
                return;

            if (disabledTransformTriggers.Contains(source))
                return;

            if (isTouching)
            {
                nearbyTransformTriggers.Add(source);
                lastTouchedTransformTrigger = source;
            }
            else
            {
                nearbyTransformTriggers.Remove(source);
                if (lastTouchedTransformTrigger == source)
                    lastTouchedTransformTrigger = null;
            }
        }

        private void HandleTransformInput()
        {
            if (playerInput == null || !playerInput.TransformPressed)
                return;

            if (!IsTransformSkillUnlocked())
                return;

            if (!IsAlive || IsDeathSequencePlaying)
                return;

            if (trapRespawnCoroutine != null)
                return;

            if (IsShrinePrayInProgress)
                return;

            ToggleTransformForm();
        }

        public bool IsTransformSkillUnlocked()
        {
            SkillTreeManager manager = SkillTreeManager.Instance;
            return manager != null && manager.IsTransformUnlocked();
        }


        private void SwitchToSelectedForm()
        {
            HideTransformCorpse();

            var entry = playerFormConfig != null ? playerFormConfig.GetUnlockedForm() : null;
            if (entry == null)
            {
                Debug.LogWarning("[PlayerController] Selected form entry is null.");
                return;
            }

            if (!playerFormConfig.IsFormUnlocked(entry))
            {
                Debug.LogWarning($"[PlayerController] Form {entry.formId} is not unlocked yet.");
                return;
            }


            if (!UpdateAnimatorController(entry.animatorController))
            {
                Debug.LogWarning("[PlayerController] Selected form AnimatorController is not assigned.");
                return;
            }

            humanHealthBeforeTransform = playerStats != null ? playerStats.CurrentHealth : humanHealthBeforeTransform;
            humanMaxHealthBeforeTransform = playerStats != null ? playerStats.MaxHealth : humanMaxHealthBeforeTransform;

            currentForm = PlayerForm.Transformed;

            float formMaxHealth = playerFormConfig.GetFormMaxHealth(entry.formId);
            float formCurrentHealth = formMaxHealth;
            if (activeTransformTrigger != null)
                formCurrentHealth = activeTransformTrigger.GetRuntimeHealth(formMaxHealth);
            if (playerStats != null)
                playerStats.SetHealth(formCurrentHealth, formMaxHealth);

            SetSwordVisible(false);
            playerCombat?.ApplyFormProfile(entry.combatProfile);
            // Bật form prefab (instantiate) + tắt HumanForm
            playerFormManager?.ActivateForm(entry);
            // Cập nhật collider của PlayerMovement theo form mới
            SyncPlayerMovementCollider();
            if (transformIntroCoroutine != null)
                StopCoroutine(transformIntroCoroutine);
            transformIntroCoroutine = StartCoroutine(PlayTransformIntro(entry));
        }

        private void SwitchToHuman()
        {
            if (transformIntroCoroutine != null)
            {
                StopCoroutine(transformIntroCoroutine);
                transformIntroCoroutine = null;
            }

            if (CurrentFormEntry != null && playerStats != null && activeTransformTrigger != null)
            {
                activeTransformTrigger.SetRuntimeHealth(playerStats.CurrentHealth);
            }

            if (!UpdateAnimatorController(humanAnimatorController))
            {
                Debug.LogWarning("[PlayerController] Human AnimatorController is not assigned.");
                return;
            }

            currentForm = PlayerForm.Human;
            SetSwordVisible(true);
            playerCombat?.ResetToDefaultProfile();
            // Bật lại HumanForm, tắt/destroy form prefab
            playerFormManager?.DeactivateCurrentForm();
            // Restore collider gốc cho PlayerMovement
            SyncPlayerMovementCollider();
            if (playerStats != null)
                playerStats.SetHealth(humanHealthBeforeTransform, humanMaxHealthBeforeTransform);
            stateMachine.ForceChangeState(IdleState);

            if (showCorpseOnRevert)
                ShowTransformCorpseAtPlayer();
        }

        /// <summary>
        /// Gọi khi HP form biến hình về 0.
        /// Khôi phục HP form về tối đa rồi chuyển về Human.
        /// Player vẫn có thể biến hình lại form đó bình thường.
        /// </summary>
        public void HandleTransformedFormDepleted()
        {
            if (!IsTransformed || playerFormConfig == null || playerStats == null)
                return;

            PlayerFormDataSO entry = CurrentFormEntry;

            // SwitchToHuman trước — nó sẽ lưu HP hiện tại (= 0) vào runtime health
            SwitchToHuman();

            if (entry != null && activeTransformTrigger != null)
                activeTransformTrigger.SetRuntimeHealth(0f);

            // Clear unlocked form; must defeat another enemy to unlock again
            playerFormConfig.ClearUnlockedForm();

            DisableTransformCorpsePermanently();
        }

        private bool CanTransformFromCorpse()
        {
            if (!requireCorpseProximityToTransform)
                return true;

            return nearbyTransformTriggers.Count > 0;
        }

        private void SelectActiveTransformTrigger()
        {
            if (nearbyTransformTriggers.Count == 0)
            {
                activeTransformTrigger = null;
                activeTransformCorpse = null;
                return;
            }

            Enemy.EnemyTransformUnlockPickup selected = lastTouchedTransformTrigger;

            if (selected == null || !nearbyTransformTriggers.Contains(selected))
            {
                foreach (Enemy.EnemyTransformUnlockPickup trigger in nearbyTransformTriggers)
                {
                    selected = trigger;
                    break;
                }
            }

            activeTransformTrigger = selected;
            activeTransformCorpse = null;
            if (activeTransformTrigger != null)
                corpseByTrigger.TryGetValue(activeTransformTrigger, out activeTransformCorpse);
        }

        private bool PrepareTransformFromTrigger()
        {
            if (activeTransformTrigger == null)
            {
                Debug.LogWarning("[PlayerController] No active transform trigger selected.");
                return false;
            }

            PlayerFormDataSO formData = activeTransformTrigger.UnlockFormData;
            if (formData == null)
            {
                Debug.LogWarning("[PlayerController] Active transform trigger has no form data.");
                return false;
            }

            if (!playerFormConfig.UnlockForm(formData))
            {
                Debug.LogWarning("[PlayerController] Failed to unlock form from active transform trigger.");
                return false;
            }

            return true;
        }

        private void HideTransformCorpse()
        {
            if (activeTransformCorpse == null)
                return;

            activeTransformCorpse.SetActive(false);
            if (activeTransformTrigger != null)
                activeTransformTrigger.gameObject.SetActive(false);
        }

        private void ShowTransformCorpseAtPlayer()
        {
            if (activeTransformCorpse == null || activeTransformTrigger == null)
                return;

            if (disabledTransformTriggers.Contains(activeTransformTrigger))
                return;

            activeTransformCorpse.transform.position = transform.position;
            activeTransformCorpse.SetActive(true);
            activeTransformTrigger.gameObject.SetActive(true);
        }

        private void DisableTransformCorpsePermanently()
        {
            if (activeTransformTrigger == null)
                return;

            disabledTransformTriggers.Add(activeTransformTrigger);
            nearbyTransformTriggers.Remove(activeTransformTrigger);

            if (activeTransformCorpse != null)
                activeTransformCorpse.SetActive(false);
            activeTransformTrigger.gameObject.SetActive(false);

            UIManager.Instance?.HideInteractPrompt(activeTransformTrigger);

            corpseByTrigger.Remove(activeTransformTrigger);
            activeTransformTrigger = null;
            activeTransformCorpse = null;
        }

        /// <summary>
        /// Update animator controller and animation controller reference
        /// </summary>
        private bool UpdateAnimatorController(RuntimeAnimatorController controller)
        {
            if (animator == null || controller == null)
                return false;

            animator.runtimeAnimatorController = controller;
            animationController = new PlayerAnimationController(animator);
            return true;
        }

        private IEnumerator PlayTransformIntro(PlayerFormDataSO entry)
        {
            if (entry == null)
            {
                transformIntroCoroutine = null;
                yield break;
            }

            string respawnAnim = FormAnimationHelper.GetRespawnGushAnimation(entry.formId);
            float timeout = Mathf.Max(0.05f, entry.respawnGushTimeout);

            if (!string.IsNullOrWhiteSpace(respawnAnim))
            {
                animationController?.ForcePlayAnimation(respawnAnim);
                yield return WaitForAnimationFinished(respawnAnim, timeout);
            }

            // Change to per-form idle state using factory
            PlayerState idleState = formStateFactory?.GetIdleState(entry.formId) ?? IdleState;
            if (idleState != null)
                stateMachine.ForceChangeState(idleState);

            transformIntroCoroutine = null;
        }

        /// <summary>
        /// Play default human respawn animation sequence (crouch start -> end)
        /// </summary>
        private IEnumerator PlayDefaultRespawnSequence()
        {
            animationController?.ForcePlayAnimation(PlayerAnimationController.TRAP_RESPAWN_START);

            float waitStart = Mathf.Max(0f, trapRespawnStartDuration);
            if (waitStart > 0f)
                yield return new WaitForSeconds(waitStart);

            animationController?.ForcePlayAnimation(PlayerAnimationController.CROUCH_END);

            float waitCrouchEnd = Mathf.Max(0f, trapCrouchEndDuration);
            if (waitCrouchEnd > 0f)
                yield return new WaitForSeconds(waitCrouchEnd);
        }

        private IEnumerator WaitForAnimationFinished(string animationName, float timeout)
        {
            if (animationController == null)
                yield break;

            float limit = Mathf.Max(0.05f, timeout);
            float timer = 0f;
            bool hasStarted = false;

            while (timer < limit)
            {
                if (animationController.IsPlaying(animationName))
                {
                    hasStarted = true;
                    if (animationController.HasAnimationFinished())
                        yield break;
                }
                else if (hasStarted)
                {
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Flash sprite với màu (hit effect, power-up, v.v.)
        /// </summary>
        public void FlashSprite(Color flashColor, float duration = 0.1f)
        {
            playerVisualFeedback?.FlashSprite(flashColor, duration);
        }

        #endregion

        #region Animation Events
        
        // Animation Events được gọi từ animation clips
        // Tên method phải khớp với tên event trong Animation window

        /// <summary>
        /// Kiểm tra xem Player hiện tại có đang trong bất kỳ AttackState nào không
        /// (bao gồm cả Human AttackState và Form-specific AttackState như Em0060AttackState).
        /// </summary>
        public bool IsInAnyAttackState
        {
            get
            {
                var current = stateMachine?.CurrentState;
                if (current == null) return false;
                // Human form attack state
                if (current == AttackState) return true;
                // Form-specific: kiểm tra theo form hiện tại
                if (IsTransformed)
                {
                    var formAttack = formStateFactory?.GetAttackState(CurrentFormId);
                    if (formAttack != null && current == formAttack) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra xem một collider có phải là body collider chính của Player hay không.
        /// </summary>
        public bool IsBodyCollider(Collider2D col)
        {
            if (col == null) return false;
            if (col.gameObject == gameObject) return true;
            if (playerFormManager != null && playerFormManager.ActiveBodyColliderObject == col.gameObject)
                return true;
            return false;
        }
        
        /// <summary>
        /// Attack hit event - Khi attack animation vừa hit target
        /// </summary>
        public bool OnAttackHit()
        {
            if (playerCombat == null) return false;
            return playerCombat.OnAttackHit();
        }
        
        /// <summary>
        /// Attack end event - Khi attack animation kết thúc
        /// </summary>
        public void OnAttackEnd()
        {
            // TODO: Reset attack state
        }
        
        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            // Null checks để tránh lỗi khi components chưa initialize
            if (stateMachine == null || playerMovement == null || playerStats == null)
                return;

            // Hiển thị debug info
            GUI.Label(new Rect(10, 10, 300, 20), $"State: {stateMachine.CurrentState?.GetType().Name ?? "None"}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Grounded: {playerMovement.IsGrounded}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Velocity: {playerMovement.Velocity}");
        }

        private void OnDrawGizmosSelected()
        {
            if (playerCombat == null)
                playerCombat = GetComponent<PlayerCombat>();

            playerCombat?.DrawAttackGizmos(Application.isPlaying);
        }

        #endregion
    }
}
