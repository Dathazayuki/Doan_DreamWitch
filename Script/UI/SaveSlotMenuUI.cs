using DreamKnight.Systems.SaveLoad;
using UnityEngine;
using UnityEngine.Events;

namespace DreamKnight.UI
{
    public class SaveSlotMenuUI : MonoBehaviour
    {
        [SerializeField] private TitleCanvasManager titleCanvasManager;
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
            }
        }

        private void UnbindButtons()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                SaveSlotButtonView view = slotViews[i];
                if (view != null && view.Button != null)
                    view.Button.onClick.RemoveAllListeners();
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

            titleCanvasManager?.LoadGameplaySceneFromSaveMenu();
        }

        private void ResolveReferences()
        {
            if (titleCanvasManager == null)
                titleCanvasManager = GetComponentInParent<TitleCanvasManager>();
        }
    }
}
