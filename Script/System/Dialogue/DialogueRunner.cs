using System;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

namespace DreamKnight.Systems.Dialogue
{
    /// <summary>
    /// Minimal wrapper: chỉ expose Yarn DialogueRunner và events.
    /// Không can thiệp vào dialogue flow (delegate trực tiếp cho Yarn).
    /// 
    /// Setup: Gắn cùng GameObject với Yarn.Unity.DialogueRunner và YarnDialogueView.
    /// </summary>
    [DisallowMultipleComponent]
    public class DreamKnightDialogueRunner : MonoBehaviour
    {
        [Header("Yarn Spinner Reference")]
        [SerializeField] private DialogueRunner yarnRunner;

        // ── Singleton ──────────────────────────────────────────────────────
        private static DreamKnightDialogueRunner _instance;
        public static DreamKnightDialogueRunner Instance => _instance;

        // ── Expose Yarn State ──────────────────────────────────────────────
        public bool IsRunning => yarnRunner != null && yarnRunner.IsDialogueRunning;
        public DialogueRunner YarnRunner => yarnRunner;

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Khi Yarn bắt đầu hội thoại (thông qua onNodeStart)</summary>
        public event Action<string> OnDialogueStarted;
        /// <summary>Khi Yarn kết thúc hội thoại (thông qua onDialogueComplete)</summary>
        public event Action OnDialogueEnded;

        // ──────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (yarnRunner == null)
                yarnRunner = GetComponent<DialogueRunner>();

            if (yarnRunner == null)
                Debug.LogError("[DKDialogueRunner] Không tìm thấy Yarn DialogueRunner!", this);
        }

        private void OnEnable()
        {
            if (yarnRunner != null)
            {
                yarnRunner.onDialogueComplete.AddListener(HandleDialogueComplete);
                yarnRunner.onNodeStart.AddListener(HandleNodeStart);
            }
        }

        private void OnDisable()
        {
            if (yarnRunner != null)
            {
                yarnRunner.onDialogueComplete.RemoveListener(HandleDialogueComplete);
                yarnRunner.onNodeStart.RemoveListener(HandleNodeStart);
            }
        }

        // ──────────────────────────────────────────────────────────────────
        private void HandleNodeStart(string nodeName)
        {
            OnDialogueStarted?.Invoke(nodeName);
        }

        private void HandleDialogueComplete()
        {
            // Reset HUD immediately so UI hides right away.
            MvHud_Talk.Instance?.ResetAndHideImmediate();

            // Invoke OnDialogueEnded on the next frame to avoid the same-frame
            // input re-read (e.g., Space -> also triggers player jump).
            StartCoroutine(InvokeDialogueEndedNextFrame());
        }

        private IEnumerator InvokeDialogueEndedNextFrame()
        {
            yield return null;
            try
            {
                OnDialogueEnded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DKDialogueRunner] Exception invoking OnDialogueEnded: {ex}");
            }
        }

        /// <summary>Emergency stop (scene unload, player death)</summary>
        public void StopDialogueImmediately()
        {
            if (yarnRunner != null && IsRunning)
                yarnRunner.Stop();
        }
    }
}
