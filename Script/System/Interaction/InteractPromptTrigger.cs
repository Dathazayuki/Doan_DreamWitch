using DreamKnight.Player;
using DreamKnight.UI;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class InteractPromptTrigger : MonoBehaviour
    {
        [Header("Prompt")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private bool useBindableAction = true;
        [SerializeField] private PlayerInput.BindableAction action = PlayerInput.BindableAction.MoveUp;
        [SerializeField] private string fallbackKeyName = "E";
        [SerializeField] private string promptFormat = "{icon}";

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other) || UIManager.Instance == null)
                return;

            Transform anchor = promptAnchor != null ? promptAnchor : transform;
            if (useBindableAction)
                UIManager.Instance.ShowInteractPrompt(this, anchor, action, promptFormat);
            else
                UIManager.Instance.ShowInteractPromptByKeyName(this, anchor, fallbackKeyName, promptFormat);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other) || UIManager.Instance == null)
                return;

            UIManager.Instance.HideInteractPrompt(this);
        }
    }
}
