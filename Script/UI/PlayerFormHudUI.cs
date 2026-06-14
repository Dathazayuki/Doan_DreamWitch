using DreamKnight.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.UI
{
    [DisallowMultipleComponent]
    public class PlayerFormHudUI : MonoBehaviour
    {
        [SerializeField] private Image formIconImage;
        [SerializeField] private bool hideWhenNoForm = true;

        private PlayerFormConfig playerFormConfig;
        private PlayerFormDataSO lastUnlockedForm;

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

            PlayerFormDataSO unlockedForm = playerFormConfig.GetUnlockedForm();
            if (unlockedForm == lastUnlockedForm)
                return;

            lastUnlockedForm = unlockedForm;
            UpdateFormDisplay(unlockedForm);
        }

        private void ResolveReferences()
        {
            if (playerFormConfig != null)
                return;

            PlayerController controller = FindAnyObjectByType<PlayerController>();
            if (controller != null)
                playerFormConfig = controller.GetComponent<PlayerFormConfig>();
        }

        private void UpdateFormDisplay(PlayerFormDataSO unlockedForm)
        {
            if (playerFormConfig == null || unlockedForm == null)
            {
                if (formIconImage != null)
                    formIconImage.sprite = null;

                if (hideWhenNoForm && formIconImage != null)
                    formIconImage.gameObject.SetActive(false);

                return;
            }

            if (formIconImage != null)
            {
                formIconImage.sprite = unlockedForm.formIcon;
                formIconImage.gameObject.SetActive(true);
            }
        }
    }
}
