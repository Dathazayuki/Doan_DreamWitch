using UnityEngine;
using System.Collections;

namespace DreamKnight.Systems.Scene
{
    [DisallowMultipleComponent]
    public class GlobalLoadingOverlay : MonoBehaviour
    {
        private static GlobalLoadingOverlay instance;

        [SerializeField] private CanvasGroup canvasGroup;
        private Coroutine fadeCoroutine;

        public static GlobalLoadingOverlay Instance => instance;

        public static void BootstrapFromTemplate(CanvasGroup template)
        {
            if (instance != null || template == null)
                return;

            Canvas rootCanvas = template.GetComponentInParent<Canvas>();
            GameObject sourceRoot = rootCanvas != null ? rootCanvas.gameObject : template.gameObject;

            GameObject clone = Object.Instantiate(sourceRoot);
            clone.name = "GlobalLoadingOverlay";
            clone.transform.SetParent(null, true);
            clone.SetActive(true);

            GlobalLoadingOverlay overlay = clone.GetComponent<GlobalLoadingOverlay>();
            if (overlay == null)
                overlay = clone.AddComponent<GlobalLoadingOverlay>();

            if (rootCanvas != null)
            {
                string path = GetRelativePath(template.transform, rootCanvas.transform);
                Transform mapped = string.IsNullOrEmpty(path) ? clone.transform : clone.transform.Find(path);
                if (mapped != null)
                    overlay.canvasGroup = mapped.GetComponent<CanvasGroup>();
            }

            if (overlay.canvasGroup == null)
                overlay.canvasGroup = clone.GetComponentInChildren<CanvasGroup>(true);

            overlay.ConfigureCanvasForOverlay();
            overlay.HideImmediate();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            DontDestroyOnLoad(gameObject);
            ConfigureCanvasForOverlay();
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void Show()
        {
            StopFadeCoroutine();
            SetVisible(true);
            SetAlpha(1f);
        }

        public void Hide()
        {
            StopFadeCoroutine();
            SetVisible(false);
        }

        public void HideImmediate()
        {
            StopFadeCoroutine();
            SetVisible(false);
        }

        public IEnumerator HoldAndFadeOutRealtime(float holdDuration, float fadeDuration)
        {
            StopFadeCoroutine();
            fadeCoroutine = StartCoroutine(HoldAndFadeRoutine(holdDuration, fadeDuration));
            yield return fadeCoroutine;
            fadeCoroutine = null;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        private IEnumerator HoldAndFadeRoutine(float holdDuration, float fadeDuration)
        {
            Show();

            if (holdDuration > 0f)
                yield return new WaitForSecondsRealtime(holdDuration);

            float duration = Mathf.Max(0.0001f, fadeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(1f - t);
                yield return null;
            }

            Hide();
        }

        private void StopFadeCoroutine()
        {
            if (fadeCoroutine == null)
                return;

            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        private void ConfigureCanvasForOverlay()
        {
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                    continue;

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 9000 + i;
            }
        }

        private static string GetRelativePath(Transform child, Transform root)
        {
            if (child == null || root == null || child == root)
                return string.Empty;

            System.Collections.Generic.Stack<string> parts = new System.Collections.Generic.Stack<string>();
            Transform current = child;

            while (current != null && current != root)
            {
                parts.Push(current.name);
                current = current.parent;
            }

            if (current != root)
                return string.Empty;

            return string.Join("/", parts);
        }
    }
}