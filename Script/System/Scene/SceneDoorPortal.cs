using System.Collections.Generic;
using DreamKnight.Player;
using DreamKnight.UI;
using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.SaveLoad;
using Project.UI;
using UnityEngine;

namespace DreamKnight.Systems.Scene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class SceneDoorPortal : MonoBehaviour
    {
        [Header("Door Routing")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private string targetDoorId;

        [Header("Lock Status")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private ItemDefinitionSO requiredKeyItem;
        [SerializeField] private int requiredKeyCount = 1;
        [SerializeField] private InventoryStateSO playerInventoryState;

        [Header("Lock Visuals")]
        [SerializeField] private string uniqueDoorId;
        [SerializeField] private GameObject lockedVisualObject;
        [SerializeField] private GameObject unlockedVisualObject;

        [Header("Dialogue / Prompts")]
        [SerializeField] private string lockedPromptFormat = "{icon} Unlock Door";
        [SerializeField] private string confirmUnlockMessage = "Use {0} {1} to unlock this door?";
        [SerializeField] private string notEnoughKeysMessage = "This door is locked. Requires {0} {1} (Current: {2}).";

        [Header("Input")]
        [SerializeField] private float requiredUpInput = 0.5f;
        [SerializeField] private KeyCode fallbackUpKey = KeyCode.W;

        [Header("Prompt")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private string promptFormat = "{icon}";

        private readonly HashSet<int> waitingReleasePlayers = new HashSet<int>();
        private bool isConfirming = false;

        // Persist unlocked doors across scene transitions
        private static readonly HashSet<string> unlockedDoors = new HashSet<string>();

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;

            if (string.IsNullOrEmpty(uniqueDoorId))
            {
                uniqueDoorId = name + "_" + System.Guid.NewGuid().ToString().Substring(0, 8);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        private void Start()
        {
            if (!string.IsNullOrEmpty(uniqueDoorId) && unlockedDoors.Contains(uniqueDoorId))
            {
                isLocked = false;
            }
            UpdateVisuals();
        }

        public static void CaptureUnlockedDoorIds(List<string> output)
        {
            if (output == null)
                return;

            output.Clear();
            foreach (string doorId in unlockedDoors)
            {
                if (!string.IsNullOrWhiteSpace(doorId))
                    output.Add(doorId);
            }
        }

        public static void LoadUnlockedDoorIds(IEnumerable<string> doorIds)
        {
            unlockedDoors.Clear();

            if (doorIds != null)
            {
                foreach (string doorId in doorIds)
                {
                    if (!string.IsNullOrWhiteSpace(doorId))
                        unlockedDoors.Add(doorId);
                }
            }

            SceneDoorPortal[] doors = Object.FindObjectsByType<SceneDoorPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++)
                doors[i]?.ApplySavedDoorState();
        }

        private void ApplySavedDoorState()
        {
            if (!string.IsNullOrEmpty(uniqueDoorId) && unlockedDoors.Contains(uniqueDoorId))
                isLocked = false;

            UpdateVisuals();
        }

        private void UnlockDoor()
        {
            isLocked = false;
            if (!string.IsNullOrEmpty(uniqueDoorId))
            {
                unlockedDoors.Add(uniqueDoorId);
            }
            UpdateVisuals();
            GameAutoSave.Request("door_unlock");
        }

        private void UpdateVisuals()
        {
            if (lockedVisualObject != null)
            {
                lockedVisualObject.SetActive(isLocked);
            }

            if (unlockedVisualObject != null)
            {
                unlockedVisualObject.SetActive(!isLocked);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other) || player.IsTransformed)
                return;

            if (UIManager.Instance == null)
                return;

            Transform anchor = promptAnchor != null ? promptAnchor : transform;
            string format = isLocked ? lockedPromptFormat : promptFormat;
            UIManager.Instance.ShowInteractPrompt(this, anchor, PlayerInput.BindableAction.MoveUp, format);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other) || player.IsTransformed)
                return;

            if (SceneTransitionManager.Instance == null)
                return;

            if (UIStateManager.Instance != null && UIStateManager.Instance.IsAnyUIPanelActive())
                return;

            if (isConfirming)
                return;

            int playerId = player.gameObject.GetInstanceID();
            bool wantsEnterDoor = IsUpPressed(player.Input);

            if (!wantsEnterDoor)
            {
                waitingReleasePlayers.Remove(playerId);
                return;
            }

            if (waitingReleasePlayers.Contains(playerId))
                return;

            waitingReleasePlayers.Add(playerId);

            if (isLocked)
            {
                TriggerUnlockProcess(player);
            }
            else
            {
                SceneTransitionManager.Instance.RequestDoorTransition(player, targetSceneName, targetDoorId);
            }
        }

        private void TriggerUnlockProcess(PlayerController player)
        {
            ConfirmPanelController confirmPanel = Object.FindFirstObjectByType<ConfirmPanelController>();
            if (confirmPanel == null)
            {
                ConfirmPanelController[] panels = Object.FindObjectsByType<ConfirmPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (panels != null && panels.Length > 0)
                {
                    confirmPanel = panels[0];
                }
            }

            if (confirmPanel == null)
            {
                Debug.LogWarning("[SceneDoorPortal] ConfirmPanelController not found in scene (even inactive ones)!");
                waitingReleasePlayers.Remove(player.gameObject.GetInstanceID());
                return;
            }

            isConfirming = true;
            int currentKeys = playerInventoryState != null ? playerInventoryState.GetQuantity(requiredKeyItem) : 0;
            string keyName = requiredKeyItem != null ? requiredKeyItem.DisplayName : "Key";

            if (currentKeys >= requiredKeyCount)
            {
                string message = string.Format(confirmUnlockMessage, requiredKeyCount, keyName);
                confirmPanel.Show(message, 
                    onConfirm: () =>
                    {
                        isConfirming = false;
                        if (playerInventoryState != null && requiredKeyItem != null)
                        {
                            playerInventoryState.RemoveItem(requiredKeyItem, requiredKeyCount);
                        }
                        UnlockDoor();
                        UIManager.Instance?.HideInteractPrompt(this);
                        SceneTransitionManager.Instance.RequestDoorTransition(player, targetSceneName, targetDoorId);
                    },
                    onCancel: () =>
                    {
                        isConfirming = false;
                        waitingReleasePlayers.Remove(player.gameObject.GetInstanceID());
                    },
                    showYesButton: true
                );
            }
            else
            {
                string message = string.Format(notEnoughKeysMessage, requiredKeyCount, keyName, currentKeys);
                confirmPanel.Show(message, 
                    onConfirm: null,
                    onCancel: () =>
                    {
                        isConfirming = false;
                        waitingReleasePlayers.Remove(player.gameObject.GetInstanceID());
                    },
                    showYesButton: false
                );
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other))
                return;

            waitingReleasePlayers.Remove(player.gameObject.GetInstanceID());
            UIManager.Instance?.HideInteractPrompt(this);

            if (isConfirming)
            {
                ConfirmPanelController confirmPanel = Object.FindFirstObjectByType<ConfirmPanelController>();
                if (confirmPanel == null)
                {
                    ConfirmPanelController[] panels = Object.FindObjectsByType<ConfirmPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (panels != null && panels.Length > 0)
                    {
                        confirmPanel = panels[0];
                    }
                }
                if (confirmPanel != null)
                    confirmPanel.Hide();
                isConfirming = false;
            }
        }

        private bool IsUpPressed(PlayerInput playerInput)
        {
            bool fromAxis = playerInput != null && playerInput.MoveInput.y > requiredUpInput;
            if (fromAxis)
                return true;

            return Input.GetKeyDown(fallbackUpKey);
        }
    }
}
