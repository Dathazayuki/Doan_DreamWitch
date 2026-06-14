using System.Collections.Generic;
using DreamKnight.Player;
using DreamKnight.UI;
using Project.UI;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class Storage : MonoBehaviour
    {
        [Header("Interaction Prompt")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private string promptFormat = "<sprite=192> Edit Inventory";
        [SerializeField] private float interactCooldown = 0.5f;

        private PlayerController currentPlayer;
        private float nextInteractTime;

        // Static tracking of all overlapping Storage triggers
        private static readonly HashSet<Collider2D> nearbyStorages = new HashSet<Collider2D>();

        public static bool IsPlayerNear => nearbyStorages.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            nearbyStorages.Clear();
        }

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other)) return;

            currentPlayer = player;
            Collider2D myCollider = GetComponent<Collider2D>();
            if (myCollider != null)
            {
                nearbyStorages.Add(myCollider);
            }

            ShowInteractPrompt();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other)) return;

            if (player.Input == null || !player.Input.InteractPressed) return;
            if (Time.time < nextInteractTime) return;

            nextInteractTime = Time.time + interactCooldown;
            TriggerInventory();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other)) return;

            UIManager.Instance?.HideInteractPrompt(this);

            Collider2D myCollider = GetComponent<Collider2D>();
            if (myCollider != null)
            {
                nearbyStorages.Remove(myCollider);
            }

            currentPlayer = null;
        }

        private void OnDisable()
        {
            Collider2D myCollider = GetComponent<Collider2D>();
            if (myCollider != null)
            {
                nearbyStorages.Remove(myCollider);
            }
        }

        private void OnDestroy()
        {
            Collider2D myCollider = GetComponent<Collider2D>();
            if (myCollider != null)
            {
                nearbyStorages.Remove(myCollider);
            }
        }

        private void ShowInteractPrompt()
        {
            if (UIManager.Instance == null) return;

            Transform anchor = promptAnchor != null ? promptAnchor : transform;
            // Show the interact prompt using the player's interact key config but with custom text
            UIManager.Instance.ShowInteractPrompt(this, anchor, PlayerInput.BindableAction.Interact, promptFormat);
        }

        private void TriggerInventory()
        {
            var menuController = FindAnyObjectByType<MenuMain2Controller>();
            if (menuController != null)
            {
                menuController.ToggleMenuRoot();
            }
        }
    }
}
