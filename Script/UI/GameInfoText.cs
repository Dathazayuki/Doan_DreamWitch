using System;
using DreamKnight.Systems.SaveLoad;
using TMPro;
using UnityEngine;

namespace DreamKnight.UI
{
    [DisallowMultipleComponent]
    public class GameInfoText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI targetText;
        [SerializeField] private int slotIndex = -1;
        [SerializeField] private float refreshInterval = 1f;
        [SerializeField] private string emptyText = "SAVE SLOT\n - -- -\n\nLAST SAVE\n - -- -\n\nPLAY TIME\n- 0h 00m 00s -";

        private float refreshTimer;

        private void Reset()
        {
            targetText = GetComponent<TextMeshProUGUI>();
        }

        private void Awake()
        {
            if (targetText == null)
                targetText = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            refreshTimer = 0f;
            Refresh();
        }

        private void Update()
        {
            if (refreshInterval <= 0f)
                return;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
                return;

            refreshTimer = refreshInterval;
            Refresh();
        }

        public void SetSlotIndex(int value)
        {
            slotIndex = value;
            Refresh();
        }

        public void Refresh()
        {
            if (targetText == null)
                return;

            int resolvedSlotIndex = ResolveSlotIndex();
            if (resolvedSlotIndex < 0 || !SaveSlotFileUtility.TryLoadSlotData(resolvedSlotIndex, out GameSaveData data))
            {
                targetText.text = emptyText;
                return;
            }

            int displaySlot = resolvedSlotIndex + 1;
            targetText.text =
                $"SAVE SLOT\n - {displaySlot} -\n\n" +
                $"LAST SAVE\n - {FormatLastSave(data.updatedUtcTicks)} -\n\n" +
                $"PLAY TIME\n- {FormatPlayTime(data.playTimeSeconds)} -";
        }

        private int ResolveSlotIndex()
        {
            if (slotIndex >= 0)
                return slotIndex;

            GameSaveManager saveManager = GameSaveManager.Instance;
            if (saveManager != null && saveManager.ActiveSlotIndex >= 0)
                return saveManager.ActiveSlotIndex;

            if (SaveSlotRuntimeContext.HasPendingSlot)
                return SaveSlotRuntimeContext.PendingSlotIndex;

            return -1;
        }

        private static string FormatLastSave(long updatedUtcTicks)
        {
            if (updatedUtcTicks <= 0)
                return "--";

            DateTime updatedUtc = new DateTime(updatedUtcTicks, DateTimeKind.Utc);
            TimeSpan elapsed = DateTime.UtcNow - updatedUtc;
            if (elapsed.TotalSeconds < 1d)
                return "Just Now";

            if (elapsed.TotalMinutes < 1d)
                return $"{Mathf.FloorToInt((float)elapsed.TotalSeconds)}s Ago";

            if (elapsed.TotalHours < 1d)
                return $"{Mathf.FloorToInt((float)elapsed.TotalMinutes)}m Ago";

            if (elapsed.TotalDays < 1d)
                return $"{Mathf.FloorToInt((float)elapsed.TotalHours)}h Ago";

            return $"{Mathf.FloorToInt((float)elapsed.TotalDays)}d Ago";
        }

        private static string FormatPlayTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int hours = totalSeconds / 3600;
            int minutes = totalSeconds % 3600 / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{hours}h {minutes:00}m {remainingSeconds:00}s";
        }
    }
}
