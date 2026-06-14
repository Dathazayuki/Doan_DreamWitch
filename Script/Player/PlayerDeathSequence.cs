using Unity.Cinemachine;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerDeathSequence : MonoBehaviour
    {
        [Header("Death Sequence")]
        [SerializeField] private CinemachineCamera deathCinemachineCamera;
        [SerializeField] private float deathSlowTimeScale = 0.2f;
        [SerializeField] private float deathZoomDuration = 0.35f;
        [SerializeField] private float deathOrthographicZoomSize = 3.5f;
        [SerializeField] private float deathPerspectiveFov = 40f;
        [SerializeField] private float deathMeltBlendDelay = 0.05f;

        [Header("Respawn Sequence")]
        [SerializeField] private float deathAnimationTimeout = 5f;
        [SerializeField] private float deathMeltMinDuration = 0.2f;
        [SerializeField] private float respawnGushTimeout = 2f;
        [SerializeField] private float respawnAppealTimeout = 2f;

        private PlayerController playerController;
        private PlayerAnimationController animationController;
        private Coroutine deathSequenceCoroutine;
        private bool isPlaying;
        private float defaultFixedDeltaTime;
        private bool hasCachedLens;
        private bool cachedUseCinemachineLens;
        private bool cachedIsOrthographic;
        private float cachedOrthographicSize;
        private float cachedFov;

        public bool IsPlaying => isPlaying;
        public CinemachineCamera CameraReference => deathCinemachineCamera;

        public void Initialize(PlayerController ownerController, PlayerAnimationController playerAnimationController, float initialFixedDeltaTime)
        {
            playerController = ownerController;
            animationController = playerAnimationController;
            defaultFixedDeltaTime = initialFixedDeltaTime;

            if (deathCinemachineCamera == null)
                deathCinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        public void Play()
        {
            if (isPlaying)
                return;

            if (deathSequenceCoroutine != null)
                StopCoroutine(deathSequenceCoroutine);
            deathSequenceCoroutine = StartCoroutine(DeathSequenceCoroutine());
        }

        public void StopAndResetTimeScale()
        {
            if (deathSequenceCoroutine != null)
            {
                StopCoroutine(deathSequenceCoroutine);
                deathSequenceCoroutine = null;
            }

            isPlaying = false;
            if (Mathf.Abs(Time.timeScale - 1f) > 0.0001f)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = defaultFixedDeltaTime;
            }

            RestoreLensToCachedValue();
        }

        private void ResetTimeScaleToDefault()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }

        private System.Collections.IEnumerator DeathSequenceCoroutine()
        {
            isPlaying = true;

            CinemachineCamera cinemachineCamera = deathCinemachineCamera;
            Camera cam = Camera.main;
            bool useCinemachineLens = cinemachineCamera != null;
            bool isOrthographic = useCinemachineLens
                ? cinemachineCamera.Lens.Orthographic
                : cam != null && cam.orthographic;
            float startOrthoSize = useCinemachineLens
                ? cinemachineCamera.Lens.OrthographicSize
                : isOrthographic && cam != null ? cam.orthographicSize : 0f;
            float startFov = useCinemachineLens
                ? cinemachineCamera.Lens.FieldOfView
                : !isOrthographic && cam != null ? cam.fieldOfView : 0f;

            CacheInitialLens(useCinemachineLens, isOrthographic, startOrthoSize, startFov);

            float targetTimeScale = Mathf.Clamp(deathSlowTimeScale, 0.01f, 1f);
            Time.timeScale = targetTimeScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime * targetTimeScale;

            animationController?.ForcePlayAnimation(PlayerAnimationController.DEATH);

            float zoomTimer = 0f;
            while (zoomTimer < deathZoomDuration)
            {
                zoomTimer += Time.unscaledDeltaTime;
                float t = deathZoomDuration <= 0.0001f ? 1f : Mathf.Clamp01(zoomTimer / deathZoomDuration);
                if (useCinemachineLens)
                {
                    LensSettings lens = cinemachineCamera.Lens;
                    if (isOrthographic)
                        lens.OrthographicSize = Mathf.Lerp(startOrthoSize, deathOrthographicZoomSize, t);
                    else
                        lens.FieldOfView = Mathf.Lerp(startFov, deathPerspectiveFov, t);

                    cinemachineCamera.Lens = lens;
                }
                else if (cam != null)
                {
                    if (isOrthographic)
                        cam.orthographicSize = Mathf.Lerp(startOrthoSize, deathOrthographicZoomSize, t);
                    else
                        cam.fieldOfView = Mathf.Lerp(startFov, deathPerspectiveFov, t);
                }
                yield return null;
            }

            // Đợi 1 frame để Animator kịp transition sang state DeathHit
            yield return null;
            float safeDeathTimeout = Mathf.Max(0.5f, deathAnimationTimeout);
            yield return WaitForAnimationFinishedRealtime(PlayerAnimationController.DEATH, safeDeathTimeout);

            if (deathMeltBlendDelay > 0f)
                yield return new WaitForSecondsRealtime(deathMeltBlendDelay);

            animationController?.ForcePlayAnimation(PlayerAnimationController.DEATH_MELT);

            float meltWait = Mathf.Max(0f, deathMeltMinDuration);
            if (meltWait > 0f)
                yield return new WaitForSecondsRealtime(meltWait);

            yield return WaitForAnimationFinishedRealtime(PlayerAnimationController.DEATH_MELT, respawnGushTimeout);

            ResetTimeScaleToDefault();
            if (playerController != null)
                yield return playerController.PrepareRespawnFromDeathSequenceRoutine();

            animationController?.ForcePlayAnimation(PlayerAnimationController.RESPAWN_GUSH);
            yield return WaitForAnimationFinishedRealtime(PlayerAnimationController.RESPAWN_GUSH, respawnGushTimeout);

            animationController?.ForcePlayAnimation(PlayerAnimationController.RESPAWN_APPEAL);
            yield return WaitForAnimationFinishedRealtime(PlayerAnimationController.RESPAWN_APPEAL, respawnAppealTimeout);

            RestoreLensToCachedValue();
            playerController?.CompleteRespawnFromDeathSequence();
            isPlaying = false;
            deathSequenceCoroutine = null;
        }

        private void CacheInitialLens(bool useCinemachineLens, bool isOrthographic, float orthographicSize, float fov)
        {
            hasCachedLens = true;
            cachedUseCinemachineLens = useCinemachineLens;
            cachedIsOrthographic = isOrthographic;
            cachedOrthographicSize = orthographicSize;
            cachedFov = fov;
        }

        private void RestoreLensToCachedValue()
        {
            if (!hasCachedLens)
                return;

            CinemachineCamera cinemachineCamera = deathCinemachineCamera;
            Camera cam = Camera.main;

            if (cachedUseCinemachineLens && cinemachineCamera != null)
            {
                LensSettings lens = cinemachineCamera.Lens;
                if (cachedIsOrthographic)
                    lens.OrthographicSize = cachedOrthographicSize;
                else
                    lens.FieldOfView = cachedFov;

                cinemachineCamera.Lens = lens;
                return;
            }

            if (cam == null)
                return;

            if (cachedIsOrthographic)
                cam.orthographicSize = cachedOrthographicSize;
            else
                cam.fieldOfView = cachedFov;
        }

        private System.Collections.IEnumerator WaitForAnimationFinishedRealtime(string animationName, float timeout)
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

                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private System.Collections.IEnumerator WaitForAnimationFinishedNoTimeout(string animationName)
        {
            if (animationController == null)
                yield break;

            bool hasStarted = false;

            while (true)
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

                yield return null;
            }
        }
    }
}