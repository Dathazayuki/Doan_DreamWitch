using System.Collections;
using DreamKnight.Player;
using DreamKnight.UI;
using DreamKnight.Systems.Map;
using DreamKnight.Systems.Zone;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamKnight.Systems.Scene
{
    public static class GameSession
    {
        public static void ResetForTitle(GameObject excludeRoot = null)
        {
            UIManager.ResetSessionState();
            PortalCheckpointService.ResetSessionState();
            RespawnShrineService.ClearShrine();

            MapRenderTextureController mapController = Object.FindAnyObjectByType<MapRenderTextureController>();
            if (mapController != null)
                mapController.ResetFullMapState(true);

            DestroyDontDestroyOnLoadRoots(excludeRoot);
        }

        public static void LoadTitleAfterReset(string titleSceneName)
        {
            GameObject runnerObject = new GameObject("GameSessionRunner");
            Object.DontDestroyOnLoad(runnerObject);
            GameSessionRunner runner = runnerObject.AddComponent<GameSessionRunner>();
            runner.Begin(titleSceneName);
        }

        private static void DestroyDontDestroyOnLoadRoots(GameObject excludeRoot)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject root = allObjects[i];
                if (root == null)
                    continue;

                if (excludeRoot != null && root == excludeRoot)
                    continue;

                if (root.transform.parent != null)
                    continue;

                if (root.scene.name != "DontDestroyOnLoad")
                    continue;

                if (root.GetComponent<SingleInstance>() != null)
                    continue;

                Object.Destroy(root);
            }
        }

        private sealed class GameSessionRunner : MonoBehaviour
        {
            private string targetSceneName;

            public void Begin(string sceneName)
            {
                targetSceneName = sceneName;
                StartCoroutine(RunRoutine());
            }

            private IEnumerator RunRoutine()
            {
                ResetForTitle(gameObject);

                // Show global loading overlay if available while we clear DDOL roots
                GlobalLoadingOverlay overlay = GlobalLoadingOverlay.Instance;
                if (overlay != null)
                    overlay.Show();

                int safetyFrames = 0;
                while (HasRemainingDontDestroyRoots(gameObject) && safetyFrames < 120)
                {
                    safetyFrames++;
                    yield return null;
                }

                // Fade out the overlay if we showed it
                if (overlay != null)
                {
                    yield return overlay.HoldAndFadeOutRealtime(0.08f, 0.22f);
                }

                if (!string.IsNullOrWhiteSpace(targetSceneName))
                    SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);

                Destroy(gameObject);
            }

            private static bool HasRemainingDontDestroyRoots(GameObject excludeRoot)
            {
                GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                for (int i = 0; i < allObjects.Length; i++)
                {
                    GameObject root = allObjects[i];
                    if (root == null)
                        continue;

                    if (excludeRoot != null && root == excludeRoot)
                        continue;

                    if (root.transform.parent != null)
                        continue;

                    if (root.scene.name != "DontDestroyOnLoad")
                        continue;

                    if (root.GetComponent<SingleInstance>() != null)
                        continue;

                    return true;
                }

                return false;
            }
        }
    }
}
