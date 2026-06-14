using UnityEngine;
using Yarn.Unity;

namespace DreamKnight.Systems.Dialogue
{
    /// <summary>
    /// Bridge cho Yarn Spinner v2.4.0: nhận line/options từ Yarn và chuyển sang MvHud_Talk.
    /// Implement DialogueViewBase (callback-based API).
    /// 
    /// KHÔNG CÓ WATCHDOG: Yarn DialogueComplete sẽ gọi trực tiếp, không qua wrapper.
    /// </summary>
    [DisallowMultipleComponent]
    public class YarnDialogueView : DialogueViewBase
    {
        [Header("UI Reference")]
        [Tooltip("Kéo MvHud_Talk từ Canvas vào đây")]
        [SerializeField] private MvHud_Talk hudTalk;

        private System.Action currentLineFinished;
        private System.Action<int> currentOptionFinished;
        private bool waitingForLine = false;
        private bool waitingForOptions = false;
        private bool isBound = false;

        private void OnEnable()
        {
            EnsureHudTalkBound();
        }

        private void OnDisable()
        {
            UnbindHudTalk();
        }

        private void EnsureHudTalkBound()
        {
            if (hudTalk == null)
            {
                hudTalk = MvHud_Talk.Instance;
            }

            if (hudTalk != null && !isBound)
            {
                hudTalk.OnUserRequestedAdvance += HandleUserAdvance;
                hudTalk.OnOptionSelected += HandleOptionSelected;
                isBound = true;
            }
        }

        public override void RunLine(LocalizedLine dialogueLine, System.Action onDialogueLineFinished)
        {
            EnsureHudTalkBound();

            string speaker = dialogueLine.CharacterName ?? string.Empty;
            string lineText = dialogueLine.TextWithoutCharacterName.Text ?? string.Empty;

            currentLineFinished = onDialogueLineFinished;
            waitingForLine = true;
            waitingForOptions = false;

            if (hudTalk != null)
            {
                hudTalk.ShowLine(speaker, lineText);
            }
            else
            {
                waitingForLine = false;
                onDialogueLineFinished?.Invoke();
            }
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, System.Action<int> onOptionSelected)
        {
            EnsureHudTalkBound();
            currentOptionFinished = onOptionSelected;
            waitingForOptions = true;
            waitingForLine = false;

            if (hudTalk != null)
            {
                string[] texts = new string[dialogueOptions.Length];
                bool[] available = new bool[dialogueOptions.Length];

                for (int i = 0; i < dialogueOptions.Length; i++)
                {
                    texts[i] = dialogueOptions[i].Line.TextWithoutCharacterName.Text;
                    available[i] = dialogueOptions[i].IsAvailable;
                }


                hudTalk.ShowOptions(texts, available);
            }
            else
            {
                waitingForOptions = false;
                onOptionSelected?.Invoke(0);
            }
        }

        public override void DialogueStarted()
        {
            EnsureHudTalkBound();
            hudTalk?.ResetAndHideImmediate();
        }

        public override void DialogueComplete()
        {
            waitingForLine = false;
            waitingForOptions = false;
            hudTalk?.ResetAndHideImmediate();
        }

        public override void DismissLine(System.Action onDismissalComplete)
        {

            onDismissalComplete?.Invoke();
        }

        private void HandleUserAdvance()
        {
                var lineFinishedCallback = currentLineFinished;
                currentLineFinished = null;
                waitingForLine = false;
                lineFinishedCallback.Invoke();
        }

        private void HandleOptionSelected(int optionIndex)
        {
            if (waitingForOptions && currentOptionFinished != null)
            {
                waitingForOptions = false;
                currentOptionFinished.Invoke(optionIndex);
                currentOptionFinished = null;
            }
        }

        private void UnbindHudTalk()
        {
            if (hudTalk == null || !isBound)
                return;

            hudTalk.OnUserRequestedAdvance -= HandleUserAdvance;
            hudTalk.OnOptionSelected -= HandleOptionSelected;
            isBound = false;
        }
    }
}
