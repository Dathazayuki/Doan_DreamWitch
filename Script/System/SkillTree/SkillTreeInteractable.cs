using DreamKnight.Player;
using DreamKnight.UI;
using UnityEngine;

namespace DreamKnight.Systems.SkillTree
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class SkillTreeInteractable : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SkillTreeCanvasController canvasController;
        [SerializeField] private UIStateManager uiStateManager;

        [Header("Interaction Prompt")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private string promptFormat = "<sprite=192> Skill Tree";
        [SerializeField] private float interactCooldown = 0.5f;

        private PlayerController currentPlayer;
        private float nextInteractTime;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other))
                return;

            currentPlayer = player;
            ShowInteractPrompt();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other))
                return;

            if (player.Input == null || !player.Input.InteractPressed)
                return;

            if (Time.time < nextInteractTime)
                return;

            nextInteractTime = Time.time + interactCooldown;
            OpenSkillTree();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other))
                return;

            HideInteractPrompt();
            currentPlayer = null;
        }

        private void OnDisable()
        {
            HideInteractPrompt();
            currentPlayer = null;
        }

        private void ShowInteractPrompt()
        {
            if (UIManager.Instance == null)
                return;

            Transform anchor = promptAnchor != null ? promptAnchor : transform;
            UIManager.Instance.ShowInteractPrompt(this, anchor, PlayerInput.BindableAction.Interact, promptFormat);
        }

        private void HideInteractPrompt()
        {
            UIManager.Instance?.HideInteractPrompt(this);
        }

        private void OpenSkillTree()
        {
            canvasController = canvasController != null ? canvasController : SkillTreeCanvasController.Instance;
            if (canvasController == null)
                return;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null && uiStateManager.IsAnyUIPanelActive())
                return;

            HideInteractPrompt();
            canvasController.OpenSkillTree();
        }
    }
}
