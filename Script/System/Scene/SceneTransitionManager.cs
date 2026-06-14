using System.Collections;
using DreamKnight.Player;
using DreamKnight.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamKnight.Systems.Scene
{
    [DisallowMultipleComponent]
    public class SceneTransitionManager : MonoBehaviour
    {
        private static SceneTransitionManager instance;

        [Header("Transition")]
        [SerializeField] private float preLoadAnimationDuration = 0.4f;
        [SerializeField] private float playerTransitionCrossFade = 0.05f;
        [SerializeField] private float postLoadBlackHoldDuration = 0.06f;
        [SerializeField] private float postLoadFadeOutDuration = 0.2f;

        [Tooltip("Số giây Player được miễn sát thương sau khi spawn qua cửa (tránh dính trap/enemy ngay điểm spawn).")]
        [SerializeField] private float spawnImmunityDuration = 2f;

        [Header("Loading Overlay")]
        [SerializeField] private CanvasGroup loadingOverlayTemplate;

        [Header("Persistent Objects")]
        [SerializeField] private bool persistMainCamera = true;
        [SerializeField] private bool persistCinemachineCamera = true;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CinemachineCamera cinemachineCamera;

        private bool isTransitioning;
        private string pendingTargetDoorId;

        public static SceneTransitionManager Instance => instance;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (mainCamera == null)
                mainCamera = Camera.main;
            if (cinemachineCamera == null)
                cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();

            if (persistMainCamera && mainCamera != null)
                DontDestroyOnLoad(mainCamera.gameObject);

            if (persistCinemachineCamera && cinemachineCamera != null)
                DontDestroyOnLoad(cinemachineCamera.gameObject);

            EnsureLoadingOverlay();

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        public void RequestDoorTransition(PlayerController player, string targetSceneName, string targetDoorId)
        {
            if (isTransitioning)
                return;

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogWarning("SceneTransitionManager: targetSceneName is empty.");
                return;
            }

            StartCoroutine(TransitionRoutine(player, targetSceneName, targetDoorId));
        }

        private IEnumerator TransitionRoutine(PlayerController player, string targetSceneName, string targetDoorId)
        {
            isTransitioning = true;
            pendingTargetDoorId = targetDoorId;
            EnsureLoadingOverlay();

            PlayerInput playerInput = player != null ? player.Input : null;
            if (playerInput != null)
                playerInput.DisableInput();

            PlayPlayerTransitionState(player);

            float crossFadeWait = Mathf.Max(0f, playerTransitionCrossFade);
            if (crossFadeWait > 0f)
                yield return new WaitForSecondsRealtime(crossFadeWait);

            GlobalLoadingOverlay.Instance?.Show();

            float remainAnimationWait = Mathf.Max(0f, preLoadAnimationDuration);
            if (remainAnimationWait > 0f)
                yield return new WaitForSecondsRealtime(remainAnimationWait);

            // ⚠️ Set immunity TỪ TRƯỚC khi scene load — phải set trước mọi yield,
            // vì FixedUpdate (Physics) chạy TRƯỚC khi coroutine resume sau yield return null.
            // Nếu set sau yield thì Physics đã kịp fire OnTriggerEnter2D rồi.
            PlayerStats playerStatsRef = player != null ? player.GetComponent<PlayerStats>() : null;
            if (playerStatsRef != null && spawnImmunityDuration > 0f)
            {
                playerStatsRef.SetSpawnImmunity(true);
            }

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetSceneName);
            while (loadOp != null && !loadOp.isDone)
                yield return null;

            // Chờ 2 frame để tất cả Awake/Start trong scene mới hoàn tất.
            yield return null;
            yield return null;

            // Disable collider trong lúc teleport để OnTriggerEnter2D không fire
            // ngay cả khi Physics2D.SyncTransforms() chạy synchronous.
            Collider2D bodyCol = player != null ? player.ActiveCollider : null;
            if (bodyCol != null) bodyCol.enabled = false;

            PlacePlayerAtPendingDoor(player);
            ForcePlayerToIdleAfterTransition(player);
            RebindCameraFollow(player);

            // Re-enable collider frame tiếp theo (immunity vẫn còn active)
            StartCoroutine(ReEnableColliderNextFrame(bodyCol));

            // Hẹn giờ tắt immunity sau duration
            if (playerStatsRef != null && spawnImmunityDuration > 0f)
                StartCoroutine(ClearSpawnImmunityAfter(playerStatsRef, spawnImmunityDuration));

            if (GlobalLoadingOverlay.Instance != null)
                yield return GlobalLoadingOverlay.Instance.HoldAndFadeOutRealtime(postLoadBlackHoldDuration, postLoadFadeOutDuration);
            else
                yield return null;

            if (playerInput != null)
                playerInput.EnableInput();

            isTransitioning = false;
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            CleanupDuplicateSingletons();
            EnsureLoadingOverlay();
            PlayerController player = ResolvePlayer();
            RebindCameraFollow(player);
            CleanupDuplicateMainCameras();
            CleanupDuplicateCinemachineCameras();

            // Safety hide only when not in active transition.
            if (!isTransitioning)
                GlobalLoadingOverlay.Instance?.Hide();
        }

        private void CleanupDuplicateSingletons()
        {
            CleanupDuplicateComponents(this);
            CleanupDuplicateComponents(GlobalLoadingOverlay.Instance);
            CleanupDuplicateComponents(PersistentPlayerRoot.Instance);
            CleanupDuplicateComponents(VfxPoolManager.Instance);
            CleanupDuplicateComponents(UIManager.Instance);
            CleanupDuplicateComponents(PlayerSpecificationsUI.Instance);
        }

        private void CleanupDuplicateComponents<T>(T keep) where T : MonoBehaviour
        {
            T[] found = FindObjectsOfType<T>(true);
            T keepRef = keep;

            for (int i = 0; i < found.Length; i++)
            {
                T candidate = found[i];
                if (candidate == null)
                    continue;

                if (keepRef == null)
                {
                    keepRef = candidate;
                    continue;
                }

                if (candidate == keepRef)
                    continue;

                Destroy(candidate.gameObject);
            }
        }

        private void PlayPlayerTransitionState(PlayerController player)
        {
            if (player == null)
                return;

            PlayerAnimationController animationController = player.AnimationController;
            if (animationController == null)
                return;

            string transitionState = PlayerAnimationController.ENTER_DOOR;

            try
            {
                if (playerTransitionCrossFade > 0f)
                    animationController.CrossFadeAnimation(transitionState, playerTransitionCrossFade, 0);
                else
                    animationController.ForcePlayAnimation(transitionState, 0);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SceneTransitionManager: failed to play state '{transitionState}'. {e.Message}");
            }
        }

        private void PlacePlayerAtPendingDoor(PlayerController player)
        {
            if (player == null)
                player = ResolvePlayer();

            if (player == null || string.IsNullOrWhiteSpace(pendingTargetDoorId))
                return;

            SceneDoorSpawnPoint[] points = FindObjectsOfType<SceneDoorSpawnPoint>(true);
            for (int i = 0; i < points.Length; i++)
            {
                SceneDoorSpawnPoint point = points[i];
                if (point == null)
                    continue;

                if (!string.Equals(point.DoorId, pendingTargetDoorId, System.StringComparison.Ordinal))
                    continue;

                TeleportPlayerTo(player, point.transform.position);
                pendingTargetDoorId = null;
                return;
            }

            // Fallback by object name for quick setup.
            GameObject namedDoor = GameObject.Find(pendingTargetDoorId);
            if (namedDoor != null)
                TeleportPlayerTo(player, namedDoor.transform.position);

            pendingTargetDoorId = null;
        }

        /// <summary>
        /// Dịch chuyển Player tức thì và sync Rigidbody2D để tránh Physics engine
        /// đọc vị trí cũ trong frame tiếp theo (gây kích hoạt trap/trigger sai vị trí).
        /// </summary>
        private void TeleportPlayerTo(PlayerController player, Vector3 targetPosition)
        {
            if (player == null) return;

            // 1. Dừng vận tốc trước khi dịch chuyển
            if (player.Movement != null)
                player.Movement.SetVelocity(Vector2.zero);

            // 2. Đặt transform.position
            player.transform.position = targetPosition;

            // 3. Sync Rigidbody2D.position ngay lập tức để Physics engine không
            //    dùng vị trí cũ trong FixedUpdate kế tiếp (tránh ghost collision).
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = targetPosition;
                rb.linearVelocity = Vector2.zero;
            }

            // 4. Yêu cầu Physics2D đồng bộ tất cả Transform → Collider ngay frame này.
            Physics2D.SyncTransforms();
        }

        /// <summary>
        /// Hẹn giờ tắt spawn immunity sau [duration] giây.
        /// </summary>
        private IEnumerator ClearSpawnImmunityAfter(PlayerStats stats, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (stats != null)
            {
                stats.SetSpawnImmunity(false);
            }
        }

        /// <summary>
        /// Re-enable body collider sau 1 frame (immunity vẫn còn active).
        /// Collider bị disable trong lúc teleport để block OnTriggerEnter2D.
        /// </summary>
        private IEnumerator ReEnableColliderNextFrame(Collider2D col)
        {
            yield return null;
            if (col != null)
                col.enabled = true;
        }

        private void ForcePlayerToIdleAfterTransition(PlayerController player)
        {
            if (player == null)
                return;

            if (player.StateMachine != null && player.IdleState != null && player.StateMachine.CurrentState != player.IdleState)
                player.StateMachine.ChangeState(player.IdleState);

            player.AnimationController?.CrossFadeAnimation(PlayerAnimationController.IDLE, 0.05f);
        }

        private void RebindCameraFollow(PlayerController player)
        {
            if (player == null)
                player = ResolvePlayer();

            if (player == null)
                return;

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (cinemachineCamera == null)
                cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();

            if (cinemachineCamera != null)
            {
                PlayerCameraLookController lookController = cinemachineCamera.GetComponent<PlayerCameraLookController>();
                if (lookController != null)
                {
                    lookController.BindToPlayer(player);
                }
                else
                {
                    cinemachineCamera.Follow = player.transform;
                    cinemachineCamera.LookAt = player.transform;
                }
            }
        }

        private PlayerController ResolvePlayer()
        {
            if (PersistentPlayerRoot.Instance != null)
                return PersistentPlayerRoot.Instance.GetComponent<PlayerController>();

            return FindAnyObjectByType<PlayerController>();
        }

        private void CleanupDuplicateMainCameras()
        {
            if (!persistMainCamera || mainCamera == null)
                return;

            Camera[] cameras = FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam == null || cam == mainCamera)
                    continue;

                if (!cam.CompareTag("MainCamera"))
                    continue;

                Destroy(cam.gameObject);
            }
        }

        private void CleanupDuplicateCinemachineCameras()
        {
            if (!persistCinemachineCamera)
                return;

            CinemachineCamera[] cameras = FindObjectsOfType<CinemachineCamera>(true);
            if (cameras == null || cameras.Length <= 1)
                return;

            CinemachineCamera keep = cinemachineCamera;
            if (keep == null)
            {
                keep = cameras[0];
                cinemachineCamera = keep;

                if (keep != null)
                    DontDestroyOnLoad(keep.gameObject);
            }

            for (int i = 0; i < cameras.Length; i++)
            {
                CinemachineCamera cam = cameras[i];
                if (cam == null || cam == keep)
                    continue;

                Destroy(cam.gameObject);
            }
        }

        private void EnsureLoadingOverlay()
        {
            if (GlobalLoadingOverlay.Instance != null)
                return;

            if (loadingOverlayTemplate == null)
                loadingOverlayTemplate = FindTemplateFromScene();

            if (loadingOverlayTemplate != null)
                GlobalLoadingOverlay.BootstrapFromTemplate(loadingOverlayTemplate);
        }

        private CanvasGroup FindTemplateFromScene()
        {
            CanvasGroup[] groups = FindObjectsOfType<CanvasGroup>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                CanvasGroup group = groups[i];
                if (group == null)
                    continue;

                if (group.name.IndexOf("loading", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return group;
            }

            return null;
        }
    }
}
