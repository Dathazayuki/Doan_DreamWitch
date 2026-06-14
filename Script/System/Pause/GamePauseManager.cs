using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Pause
{
    public static class GamePauseManager
    {
        private struct AnimatorRestoreInfo
        {
            public Animator Animator;
            public AnimatorUpdateMode UpdateMode;
        }

        private static readonly HashSet<int> pauseRequestIds = new HashSet<int>();
        private static readonly Dictionary<int, List<AnimatorRestoreInfo>> uiAnimatorRestoreMap = new Dictionary<int, List<AnimatorRestoreInfo>>();
        private static float previousTimeScale = 1f;

        public static bool IsPaused => pauseRequestIds.Count > 0;

        public static void RequestPause(Object requester, GameObject uiRoot = null, bool keepUiAnimating = true)
        {
            if (requester == null)
                return;

            int requesterId = requester.GetInstanceID();
            if (!pauseRequestIds.Add(requesterId))
                return;

            if (pauseRequestIds.Count == 1)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            if (keepUiAnimating && uiRoot != null)
                SetUiAnimatorsUnscaled(requesterId, uiRoot);
        }

        public static void ReleasePause(Object requester)
        {
            if (requester == null)
                return;

            int requesterId = requester.GetInstanceID();
            if (!pauseRequestIds.Remove(requesterId))
                return;

            RestoreUiAnimators(requesterId);

            if (pauseRequestIds.Count == 0)
                Time.timeScale = previousTimeScale;
        }

        private static void SetUiAnimatorsUnscaled(int requesterId, GameObject uiRoot)
        {
            Animator[] animators = uiRoot.GetComponentsInChildren<Animator>(true);
            if (animators == null || animators.Length == 0)
                return;

            var restoreList = new List<AnimatorRestoreInfo>(animators.Length);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator == null)
                    continue;

                restoreList.Add(new AnimatorRestoreInfo
                {
                    Animator = animator,
                    UpdateMode = animator.updateMode
                });
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            if (restoreList.Count > 0)
                uiAnimatorRestoreMap[requesterId] = restoreList;
        }

        private static void RestoreUiAnimators(int requesterId)
        {
            if (!uiAnimatorRestoreMap.TryGetValue(requesterId, out List<AnimatorRestoreInfo> restoreList))
                return;

            for (int i = 0; i < restoreList.Count; i++)
            {
                Animator animator = restoreList[i].Animator;
                if (animator != null)
                    animator.updateMode = restoreList[i].UpdateMode;
            }

            uiAnimatorRestoreMap.Remove(requesterId);
        }
    }
}
