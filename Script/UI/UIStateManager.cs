using UnityEngine;

namespace DreamKnight.UI
{
    public enum UIState
    {
        None,
        MenuMain,
        Shop,
        Facility,
        SkillTree,
        Talk,
        Map
    }

    [System.Serializable]
    public class UIStateEntry
    {
        public UIState state;
        public GameObject canvasRoot;
    }

    /// <summary>
    /// Central manager for UI canvas states.
    /// Only one canvas can be open at a time.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIStateManager : MonoBehaviour
    {
        [SerializeField] private UIStateEntry[] entries = new UIStateEntry[0];

        private static UIStateManager instance;
        private UIState currentState = UIState.None;

        public static UIStateManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindAnyObjectByType<UIStateManager>();
                return instance;
            }
        }

        public UIState CurrentState => currentState;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            InitializeCurrentState();
        }

        public void Open(UIState state)
        {
            if (state == UIState.None)
            {
                CloseAll();
                return;
            }

            if (currentState == state && IsOpen(state))
                return;

            if (currentState != UIState.None && currentState != state && IsOpen(currentState))
                return;
            SetStateActive(state, true);
            currentState = state;
        }

        public void Close(UIState state)
        {
            if (state == UIState.None)
            {
                CloseAll();
                return;
            }

            SetStateActive(state, false);

            if (currentState == state)
                currentState = UIState.None;
        }

        public void Toggle(UIState state)
        {
            if (IsOpen(state))
                Close(state);
            else
                Open(state);
        }

        public void CloseAll()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].canvasRoot != null)
                    entries[i].canvasRoot.SetActive(false);
            }

            currentState = UIState.None;
        }

        public bool IsOpen(UIState state)
        {
            GameObject canvas = GetCanvas(state);
            return canvas != null && canvas.activeSelf;
        }

        public bool IsAnyUIPanelActive()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].canvasRoot != null && entries[i].canvasRoot.activeSelf)
                    return true;
            }

            return false;
        }

        private void InitializeCurrentState()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].canvasRoot != null && entries[i].canvasRoot.activeSelf)
                {
                    currentState = entries[i].state;
                    CloseAllExcept(entries[i].state);
                    return;
                }
            }

            currentState = UIState.None;
        }

        private void CloseAllExcept(UIState state)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null || entries[i].canvasRoot == null)
                    continue;

                if (entries[i].state == state)
                    continue;

                entries[i].canvasRoot.SetActive(false);
            }
        }

        private void SetStateActive(UIState state, bool active)
        {
            GameObject canvas = GetCanvas(state);
            if (canvas != null)
                canvas.SetActive(active);
        }

        private GameObject GetCanvas(UIState state)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].state == state)
                    return entries[i].canvasRoot;
            }

            return null;
        }
    }
}
