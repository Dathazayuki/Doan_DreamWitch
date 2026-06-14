using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DreamKnight.Player;
using Project.UI;

namespace DreamKnight.UI
{
    [DisallowMultipleComponent]
    public class SpellBookSelectionUI : MonoBehaviour
    {
        private static SpellBookSelectionUI instance;
        public static SpellBookSelectionUI Instance => instance;

        [Header("UI Panels")]
        [SerializeField] private GameObject uiPanelRoot;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Book Slots (3 Slots)")]
        [SerializeField] private Image[] bookIcons = new Image[3];
        [SerializeField] private TextMeshProUGUI[] bookNames = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] bookDescriptions = new TextMeshProUGUI[3];
        [SerializeField] private Button[] bookButtons = new Button[3];

        [Header("Inspect Button")]
        [SerializeField] private Button inspectStatusButton;

        private SpellBookSO[] currentBooks;
        private Action<int> onSelectCallback;
        private PlayerController currentPlayer;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            if (uiPanelRoot != null)
            {
                uiPanelRoot.SetActive(false);
            }

            // Setup button click listeners
            for (int i = 0; i < bookButtons.Length; i++)
            {
                int index = i; // capture index
                if (bookButtons[i] != null)
                {
                    bookButtons[i].onClick.AddListener(() => OnBookClicked(index));
                }
            }

            if (inspectStatusButton != null)
            {
                inspectStatusButton.onClick.AddListener(OnInspectStatusClicked);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void Open(SpellBookSO[] books, PlayerController player, Action<int> onSelected)
        {
            if (books == null || books.Length < 3 || player == null)
            {
                Debug.LogWarning("[SpellBookSelectionUI] Invalid Open parameters.");
                return;
            }

            currentBooks = books;
            currentPlayer = player;
            onSelectCallback = onSelected;

            // Fill UI details
            for (int i = 0; i < 3; i++)
            {
                if (books[i] != null)
                {
                    if (bookNames[i] != null) bookNames[i].text = books[i].displayName;
                    if (bookIcons[i] != null) bookIcons[i].sprite = books[i].icon;
                    if (bookDescriptions[i] != null) bookDescriptions[i].text = books[i].description;
                    if (bookButtons[i] != null) bookButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    if (bookButtons[i] != null) bookButtons[i].gameObject.SetActive(false);
                }
            }

            if (uiPanelRoot != null)
            {
                uiPanelRoot.SetActive(true);
            }

            // Disable player input during selection
            currentPlayer.Input?.DisableInput();

            // Set UI State in UIStateManager if exists
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.Open(UIState.None); // close other menus
            }
        }

        private void OnBookClicked(int index)
        {
            if (currentBooks == null || index < 0 || index >= currentBooks.Length || currentBooks[index] == null)
                return;

            Debug.Log($"[SpellBookSelectionUI] Selected SpellBook: {currentBooks[index].displayName} (Index: {index})");

            // Apply SpellBook to player
            if (currentPlayer != null && currentPlayer.Stats != null)
            {
                currentPlayer.Stats.SetActiveSpellBook(currentBooks[index]);
            }

            // Re-enable player input
            if (currentPlayer != null)
            {
                currentPlayer.Input?.EnableInput();
            }

            if (uiPanelRoot != null)
            {
                uiPanelRoot.SetActive(false);
            }

            // Trigger callback
            onSelectCallback?.Invoke(index);
            onSelectCallback = null;
            currentBooks = null;
            currentPlayer = null;
        }

        private void OnInspectStatusClicked()
        {
            // Toggle MainMenu with Status (index 1) active
            var menuController = FindAnyObjectByType<MenuMain2Controller>();
            if (menuController != null)
            {
                menuController.OnButtonClicked(1);
            }
        }

    }
}
