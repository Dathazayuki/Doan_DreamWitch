using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DreamKnight.UI;

namespace DreamKnight.Systems.Dialogue
{
    [DisallowMultipleComponent]
    public class MvHud_Talk : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("Dialogue Box")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueLineText;
        [SerializeField] private GameObject continueArrow;

        [Header("Options Panel")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TextMeshProUGUI[] optionButtonTexts;

        [Header("Typewriter")]
        [Tooltip("Ký tự hiển thị mỗi giây. 0 = tức thì")]
        [SerializeField] private float charsPerSecond = 45f;

        [Header("UI State")]
        [SerializeField] private UIStateManager uiStateManager;

        public event Action OnUserRequestedAdvance;
        public event Action<int> OnOptionSelected;

        private static MvHud_Talk _instance;
        public static MvHud_Talk Instance => _instance;

        private bool isTypewriting;
        private bool lineFullyDisplayed;
        private string currentFullText = string.Empty;
        private Coroutine typewriterCoroutine;
        private bool initialized;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (uiStateManager == null)
                uiStateManager = UIStateManager.Instance;
            ResetAndHideImmediate();
            initialized = true;
        }

        private void OnEnable()
        {
            if (!initialized)
            {
                ResetAndHideImmediate();
                initialized = true;
            }
            else
            {
                HideImmediate();
            }
        }

        private void Update()
        {
            if (rootCanvasGroup == null || rootCanvasGroup.alpha < 0.01f)
                return;

            if (optionsPanel != null && optionsPanel.activeSelf)
                return;

            bool advancePressed = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                advancePressed = keyboard.spaceKey.wasPressedThisFrame ||
                                 keyboard.enterKey.wasPressedThisFrame ||
                                 keyboard.eKey.wasPressedThisFrame;
            }
#else
            advancePressed = Input.GetKeyDown(KeyCode.Space) ||
                             Input.GetKeyDown(KeyCode.Return) ||
                             Input.GetKeyDown(KeyCode.E);
#endif

            if (!advancePressed)
                return;

            if (isTypewriting)
            {
                SkipTypewriter();
                return;
            }

            if (lineFullyDisplayed)
            {
                lineFullyDisplayed = false;
                if (continueArrow != null)
                    continueArrow.SetActive(false);

                try
                {
                    OnUserRequestedAdvance?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MvHud_Talk] Exception invoking OnUserRequestedAdvance: {ex}");
                }
            }
        }

        public void ShowLine(string speakerName, string lineText)
        {
            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            uiStateManager?.Open(UIState.Talk);

            if (speakerNameText != null)
                speakerNameText.text = speakerName ?? string.Empty;

            currentFullText = lineText ?? string.Empty;

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.gameObject.SetActive(true);
                rootCanvasGroup.alpha = 1f;
                rootCanvasGroup.interactable = true;
                rootCanvasGroup.blocksRaycasts = true;
            }

            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);

            typewriterCoroutine = StartCoroutine(TypewriterRoutine(currentFullText));
        }

        public void ShowOptions(string[] optionTexts, bool[] optionAvailable)
        {
            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            uiStateManager?.Open(UIState.Talk);

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.gameObject.SetActive(true);
                rootCanvasGroup.alpha = 1f;
                rootCanvasGroup.interactable = true;
                rootCanvasGroup.blocksRaycasts = true;
            }

            if (optionsPanel != null)
                optionsPanel.SetActive(true);

            Button firstAvailableButton = null;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool hasOption = optionTexts != null && i < optionTexts.Length;
                optionButtons[i].gameObject.SetActive(hasOption);

                if (!hasOption)
                    continue;

                int capturedIndex = i;

                if (optionButtonTexts != null && i < optionButtonTexts.Length && optionButtonTexts[i] != null)
                    optionButtonTexts[i].text = optionTexts[i];

                bool interactable = optionAvailable == null || i >= optionAvailable.Length || optionAvailable[i];
                optionButtons[i].interactable = interactable;

                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() =>
                {
                    if (optionsPanel != null)
                        optionsPanel.SetActive(false);

                    try
                    {
                        OnOptionSelected?.Invoke(capturedIndex);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MvHud_Talk] Exception invoking OnOptionSelected: {ex}");
                    }
                });

                if (interactable && firstAvailableButton == null)
                    firstAvailableButton = optionButtons[i];
            }

            if (firstAvailableButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstAvailableButton.gameObject);
        }

        public void HideImmediate()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            uiStateManager?.Close(UIState.Talk);

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.gameObject.SetActive(false);
                rootCanvasGroup.alpha = 0f;
                rootCanvasGroup.interactable = false;
                rootCanvasGroup.blocksRaycasts = false;
            }

            if (optionsPanel != null)
                optionsPanel.SetActive(false);

            if (continueArrow != null)
                continueArrow.SetActive(false);
        }

        public void ResetAndHideImmediate()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            isTypewriting = false;
            lineFullyDisplayed = false;
            currentFullText = string.Empty;

            if (speakerNameText != null)
                speakerNameText.text = string.Empty;

            if (dialogueLineText != null)
                dialogueLineText.text = string.Empty;

            HideImmediate();
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            isTypewriting = true;

            if (dialogueLineText != null)
                dialogueLineText.text = string.Empty;

            if (charsPerSecond <= 0f)
            {
                if (dialogueLineText != null)
                    dialogueLineText.text = fullText;
            }
            else
            {
                float delay = 1f / charsPerSecond;
                for (int i = 0; i <= fullText.Length; i++)
                {
                    if (dialogueLineText != null)
                        dialogueLineText.text = fullText.Substring(0, i);

                    yield return new WaitForSecondsRealtime(delay);
                }
            }

            isTypewriting = false;
            lineFullyDisplayed = true;
            if (continueArrow != null)
                continueArrow.SetActive(true);
        }

        private void SkipTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            if (dialogueLineText != null)
                dialogueLineText.text = currentFullText;

            isTypewriting = false;
            lineFullyDisplayed = true;
            if (continueArrow != null)
                continueArrow.SetActive(true);
        }
    }
}
