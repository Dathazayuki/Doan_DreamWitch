using System.Collections;
using UnityEngine;
using Yarn.Unity;
using DreamKnight.UI;

namespace DreamKnight.Systems.Shop
{
    public class ShopCanvasController : MonoBehaviour
    {
        private static ShopCanvasController _instance;
        public static ShopCanvasController Instance => _instance;

        [Header("References")]
        [SerializeField] private GameObject shopCanvasRoot;
        [SerializeField] private ShopUI shopUI;
        [SerializeField] private UIStateManager uiStateManager;

        [Header("Dialogue")]
        [SerializeField] private bool hideDialogueHudOnOpen = true;
        [SerializeField] private DreamKnight.Systems.Dialogue.MvHud_Talk dialogueHud;

        private bool shopOpen;
        public bool IsShopOpen => shopOpen;
        private int lastEscConsumeFrame = -1;
        public bool ConsumedEscThisFrame => Time.frameCount == lastEscConsumeFrame;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            if (!shopOpen)
                return;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null && !uiStateManager.IsOpen(UIState.Shop))
            {
                shopOpen = false;
                return;
            }

            bool closePressed = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
                closePressed = keyboard.escapeKey.wasPressedThisFrame;
#else
            closePressed = Input.GetKeyDown(KeyCode.Escape);
#endif

            if (closePressed)
            {
                lastEscConsumeFrame = Time.frameCount;
                CloseShop();
            }
        }

        [YarnCommand("open_shop")]
        public static IEnumerator OpenShopCommand()
        {
            if (Instance == null)
                yield break;

            yield return Instance.OpenShopRoutine();
        }

        private IEnumerator OpenShopRoutine()
        {
            if (shopOpen)
                yield break;

            shopOpen = true;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null)
            {
                uiStateManager.Close(UIState.Talk);
                uiStateManager.Open(UIState.Shop);
            }
            else if (shopCanvasRoot != null)
                shopCanvasRoot.SetActive(true);

            if (shopUI != null)
                shopUI.Refresh();

            if (hideDialogueHudOnOpen)
            {
                if (dialogueHud == null)
                    dialogueHud = DreamKnight.Systems.Dialogue.MvHud_Talk.Instance;

                dialogueHud?.HideImmediate();
            }

            // Pause Yarn until the shop is closed.
            yield return new WaitUntil(() => !shopOpen);
        }

        [YarnCommand("close_shop")]
        public static void CloseShopCommand()
        {
            if (Instance == null)
                return;

            Instance.CloseShop();
        }

        public void CloseShop()
        {
            if (!shopOpen)
                return;

            shopOpen = false;
            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null)
                uiStateManager.Close(UIState.Shop);
            else if (shopCanvasRoot != null)
                shopCanvasRoot.SetActive(false);
        }
    }
}
