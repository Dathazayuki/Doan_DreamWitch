using DreamKnight.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.UI
{
    [DisallowMultipleComponent]
    public class PlayerFormHudUI : MonoBehaviour
    {
        [SerializeField] private Image formIconImage;
        [SerializeField] private Sprite defaultFormIcon;
        [SerializeField] private bool hideWhenNoForm = true;

        private PlayerController playerController;
        private PlayerFormConfig playerFormConfig;
        private PlayerFormDataSO lastUnlockedForm;
        private bool lastTransformedState;
        private bool hasRendered;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void LateUpdate()
        {
            if (playerFormConfig == null)
            {
                ResolveReferences();
                return;
            }

            bool transformed = playerController != null && playerController.IsTransformed;
            PlayerFormDataSO unlockedForm = transformed ? playerFormConfig.GetUnlockedForm() : null;
            if (hasRendered && unlockedForm == lastUnlockedForm && transformed == lastTransformedState)
                return;

            lastUnlockedForm = unlockedForm;
            lastTransformedState = transformed;
            hasRendered = true;
            UpdateFormDisplay(unlockedForm, transformed);
        }

        private void ResolveReferences()
        {
            if (playerFormConfig != null)
                return;

            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
                playerFormConfig = playerController.GetComponent<PlayerFormConfig>();
        }

        private void UpdateFormDisplay(PlayerFormDataSO unlockedForm, bool transformed)
        {
            if (formIconImage == null)
                return;

            if (!transformed)
            {
                formIconImage.sprite = defaultFormIcon;
                formIconImage.gameObject.SetActive(defaultFormIcon != null || !hideWhenNoForm);
                return;
            }

            if (playerFormConfig == null || unlockedForm == null)
            {
                formIconImage.sprite = defaultFormIcon;
                formIconImage.gameObject.SetActive(defaultFormIcon != null || !hideWhenNoForm);

                return;
            }

            formIconImage.sprite = unlockedForm.formIcon != null ? unlockedForm.formIcon : defaultFormIcon;
            formIconImage.gameObject.SetActive(formIconImage.sprite != null || !hideWhenNoForm);
        }
    }
}
