using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamKnight.Systems.Scene
{
    [DisallowMultipleComponent]
    public class CutsceneAnimatorSceneLoader : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private int layerIndex;
        [SerializeField] private string lastStateName;
        [SerializeField, Range(0.8f, 1f)] private float completeNormalizedTime = 0.99f;

        [Header("Next Scene")]
        [SerializeField] private string gameplaySceneName;
        [SerializeField] private int gameplaySceneIndex = -1;
        [SerializeField] private float delayAfterComplete;

        private bool hasEnteredLastState;
        private bool loading;

        private void Reset()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            GlobalLoadingOverlay.Instance?.HideImmediate();
        }

        private void Update()
        {
            if (loading || animator == null)
                return;

            if (animator.IsInTransition(layerIndex))
                return;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (!IsLastState(stateInfo))
                return;

            hasEnteredLastState = true;

            if (stateInfo.loop)
                return;

            if (stateInfo.normalizedTime < completeNormalizedTime)
                return;

            StartCoroutine(LoadGameplayRoutine());
        }

        private bool IsLastState(AnimatorStateInfo stateInfo)
        {
            if (string.IsNullOrWhiteSpace(lastStateName))
                return hasEnteredLastState;

            return stateInfo.IsName(lastStateName);
        }

        private IEnumerator LoadGameplayRoutine()
        {
            loading = true;

            float delay = Mathf.Max(0f, delayAfterComplete);
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            GlobalLoadingOverlay.Instance?.Show();

            AsyncOperation loadOperation = null;
            if (!string.IsNullOrWhiteSpace(gameplaySceneName))
                loadOperation = SceneManager.LoadSceneAsync(gameplaySceneName);
            else if (gameplaySceneIndex >= 0)
                loadOperation = SceneManager.LoadSceneAsync(gameplaySceneIndex);

            if (loadOperation == null)
            {
                Debug.LogWarning("[CutsceneAnimatorSceneLoader] Gameplay scene is not configured.", this);
                loading = false;
                yield break;
            }

            while (!loadOperation.isDone)
                yield return null;
        }
    }
}
