using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.Facility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.UI
{
    public class PlayerToolHudUI : MonoBehaviour
    {
        [SerializeField] private ToolEquipSO toolEquip;
        [SerializeField] private FacilityManager facilityManager;
        [SerializeField] private Image toolImage;
        [SerializeField] private TextMeshProUGUI countText;

        private void OnEnable()
        {
            ResolveReferences();

            if (toolEquip != null)
            {
                toolEquip.OnEquipmentChanged += UpdateToolDisplay;
            }

            if (facilityManager != null)
            {
                facilityManager.OnAppliedStatsChanged += HandleFacilityStatsChanged;
                SyncToolCapacityFromFacility(facilityManager.CurrentStats);
            }

            UpdateToolDisplay();
        }

        private void OnDisable()
        {
            if (toolEquip != null)
            {
                toolEquip.OnEquipmentChanged -= UpdateToolDisplay;
            }

            if (facilityManager != null)
            {
                facilityManager.OnAppliedStatsChanged -= HandleFacilityStatsChanged;
            }
        }

        private void ResolveReferences()
        {
            if (facilityManager == null)
                facilityManager = FindAnyObjectByType<FacilityManager>();

            if (toolEquip == null && facilityManager != null)
                toolEquip = facilityManager.ToolEquip;
        }

        private void HandleFacilityStatsChanged(FacilityAppliedStats stats)
        {
            SyncToolCapacityFromFacility(stats);
            UpdateToolDisplay();
        }

        private void SyncToolCapacityFromFacility(FacilityAppliedStats stats)
        {
            if (toolEquip == null)
                return;

            toolEquip.SetFacilitySlotBonus(stats.toolCapacityBonus);
        }

        private void UpdateToolDisplay()
        {
            if (toolEquip == null)
                return;

            // Count equipped tools
            int equippedCount = 0;
            ItemDefinitionSO currentTool = null;

            for (int i = 0; i < toolEquip.SlotCount; i++)
            {
                ItemDefinitionSO item = toolEquip.GetSlotItem(i);
                if (item != null)
                {
                    equippedCount++;
                    if (currentTool == null)
                        currentTool = item;
                }
            }

            // Update image
            if (toolImage != null)
            {
                if (currentTool != null && currentTool.Icon != null)
                {
                    toolImage.sprite = currentTool.Icon;
                    toolImage.enabled = true;
                }
                else
                {
                    toolImage.enabled = false;
                }
            }

            // Update count text
            if (countText != null)
            {
                countText.text = $"{equippedCount}/{toolEquip.SlotCount}";
            }
        }
    }
}
