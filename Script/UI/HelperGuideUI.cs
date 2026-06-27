using System;
using System.Collections.Generic;
using DreamKnight.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    [DisallowMultipleComponent]
    public class HelperGuideUI : MonoBehaviour
    {
        [Serializable]
        public class GuideTopic
        {
            public string title;
            [TextArea(4, 12)] public string description;
            public Sprite image;
        }

        [Serializable]
        public class TopicButtonBinding
        {
            public Button button;
            public TextMeshProUGUI label;
            public GameObject selectedObject;
        }

        [Header("Topics")]
        [SerializeField] private List<GuideTopic> topics = new List<GuideTopic>();

        [Header("Input Icons")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private bool autoFindPlayerInput = true;
        [SerializeField] private bool preferInputSpriteIcons = true;

        [Header("Left Buttons")]
        [SerializeField] private List<TopicButtonBinding> topicButtons = new List<TopicButtonBinding>();

        [Header("Right Detail")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image guideImage;
        [SerializeField] private GameObject imageRoot;

        [Header("Start")]
        [SerializeField] private int defaultTopicIndex;

        private int selectedIndex = -1;

        private void Reset()
        {
            PopulateDefaultTopics();
        }

        private void Awake()
        {
            if (topics == null || topics.Count == 0)
                PopulateDefaultTopics();

            ResolvePlayerInput();
        }

        private void OnEnable()
        {
            ResolvePlayerInput();
            if (playerInput != null)
                playerInput.OnBindingChanged += HandleBindingChanged;

            BindButtons();
            SelectTopic(Mathf.Clamp(defaultTopicIndex, 0, Mathf.Max(0, topics.Count - 1)));
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.OnBindingChanged -= HandleBindingChanged;

            UnbindButtons();
        }

        public void SelectTopic(int index)
        {
            if (topics == null || topics.Count == 0)
                return;

            if (index < 0 || index >= topics.Count)
                index = 0;

            selectedIndex = index;
            GuideTopic topic = topics[index];

            if (titleText != null)
                titleText.text = topic.title ?? string.Empty;

            if (descriptionText != null)
                descriptionText.text = ApplyBindingTokens(topic.description ?? string.Empty);

            bool hasImage = topic.image != null;
            if (guideImage != null)
            {
                guideImage.sprite = topic.image;
                guideImage.enabled = hasImage;
            }

            if (imageRoot != null)
                imageRoot.SetActive(hasImage);

            RefreshButtonVisuals();
        }

        private void ResolvePlayerInput()
        {
            if (!autoFindPlayerInput)
                return;

            if (PersistentPlayerRoot.Instance != null)
                playerInput = PersistentPlayerRoot.Instance.GetComponent<PlayerInput>();

            if (playerInput == null)
                playerInput = FindAnyObjectByType<PlayerInput>();
        }

        private void HandleBindingChanged(PlayerInput.BindableAction action, string keyName, string iconTag)
        {
            if (selectedIndex >= 0)
                SelectTopic(selectedIndex);
        }

        private string ApplyBindingTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            text = ReplaceActionToken(text, "<left>", PlayerInput.BindableAction.MoveLeft);
            text = ReplaceActionToken(text, "<right>", PlayerInput.BindableAction.MoveRight);
            text = ReplaceActionToken(text, "<up>", PlayerInput.BindableAction.MoveUp);
            text = ReplaceActionToken(text, "<down>", PlayerInput.BindableAction.MoveDown);
            text = ReplaceActionToken(text, "<jump>", PlayerInput.BindableAction.Jump);
            text = ReplaceActionToken(text, "<space>", PlayerInput.BindableAction.Jump);
            text = ReplaceActionToken(text, "<dash>", PlayerInput.BindableAction.Dodge);
            text = ReplaceActionToken(text, "<dodge>", PlayerInput.BindableAction.Dodge);
            text = ReplaceActionToken(text, "<attack>", PlayerInput.BindableAction.NormalAttack);
            text = ReplaceActionToken(text, "<interact>", PlayerInput.BindableAction.Interact);
            text = ReplaceActionToken(text, "<potion>", PlayerInput.BindableAction.UsePotion);
            text = ReplaceActionToken(text, "<heal>", PlayerInput.BindableAction.UsePotion);
            text = ReplaceActionToken(text, "<tool>", PlayerInput.BindableAction.UseTool);
            text = ReplaceActionToken(text, "<spell>", PlayerInput.BindableAction.UseSpell);
            text = ReplaceActionToken(text, "<transform>", PlayerInput.BindableAction.Transform);

            return text;
        }

        private string ReplaceActionToken(string text, string token, PlayerInput.BindableAction action)
        {
            string replacement = GetActionDisplayText(action, token);
            return ReplaceOrdinalIgnoreCase(text, token, replacement);
        }

        private string GetActionDisplayText(PlayerInput.BindableAction action, string fallback)
        {
            if (playerInput == null)
                return TokenToPlainText(fallback);

            string iconTag = playerInput.GetBindingIconTag(action);
            if (preferInputSpriteIcons && !string.IsNullOrWhiteSpace(iconTag) && iconTag.StartsWith("<sprite="))
                return iconTag;

            string keyName = playerInput.GetBindingKeyName(action);
            if (!string.IsNullOrWhiteSpace(keyName))
                return keyName;

            if (!string.IsNullOrWhiteSpace(iconTag))
                return iconTag;

            return TokenToPlainText(fallback);
        }

        private static string TokenToPlainText(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;

            string value = token.Trim();
            if (value.Length >= 2 && value[0] == '<' && value[value.Length - 1] == '>')
                value = value.Substring(1, value.Length - 2);

            switch (value.ToLowerInvariant())
            {
                case "left": return "Left";
                case "right": return "Right";
                case "up": return "Up";
                case "down": return "Down";
                case "jump":
                case "space": return "Jump";
                case "dash":
                case "dodge": return "Dash";
                case "attack": return "Attack";
                case "interact": return "Interact";
                case "potion":
                case "heal": return "Heal";
                case "tool": return "Tool";
                case "spell": return "Spell";
                case "transform": return "Transform";
                default: return value;
            }
        }

        private static string ReplaceOrdinalIgnoreCase(string source, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(oldValue))
                return source;

            int index = source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return source;

            System.Text.StringBuilder builder = new System.Text.StringBuilder(source.Length);
            int lastIndex = 0;

            while (index >= 0)
            {
                builder.Append(source, lastIndex, index - lastIndex);
                builder.Append(newValue);

                lastIndex = index + oldValue.Length;
                index = source.IndexOf(oldValue, lastIndex, StringComparison.OrdinalIgnoreCase);
            }

            builder.Append(source, lastIndex, source.Length - lastIndex);
            return builder.ToString();
        }

        private void BindButtons()
        {
            if (topicButtons == null)
                return;

            for (int i = 0; i < topicButtons.Count; i++)
            {
                int index = i;
                TopicButtonBinding binding = topicButtons[i];
                if (binding == null || binding.button == null)
                    continue;

                binding.button.onClick.RemoveAllListeners();
                binding.button.onClick.AddListener(() => SelectTopic(index));
            }

            RefreshButtonLabels();
            RefreshButtonVisuals();
        }

        private void UnbindButtons()
        {
            if (topicButtons == null)
                return;

            for (int i = 0; i < topicButtons.Count; i++)
            {
                TopicButtonBinding binding = topicButtons[i];
                if (binding != null && binding.button != null)
                    binding.button.onClick.RemoveAllListeners();
            }
        }

        private void RefreshButtonLabels()
        {
            if (topicButtons == null)
                return;

            for (int i = 0; i < topicButtons.Count; i++)
            {
                TopicButtonBinding binding = topicButtons[i];
                if (binding == null)
                    continue;

                string title = i < topics.Count && topics[i] != null ? topics[i].title : string.Empty;

                if (binding.label != null)
                {
                    binding.label.text = title;
                }
                else if (binding.button != null)
                {
                    TextMeshProUGUI childLabel = binding.button.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (childLabel != null)
                        childLabel.text = title;
                }
            }
        }

        private void RefreshButtonVisuals()
        {
            if (topicButtons == null)
                return;

            for (int i = 0; i < topicButtons.Count; i++)
            {
                TopicButtonBinding binding = topicButtons[i];
                if (binding != null && binding.selectedObject != null)
                    binding.selectedObject.SetActive(i == selectedIndex);
            }
        }

        private void PopulateDefaultTopics()
        {
            topics = new List<GuideTopic>
            {
                new GuideTopic
                {
                    title = "Movement & Combat",
                    description =
                        "Press <left> and <right> to move left and right.\n" +
                        "Press <down> to crouch and move through tight spaces.\n" +
                        "Press <jump> to jump.\n" +
                        "Press <dash> to move quickly, but each dash consumes stamina.\n" +
                        "Press <attack> to perform a three-hit combo chain."
                },
                new GuideTopic
                {
                    title = "Inventory",
                    description =
                        "You can visit the Village to buy supplies before exploring.\n" +
                        "During exploration, Storage Areas allow you to equip tools, healing items, and spell skills.\n" +
                        "Press <interact> near a Storage Area to edit your equipment."
                },
                new GuideTopic
                {
                    title = "Tools & Spells",
                    description =
                        "Press <tool> to use an equipped tool.\n" +
                        "Press <heal> to use an equipped healing item.\n" +
                        "Press <spell> to cast an equipped spell skill.\n" +
                        "Tools and healing items have limited uses during exploration. Spell skills consume mana each time they are cast."
                },
                new GuideTopic
                {
                    title = "Grimoire Chests",
                    description =
                        "While exploring, you may find Grimoire Chests.\n" +
                        "Grimoires grant temporary stat bonuses.\n" +
                        "Be careful: when you die, all Grimoires are destroyed."
                },
                new GuideTopic
                {
                    title = "Transformation",
                    description =
                        "During exploration, some defeated monsters leave a body behind instead of disappearing into the dungeon.\n" +
                        "After unlocking Node 1 in the Skill Tree, you can possess these bodies and transform into that monster.\n" +
                        "Press <transform> near a valid body to transform.\n" +
                        "While transformed, you inherit the monster's health and skills."
                }
            };
        }
    }
}
