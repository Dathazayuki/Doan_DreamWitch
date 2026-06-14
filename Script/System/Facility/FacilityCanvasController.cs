using System.Collections;
using DreamKnight.UI;
using UnityEngine;
using Yarn.Unity;

namespace DreamKnight.Systems.Facility
{
    public class FacilityCanvasController : MonoBehaviour
    {
        private static FacilityCanvasController instance;

        [Header("References")]
        [SerializeField] private GameObject facilityCanvasRoot;
        [SerializeField] private FacilityUI facilityUI;
        [SerializeField] private UIStateManager uiStateManager;

        [Header("Dialogue")]
        [SerializeField] private bool hideDialogueHudOnOpen = true;
        [SerializeField] private DreamKnight.Systems.Dialogue.MvHud_Talk dialogueHud;

        private bool facilityOpen;
        private int lastEscConsumeFrame = -1;

        public static FacilityCanvasController Instance => instance;
        public bool IsFacilityOpen => facilityOpen;
        public bool ConsumedEscThisFrame => Time.frameCount == lastEscConsumeFrame;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (!facilityOpen)
                return;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null && !uiStateManager.IsOpen(UIState.Facility))
            {
                facilityOpen = false;
                return;
            }

            if (WasClosePressed())
            {
                lastEscConsumeFrame = Time.frameCount;
                CloseFacility();
            }
        }

        [YarnCommand("open_facility")]
        public static IEnumerator OpenFacilityCommand()
        {
            if (Instance == null)
                yield break;

            yield return Instance.OpenFacilityRoutine();
        }

        [YarnCommand("close_facility")]
        public static void CloseFacilityCommand()
        {
            Instance?.CloseFacility();
        }

        public void OpenFacility()
        {
            if (facilityOpen)
                return;

            StartCoroutine(OpenFacilityRoutine());
        }

        public void CloseFacility()
        {
            if (!facilityOpen)
                return;

            facilityOpen = false;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null)
                uiStateManager.Close(UIState.Facility);
            else if (facilityCanvasRoot != null)
                facilityCanvasRoot.SetActive(false);
        }

        private IEnumerator OpenFacilityRoutine()
        {
            if (facilityOpen)
                yield break;

            facilityOpen = true;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null)
            {
                uiStateManager.Close(UIState.Talk);
                uiStateManager.Open(UIState.Facility);
            }
            else if (facilityCanvasRoot != null)
                facilityCanvasRoot.SetActive(true);

            if (facilityUI != null)
                facilityUI.Refresh();

            if (hideDialogueHudOnOpen)
            {
                if (dialogueHud == null)
                    dialogueHud = DreamKnight.Systems.Dialogue.MvHud_Talk.Instance;

                dialogueHud?.HideImmediate();
            }

            yield return new WaitUntil(() => !facilityOpen);
        }

        private static bool WasClosePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
