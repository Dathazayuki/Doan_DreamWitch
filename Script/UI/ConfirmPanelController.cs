using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Project.UI
{
    /// <summary>
    /// Controls a confirm panel with Yes/No buttons.
    /// Used to confirm actions like "Abandon Exploration" or "Return to Title".
    /// </summary>
    public class ConfirmPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action onConfirmCallback;
        private Action onCancelCallback;

        private void OnEnable()
        {
            if (yesButton != null)
                yesButton.onClick.AddListener(OnYesClicked);
            if (noButton != null)
                noButton.onClick.AddListener(OnNoClicked);
        }

        private void OnDisable()
        {
            if (yesButton != null)
                yesButton.onClick.RemoveListener(OnYesClicked);
            if (noButton != null)
                noButton.onClick.RemoveListener(OnNoClicked);
        }

        /// <summary>
        /// Show the confirm panel with a message and callbacks.
        /// </summary>
        public void Show(string message, Action onConfirm = null, Action onCancel = null, bool showYesButton = true)
        {
            gameObject.SetActive(true);

            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (messageText != null)
                messageText.text = message;

            if (yesButton != null)
                yesButton.gameObject.SetActive(showYesButton);

            onConfirmCallback = onConfirm;
            onCancelCallback = onCancel;
        }

        /// <summary>
        /// Hide the confirm panel.
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            gameObject.SetActive(false);

            onConfirmCallback = null;
            onCancelCallback = null;
        }

        private void OnYesClicked()
        {
            onConfirmCallback?.Invoke();
            Hide();
        }

        private void OnNoClicked()
        {
            onCancelCallback?.Invoke();
            Hide();
        }
    }
}
