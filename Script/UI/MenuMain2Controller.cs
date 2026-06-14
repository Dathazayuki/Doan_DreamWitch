using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DreamKnight.UI;

namespace Project.UI
{
    /// <summary>
    /// Controls the top-level MenuMain2 UI.
    /// Each button has its own Deco child object and UI menu panel.
    /// When a button is clicked, its menu and deco are shown, others are hidden.
    /// </summary>
    [System.Serializable]
    public class MenuButtonConfig
    {
        [Tooltip("The button that triggers this menu")]
        public UnityEngine.UI.Button button;

        [Tooltip("The UI menu panel to show when this button is clicked")]
        public GameObject menuPanel;

        [Tooltip("The decorative background as a child of this button")]
        public GameObject decoBg;
    }

    public class MenuMain2Controller : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Root object containing the entire MenuMain2 UI (enable/disable to show/hide)")]
        public GameObject menuRoot;

        [Tooltip("List of menu button configs: Đồ đạc, Trạng thái, Bản đồ, Giúp đỡ, Hệ thống")]
        public List<MenuButtonConfig> buttonConfigs = new List<MenuButtonConfig>();

        [SerializeField] private UIStateManager uiStateManager;

        // Currently opened menu index, -1 = none
        private int openedIndex = -1;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void Awake()
        {
            if (menuRoot == null)
                menuRoot = gameObject;

            if (uiStateManager == null)
                uiStateManager = UIStateManager.Instance;


            // Ensure all menus and decos are closed initially
            for (int i = 0; i < buttonConfigs.Count; i++)
            {
                if (buttonConfigs[i].menuPanel != null)
                    buttonConfigs[i].menuPanel.SetActive(false);
                if (buttonConfigs[i].decoBg != null)
                    buttonConfigs[i].decoBg.SetActive(false);

            }

            // Register button listeners
            for (int i = 0; i < buttonConfigs.Count; i++)
            {
                int index = i; // Capture for closure
                if (buttonConfigs[i].button != null)
                {
                    buttonConfigs[i].button.onClick.AddListener(() => 
                    {
                        OnButtonClicked(index);
                    });
                }
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CloseMenuRootOnSceneChange();
        }

        private void CloseMenuRootOnSceneChange()
        {
            CloseAllMenus();
            uiStateManager?.Close(UIState.MenuMain);
        }

        /// <summary>
        /// Toggle the whole MenuMain2 UI (e.g. when pressing Esc).
        /// When opening, always show Inventory tab (tab 0) as the initial state.
        /// When closing, reset back to initial state.
        /// </summary>
        public void ToggleMenuRoot()
        {
            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            bool willOpen = uiStateManager == null || !uiStateManager.IsOpen(UIState.MenuMain);
            if (uiStateManager != null)
            {
                if (willOpen)
                    uiStateManager.Open(UIState.MenuMain);
                else
                    uiStateManager.Close(UIState.MenuMain);
            }

            if (willOpen)
            {
                // Always open tab 0 (Inventory) as the initial state
                if (buttonConfigs.Count > 0)
                {
                    openedIndex = -1;
                    OnButtonClicked(0);
                }
            }
            else
            {
                // When closing, reset all menus to initial state
                CloseAllMenus();
            }
        }

        /// <summary>
        /// Open the menu at `index`. If already open it will close it.
        /// Shows the menu panel and its deco, hides others.
        /// </summary>
        public void OnButtonClicked(int index)
        {

            if (index < 0 || index >= buttonConfigs.Count)
            {
                return;
            }

            // If menu root is closed, open it first via UIStateManager
            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null && !uiStateManager.IsOpen(UIState.MenuMain))
                uiStateManager.Open(UIState.MenuMain);

            if (openedIndex == index)
            {
                // Keep the current panel open when clicking the same button again.
                return;
            }

            // Activate requested menu and deco, deactivate others
            for (int i = 0; i < buttonConfigs.Count; i++)
            {
                bool isActive = (i == index);
                if (buttonConfigs[i].menuPanel != null)
                {
                    buttonConfigs[i].menuPanel.SetActive(isActive);
                }
                if (buttonConfigs[i].decoBg != null)
                {
                    buttonConfigs[i].decoBg.SetActive(isActive);
                }
            }

            openedIndex = index;
        }

        /// <summary>
        /// Close all menu panels and their decos.
        /// </summary>
        public void CloseAllMenus()
        {
            for (int i = 0; i < buttonConfigs.Count; i++)
            {
                if (buttonConfigs[i].menuPanel != null)
                    buttonConfigs[i].menuPanel.SetActive(false);
                if (buttonConfigs[i].decoBg != null)
                    buttonConfigs[i].decoBg.SetActive(false);
            }

            openedIndex = -1;
        }

        /// <summary>
        /// Helper to check whether MenuMain2 is currently open (root active).
        /// </summary>
        public bool IsOpen => uiStateManager != null && uiStateManager.IsOpen(UIState.MenuMain);
    }
}
