using DreamKnight.Player;
using TMPro;
using UnityEngine;
using DKPlayerInput = DreamKnight.Player.PlayerInput;

namespace DreamKnight.Systems.Setting
{
    [DisallowMultipleComponent]
    public class KeyboardBindingText : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DKPlayerInput playerInput;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private bool autoFindPlayerInput = true;

        [Header("Binding")]
        [SerializeField] private DKPlayerInput.BindableAction action;
        [SerializeField] private bool preferSpriteIcon = true;
        [SerializeField] private string unsupportedText = "---";

        private void Awake()
        {
            if (valueText == null)
                valueText = GetComponent<TextMeshProUGUI>();

            ResolvePlayerInput();
        }

        private void OnEnable()
        {
            ResolvePlayerInput();
            RefreshText();

            if (playerInput != null)
                playerInput.OnBindingChanged += HandleBindingChanged;
        }

        private void Start()
        {
            RefreshText();
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.OnBindingChanged -= HandleBindingChanged;
        }

        public void RefreshText()
        {
            if (valueText == null)
                return;

            if (playerInput == null)
            {
                valueText.text = unsupportedText;
                return;
            }

            string iconTag = string.Empty;
            string keyName = string.Empty;
#if ENABLE_INPUT_SYSTEM
            iconTag = playerInput.GetBindingIconTag(action);
            keyName = playerInput.GetBindingKeyName(action);
#endif

            SetText(keyName, iconTag);
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

        private void HandleBindingChanged(DKPlayerInput.BindableAction changedAction, string keyName, string iconTag)
        {
            if (changedAction != action)
                return;

            SetText(keyName, iconTag);
        }

        private void SetText(string keyName, string iconTag)
        {
            if (valueText == null)
                return;

            if (preferSpriteIcon && !string.IsNullOrWhiteSpace(iconTag) && iconTag.StartsWith("<sprite="))
            {
                valueText.text = iconTag;
                return;
            }

            if (!string.IsNullOrWhiteSpace(keyName))
            {
                valueText.text = keyName;
                return;
            }

            if (!string.IsNullOrWhiteSpace(iconTag))
            {
                valueText.text = iconTag;
                return;
            }

            valueText.text = unsupportedText;
        }
    }
}
