using DreamKnight.Systems.SaveLoad;
using Project.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DreamKnight.UI
{
    public class SaveSlotMenuUI : MonoBehaviour
    {
        [SerializeField] private TitleCanvasManager titleCanvasManager;
        [SerializeField] private ConfirmPanelController confirmPanel;
        [SerializeField] private SaveSlotButtonView[] slotViews = new SaveSlotButtonView[0];
        [SerializeField] private UnityEvent<int> onSlotSelected;
        [SerializeField] private UnityEvent<int> onNewGameSlotSelected;
        [SerializeField] private UnityEvent<int> onLoadSlotSelected;

        private void OnEnable()
        {
            ResolveReferences();
            BindButtons();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        public void Refresh()
        {
            ResolveReferences();

            for (int i = 0; i < slotViews.Length; i++)
            {
                SaveSlotButtonView view = slotViews[i];
                if (view == null)
                    continue;

                SaveSlotSummary summary = new SaveSlotSummary { slotIndex = i, hasSave = false };
                bool hasSave = SaveSlotFileUtility.TryGetSlotSummary(i, out summary);
                if (!hasSave)
                    summary = new SaveSlotSummary { slotIndex = i, hasSave = false };

                view.Bind(i, summary);
            }
        }

        private void BindButtons()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                SaveSlotButtonView view = slotViews[i];
                if (view == null || view.Button == null)
                    continue;

                int slotIndex = i;
                view.Button.onClick.AddListener(() => SelectSlot(slotIndex));

                if (view.DeleteButton != null)
                    view.DeleteButton.onClick.AddListener(() => RequestDeleteSlot(slotIndex));
            }
        }

        private void UnbindButtons()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                SaveSlotButtonView view = slotViews[i];
                if (view != null && view.Button != null)
                    view.Button.onClick.RemoveAllListeners();

                if (view != null && view.DeleteButton != null)
                    view.DeleteButton.onClick.RemoveAllListeners();
            }
        }

        private void SelectSlot(int slotIndex)
        {
            ResolveReferences();
            onSlotSelected?.Invoke(slotIndex);

            bool hasSave = SaveSlotFileUtility.HasSlotSave(slotIndex);
            if (hasSave)
            {
                onLoadSlotSelected?.Invoke(slotIndex);
                SaveSlotRuntimeContext.SetPendingLoad(slotIndex);
                titleCanvasManager?.LoadGameplaySceneFromSaveMenu();
                return;
            }

            onNewGameSlotSelected?.Invoke(slotIndex);
            SaveSlotFileUtility.CreateNewGameSlot(slotIndex);
            SaveSlotRuntimeContext.SetPendingNewGame(slotIndex);

            titleCanvasManager?.LoadNewGameSceneFromSaveMenu();
        }

        private void RequestDeleteSlot(int slotIndex)
        {
            if (!SaveSlotFileUtility.HasSlotSave(slotIndex))
            {
                Refresh();
                return;
            }

            ResolveReferences();

            if (confirmPanel == null)
            {
                DeleteSlot(slotIndex);
                return;
            }

            int displaySlot = slotIndex + 1;
            confirmPanel.Show(
                $"Delete Save Slot {displaySlot}?",
                () => DeleteSlot(slotIndex));
        }

        private void DeleteSlot(int slotIndex)
        {
            SaveSlotFileUtility.DeleteSlotSave(slotIndex);
            Refresh();
        }

        private void ResolveReferences()
        {
            if (titleCanvasManager == null)
                titleCanvasManager = GetComponentInParent<TitleCanvasManager>();

            if (confirmPanel == null)
                confirmPanel = FindFirstObjectByType<ConfirmPanelController>();

            if (confirmPanel == null)
            {
                ConfirmPanelController[] panels = FindObjectsByType<ConfirmPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (panels != null && panels.Length > 0)
                    confirmPanel = panels[0];
            }
        }
    }
}
