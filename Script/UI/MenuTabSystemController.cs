using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DreamKnight.UI;
using DreamKnight.Systems.Zone;
using DreamKnight.Player;
using DreamKnight.Systems.Scene;
using DreamKnight.Systems.SaveLoad;

namespace Project.UI
{
    /// <summary>
    /// Controls the MenuMain_TabSystem menu with 4 buttons:
    /// - Setting: Opens setting menu
    /// - Từ bỏ khám phá: Abandon exploration (show confirm panel)
    /// - Quay lại tiêu đề: Return to title scene
    /// - Thoát trò chơi: Exit game
    /// 
    /// Each button has DecoL and DecoR children that show on hover and hide otherwise.
    /// </summary>
    public class MenuTabSystemController : MonoBehaviour
    {
        [System.Serializable]
        public class SystemButtonConfig
        {
            [Tooltip("The button")]
            public Button button;

            [Tooltip("Decorative object on the left")]
            public GameObject decoL;

            [Tooltip("Decorative object on the right")]
            public GameObject decoR;
        }

        [Header("Button Configs")]
        [SerializeField] private SystemButtonConfig settingButtonConfig;
        [SerializeField] private SystemButtonConfig abandonButtonConfig;
        [SerializeField] private SystemButtonConfig backToTitleButtonConfig;
        [SerializeField] private SystemButtonConfig exitButtonConfig;

        [Header("Scene Names")]
        [SerializeField] private string churchSceneName = "Church";
        [SerializeField] private string titleSceneName = "Title";

        [Header("External References")]
        [SerializeField] private MenuMain2Controller menuController;
        [SerializeField] private ConfirmPanelController confirmPanel;
        [SerializeField] private TitleSettingMenuController settingMenuController;

        private void OnEnable()
        {
            RegisterButtonEvents();
        }

        private void OnDisable()
        {
            UnregisterButtonEvents();
        }

        private void RegisterButtonEvents()
        {
            // Setting button
            if (settingButtonConfig.button != null)
            {
                settingButtonConfig.button.onClick.AddListener(OnSettingClicked);
                RegisterHoverEvents(settingButtonConfig);
            }

            // Abandon button
            if (abandonButtonConfig.button != null)
            {
                abandonButtonConfig.button.onClick.AddListener(OnAbandonClicked);
                RegisterHoverEvents(abandonButtonConfig);
            }

            // Back to title button
            if (backToTitleButtonConfig.button != null)
            {
                backToTitleButtonConfig.button.onClick.AddListener(OnBackToTitleClicked);
                RegisterHoverEvents(backToTitleButtonConfig);
            }

            // Exit button
            if (exitButtonConfig.button != null)
            {
                exitButtonConfig.button.onClick.AddListener(OnExitClicked);
                RegisterHoverEvents(exitButtonConfig);
            }
        }

        private void UnregisterButtonEvents()
        {
            if (settingButtonConfig.button != null)
            {
                settingButtonConfig.button.onClick.RemoveListener(OnSettingClicked);
                UnregisterHoverEvents(settingButtonConfig);
            }

            if (abandonButtonConfig.button != null)
            {
                abandonButtonConfig.button.onClick.RemoveListener(OnAbandonClicked);
                UnregisterHoverEvents(abandonButtonConfig);
            }

            if (backToTitleButtonConfig.button != null)
            {
                backToTitleButtonConfig.button.onClick.RemoveListener(OnBackToTitleClicked);
                UnregisterHoverEvents(backToTitleButtonConfig);
            }

            if (exitButtonConfig.button != null)
            {
                exitButtonConfig.button.onClick.RemoveListener(OnExitClicked);
                UnregisterHoverEvents(exitButtonConfig);
            }
        }

        private void RegisterHoverEvents(SystemButtonConfig config)
        {
            if (config.button == null)
                return;

            EventTrigger trigger = config.button.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = config.button.gameObject.AddComponent<EventTrigger>();

            // On pointer enter
            EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
            pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
            pointerEnterEntry.callback.AddListener((data) => OnButtonHoverEnter(config));
            trigger.triggers.Add(pointerEnterEntry);

            // On pointer exit
            EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
            pointerExitEntry.eventID = EventTriggerType.PointerExit;
            pointerExitEntry.callback.AddListener((data) => OnButtonHoverExit(config));
            trigger.triggers.Add(pointerExitEntry);
        }

        private void UnregisterHoverEvents(SystemButtonConfig config)
        {
            if (config.button == null)
                return;

            EventTrigger trigger = config.button.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.Clear();
            }
        }

        private void OnButtonHoverEnter(SystemButtonConfig config)
        {
            if (config.decoL != null)
                config.decoL.SetActive(true);
            if (config.decoR != null)
                config.decoR.SetActive(true);
        }

        private void OnButtonHoverExit(SystemButtonConfig config)
        {
            if (config.decoL != null)
                config.decoL.SetActive(false);
            if (config.decoR != null)
                config.decoR.SetActive(false);
        }

        private void OnSettingClicked()
        {
            Debug.Log("Setting clicked! settingMenuController = " + (settingMenuController != null ? "FOUND" : "NULL"));
            if (settingMenuController != null)
            {
                settingMenuController.ShowMain(resetHistory: true);
            }
        }

        private void OnAbandonClicked()
        {
            // Show confirm panel asking to abandon exploration and return to Church
            if (confirmPanel != null)
            {
                confirmPanel.Show("Bạn có chắc chắn muốn từ bỏ khám phá?", 
                    onConfirm: () => OnAbandonConfirmed(),
                    onCancel: () => OnAbandonCancelled());
            }
            else
            {
                Debug.LogWarning("[MenuTabSystemController] Confirm panel is not assigned!");
            }
        }

        private void OnAbandonConfirmed()
        {
            // QUAN TRỌNG: Không được StartCoroutine ở đây!
            // MenuTabSystemController là scene object → bị Destroy khi LoadScene
            // → coroutine bị kill giữa chừng → Player load scene nhưng không teleport được.
            //
            // Giải pháp: delegate sang PlayerController.AbandonToShrine() vì
            // PlayerController nằm trên PersistentPlayerRoot (DontDestroyOnLoad)
            // → coroutine sống sót qua toàn bộ quá trình load scene.
            if (PersistentPlayerRoot.Instance == null)
                return;

            var player = PersistentPlayerRoot.Instance.GetComponent<PlayerController>();
            if (player == null)
                return;

            player.AbandonToShrine(churchSceneName);

            // Đóng menu ngay lập tức (không cần đợi load xong)
            if (menuController != null)
                menuController.CloseAllMenus();
        }


        private void OnAbandonCancelled()
        {
            // Close confirm panel (handled by ConfirmPanelController)
        }

        private void OnBackToTitleClicked()
        {
            SaveActiveSlotIfAvailable();
            GameSession.LoadTitleAfterReset(titleSceneName);
        }

        private void OnExitClicked()
        {
            SaveActiveSlotIfAvailable();
            Application.Quit();
        }

        private void SaveActiveSlotIfAvailable()
        {
            if (GameSaveManager.Instance != null)
                GameSaveManager.Instance.SaveActiveSlot();
        }
    }
}
