using UnityEngine;

namespace DreamKnight.Systems.Pause
{
    [DisallowMultipleComponent]
    public class UIPauseRequester : MonoBehaviour
    {
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private bool keepUiAnimating = true;
        [SerializeField] private bool requestPauseOnEnable = true;

        private bool pauseRequested;

        private void OnEnable()
        {
            if (requestPauseOnEnable)
                RequestPause();
        }

        private void OnDisable()
        {
            ReleasePause();
        }

        private void OnDestroy()
        {
            ReleasePause();
        }

        public void RequestPause()
        {
            if (pauseRequested)
                return;

            GameObject root = uiRoot != null ? uiRoot : gameObject;
            GamePauseManager.RequestPause(this, root, keepUiAnimating);
            pauseRequested = true;
        }

        public void ReleasePause()
        {
            if (!pauseRequested)
                return;

            GamePauseManager.ReleasePause(this);
            pauseRequested = false;
        }
    }
}