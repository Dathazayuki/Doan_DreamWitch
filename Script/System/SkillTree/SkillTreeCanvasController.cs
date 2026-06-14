using System.Collections;
using DreamKnight.UI;
using UnityEngine;
using Yarn.Unity;

namespace DreamKnight.Systems.SkillTree
{
    public class SkillTreeCanvasController : MonoBehaviour
    {
        private static SkillTreeCanvasController instance;

        [Header("References")]
        [SerializeField] private GameObject skillTreeCanvasRoot;
        [SerializeField] private SkillTreeUI skillTreeUI;
        [SerializeField] private UIStateManager uiStateManager;

        [Header("Dialogue")]
        [SerializeField] private bool hideDialogueHudOnOpen = true;
        [SerializeField] private DreamKnight.Systems.Dialogue.MvHud_Talk dialogueHud;

        private bool skillTreeOpen;
        private int lastEscConsumeFrame = -1;

        public static SkillTreeCanvasController Instance => instance;
        public bool IsSkillTreeOpen => skillTreeOpen;
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
            if (!skillTreeOpen)
                return;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null && !uiStateManager.IsOpen(UIState.SkillTree))
            {
                skillTreeOpen = false;
                return;
            }

            if (WasClosePressed())
            {
                lastEscConsumeFrame = Time.frameCount;
                CloseSkillTree();
            }
        }

        [YarnCommand("open_skill_tree")]
        public static IEnumerator OpenSkillTreeCommand()
        {
            if (Instance == null)
                yield break;

            yield return Instance.OpenSkillTreeRoutine();
        }

        [YarnCommand("close_skill_tree")]
        public static void CloseSkillTreeCommand()
        {
            Instance?.CloseSkillTree();
        }

        public void OpenSkillTree()
        {
            if (skillTreeOpen)
                return;

            StartCoroutine(OpenSkillTreeRoutine());
        }

        public void CloseSkillTree()
        {
            if (!skillTreeOpen)
                return;

            skillTreeOpen = false;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null)
                uiStateManager.Close(UIState.SkillTree);
            else if (skillTreeCanvasRoot != null)
                skillTreeCanvasRoot.SetActive(false);
        }

        private IEnumerator OpenSkillTreeRoutine()
        {
            if (skillTreeOpen)
                yield break;

            skillTreeOpen = true;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null)
            {
                uiStateManager.Close(UIState.Talk);
                uiStateManager.Open(UIState.SkillTree);
            }
            else if (skillTreeCanvasRoot != null)
                skillTreeCanvasRoot.SetActive(true);

            if (skillTreeUI != null)
                skillTreeUI.Refresh();

            if (hideDialogueHudOnOpen)
            {
                if (dialogueHud == null)
                    dialogueHud = DreamKnight.Systems.Dialogue.MvHud_Talk.Instance;

                dialogueHud?.HideImmediate();
            }

            yield return new WaitUntil(() => !skillTreeOpen);
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
