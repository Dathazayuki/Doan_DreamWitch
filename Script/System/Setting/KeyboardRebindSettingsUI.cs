using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DreamKnight.Player;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
using DKPlayerInput = DreamKnight.Player.PlayerInput;

namespace DreamKnight.Systems.Setting
{
    [DisallowMultipleComponent]
    public class KeyboardRebindSettingsUI : MonoBehaviour
    {
        [Serializable]
        private class RebindSlot
        {
            public string id;
            public Button button;
            public TextMeshProUGUI valueText;
            public bool isSupported = true;
            public DKPlayerInput.BindableAction action;
        }

        [Header("References")]
        [SerializeField] private DKPlayerInput playerInput;
        [SerializeField] private bool autoFindPlayerInput = true;

        [Header("UI")]
        [SerializeField] private List<RebindSlot> slots = new List<RebindSlot>();
        [SerializeField] private Button resetSettingsButton;

        [Header("Texts")]
        [SerializeField] private string unsupportedText = "---";
        [SerializeField] private string waitingForKeyText = "...";

        private readonly List<UnityEngine.Events.UnityAction> slotClickHandlers = new List<UnityEngine.Events.UnityAction>();
        private int waitingSlotIndex = -1;
        private bool isWaitingForKey;

        private void Awake()
        {
            ResolvePlayerInput();
        }

        private void OnEnable()
        {
            ResolvePlayerInput();
            BindEvents();
            RefreshAllSlots();
            if (playerInput != null)
                playerInput.OnBindingChanged += HandleBindingChanged;
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.OnBindingChanged -= HandleBindingChanged;
            UnbindEvents();
            isWaitingForKey = false;
            waitingSlotIndex = -1;
        }

        private void Update()
        {
            if (!isWaitingForKey)
                return;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    ApplyRebindForWaitingSlot("<Mouse>/leftButton");
                    return;
                }

                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    ApplyRebindForWaitingSlot("<Mouse>/rightButton");
                    return;
                }

                if (Mouse.current.middleButton.wasPressedThisFrame)
                {
                    ApplyRebindForWaitingSlot("<Mouse>/middleButton");
                    return;
                }
            }

            if (Keyboard.current == null)
                return;

            KeyControl pressedKey = null;
            foreach (KeyControl key in Keyboard.current.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    pressedKey = key;
                    break;
                }
            }

            if (pressedKey == null)
                return;

            if (pressedKey.keyCode == Key.Escape)
            {
                CancelWaitingState();
                return;
            }

            ApplyRebindForWaitingSlot(BuildKeyboardControlPath(pressedKey));
#endif
        }

        private void BindEvents()
        {
            UnbindEvents();

            for (int i = 0; i < slots.Count; i++)
            {
                RebindSlot slot = slots[i];
                if (slot == null || slot.button == null)
                    continue;

                int index = i;
                UnityEngine.Events.UnityAction clickAction = () => HandleSlotClicked(index);
                slot.button.onClick.AddListener(clickAction);
                slotClickHandlers.Add(clickAction);
            }

            if (resetSettingsButton != null)
                resetSettingsButton.onClick.AddListener(HandleResetButton);
        }

        private void ResolvePlayerInput()
        {
            if (!autoFindPlayerInput)
                return;

            if (PersistentPlayerRoot.Instance != null)
                playerInput = PersistentPlayerRoot.Instance.GetComponent<DKPlayerInput>();

            if (playerInput == null)
                playerInput = FindAnyObjectByType<DKPlayerInput>();
        }

        private void UnbindEvents()
        {
            int handlerIndex = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                RebindSlot slot = slots[i];
                if (slot == null || slot.button == null)
                    continue;

                if (handlerIndex < slotClickHandlers.Count)
                    slot.button.onClick.RemoveListener(slotClickHandlers[handlerIndex]);

                handlerIndex++;
            }

            slotClickHandlers.Clear();

            if (resetSettingsButton != null)
                resetSettingsButton.onClick.RemoveListener(HandleResetButton);
        }

        private void HandleSlotClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return;

            RebindSlot slot = slots[slotIndex];
            if (slot == null)
                return;

            if (!slot.isSupported)
            {
                SetSlotText(slot, unsupportedText);
                return;
            }

            if (playerInput == null)
                return;

            if (waitingSlotIndex >= 0 && waitingSlotIndex < slots.Count)
                RefreshSlot(waitingSlotIndex);

            waitingSlotIndex = slotIndex;
            isWaitingForKey = true;
            SetSlotText(slot, waitingForKeyText);
        }

        private void HandleResetButton()
        {
            if (playerInput == null)
                return;

#if ENABLE_INPUT_SYSTEM
            playerInput.ResetBindingsToDefault();
#endif
            isWaitingForKey = false;
            waitingSlotIndex = -1;
            RefreshAllSlots();
        }

        private void CancelWaitingState()
        {
            isWaitingForKey = false;
            if (waitingSlotIndex >= 0)
                RefreshSlot(waitingSlotIndex);
            waitingSlotIndex = -1;
        }

        private void ApplyRebindForWaitingSlot(string controlPath)
        {
            if (waitingSlotIndex < 0 || waitingSlotIndex >= slots.Count)
            {
                isWaitingForKey = false;
                waitingSlotIndex = -1;
                return;
            }

            RebindSlot slot = slots[waitingSlotIndex];
            if (slot == null || !slot.isSupported || playerInput == null)
            {
                CancelWaitingState();
                return;
            }

            bool changed = false;
#if ENABLE_INPUT_SYSTEM
            changed = playerInput.RebindByControlPath(slot.action, controlPath);
#endif

            isWaitingForKey = false;

            if (!changed)
            {
                RefreshSlot(waitingSlotIndex);
                waitingSlotIndex = -1;
                return;
            }

            RefreshSlot(waitingSlotIndex);
            waitingSlotIndex = -1;
        }

        private void RefreshAllSlots()
        {
            for (int i = 0; i < slots.Count; i++)
                RefreshSlot(i);
        }

        private void RefreshSlot(int index)
        {
            if (index < 0 || index >= slots.Count)
                return;

            RebindSlot slot = slots[index];
            if (slot == null)
                return;

            if (!slot.isSupported)
            {
                SetSlotText(slot, unsupportedText);
                return;
            }

            if (playerInput == null)
            {
                SetSlotText(slot, unsupportedText);
                return;
            }

            string iconTag = string.Empty;
            string keyName = string.Empty;
#if ENABLE_INPUT_SYSTEM
            iconTag = playerInput.GetBindingIconTag(slot.action);
            keyName = playerInput.GetBindingKeyName(slot.action);
#endif

            if (!string.IsNullOrWhiteSpace(iconTag) && iconTag.StartsWith("<sprite="))
            {
                SetSlotText(slot, iconTag);
                return;
            }

            if (!string.IsNullOrWhiteSpace(keyName))
            {
                SetSlotText(slot, keyName);
                return;
            }

            SetSlotText(slot, unsupportedText);
        }

        private void SetSlotText(RebindSlot slot, string text)
        {
            if (slot == null || slot.valueText == null)
                return;

            slot.valueText.text = text;
        }

        private void HandleBindingChanged(DKPlayerInput.BindableAction action, string keyName, string iconTag)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                RebindSlot slot = slots[i];
                if (slot == null || !slot.isSupported)
                    continue;

                if (slot.action != action)
                    continue;

                if (!string.IsNullOrWhiteSpace(iconTag) && iconTag.StartsWith("<sprite="))
                {
                    SetSlotText(slot, iconTag);
                }
                else if (!string.IsNullOrWhiteSpace(keyName))
                {
                    SetSlotText(slot, keyName);
                }
                else
                {
                    SetSlotText(slot, unsupportedText);
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        private static string BuildKeyboardControlPath(KeyControl key)
        {
            return $"<Keyboard>/{key.name}";
        }
#endif
    }
}
