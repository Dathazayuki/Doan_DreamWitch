using DreamKnight.Player;
using DreamKnight.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamKnight.Systems.Zone
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class RespawnShrine : MonoBehaviour
    {
        [Header("Checkpoint")]
        [SerializeField] private Transform respawnAnchor;

        [Header("Interaction")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private string promptFormat = "{icon}";
        [SerializeField] private float interactCooldown = 0.2f;

        private PlayerController currentPlayer;
        private float nextInteractTime;
        private bool healCompleted;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        private void Awake()
        {
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other))
                return;

            currentPlayer = player;
            ShowPrompt();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other))
                return;

            if (player.IsShrinePrayInProgress)
                return;

            if (player.Input == null || !player.Input.InteractPressed)
                return;

            if (Time.time < nextInteractTime)
                return;

            nextInteractTime = Time.time + Mathf.Max(0.01f, interactCooldown);
            Interact(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other))
                return;

            UIManager.Instance?.HideInteractPrompt(this);
            currentPlayer = null;
        }

        private void Interact(PlayerController player)
        {
            if (player == null)
                return;

            healCompleted = false;
            player.TryStartShrinePraySequence(ApplyShrineEffectsInPrayLoop, () => healCompleted);
        }

        private void ApplyShrineEffectsInPrayLoop()
        {
            if (currentPlayer != null && currentPlayer.Stats != null && currentPlayer.IsAlive)
                currentPlayer.Stats.ReviveToFullHealth();
            
            // Register this shrine with the service so respawn/abandon use it
            Vector3 shrinePos = GetRespawnPosition();
            string currentScene = SceneManager.GetActiveScene().name;
            RespawnShrineService.RegisterShrine(shrinePos, currentScene);
            Debug.Log($"[RespawnShrine] Registered shrine position {shrinePos} in scene '{currentScene}' with RespawnShrineService");

            healCompleted = true;
        }

        private void ShowPrompt()
        {
            if (UIManager.Instance == null)
                return;

            Transform anchor = promptAnchor != null ? promptAnchor : transform;
            UIManager.Instance.ShowInteractPrompt(this, anchor, PlayerInput.BindableAction.Interact, promptFormat);
        }

        public Vector3 GetRespawnPosition()
        {
            return respawnAnchor != null ? respawnAnchor.position : transform.position;
        }

    }
}
