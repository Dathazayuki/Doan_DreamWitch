using DreamKnight.Systems.SaveLoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.UI
{
    public class SaveSlotButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject rootSave;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI sceneText;
        [SerializeField] private TextMeshProUGUI playTimeText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI emptyText;

        public Button Button => button != null ? button : GetComponent<Button>();

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        public void Bind(int slotIndex, SaveSlotSummary summary)
        {
            bool hasSave = summary != null && summary.hasSave;

            if (rootSave != null)
                rootSave.SetActive(hasSave);

            if (emptyText != null)
                emptyText.gameObject.SetActive(!hasSave);

            if (titleText != null)
                titleText.text = hasSave ? summary.displayName : "New Game";

            if (sceneText != null)
                sceneText.text = string.Empty;

            if (playTimeText != null)
                playTimeText.text = hasSave ? summary.playTimeText : string.Empty;

            if (goldText != null)
                goldText.text = hasSave ? summary.gold.ToString() : string.Empty;
        }
    }
}
