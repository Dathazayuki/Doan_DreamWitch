using System.Collections.Generic;
using DreamKnight.Player;
using DreamKnight.Systems.Currency;
using DreamKnight.UI;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class GoldChest : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string uniqueId;

        [Header("Reward")]
        [SerializeField] private int goldAmount = 100;
        [SerializeField] private MoneyPickup moneyPrefab;
        [SerializeField] private int pickupCount = 5;
        [SerializeField] private Transform rewardSpawnPoint;
        [SerializeField] private float spawnScatterRadius = 0.25f;

        [Header("Interaction Prompt")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private string promptFormat = "<sprite=192> Open Chest";
        [SerializeField] private float interactCooldown = 0.5f;

        [Header("Visual States")]
        [SerializeField] private GameObject closedVisual;
        [SerializeField] private GameObject openedVisual;

        private static readonly HashSet<string> openedChests = new HashSet<string>();

        private PlayerController currentPlayer;
        private float nextInteractTime;
        private bool isOpened;

        public static void ClearOpenedChests()
        {
            openedChests.Clear();
        }

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;

            if (string.IsNullOrEmpty(uniqueId))
                uniqueId = System.Guid.NewGuid().ToString();
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(uniqueId))
                uniqueId = System.Guid.NewGuid().ToString();

            if (openedChests.Contains(uniqueId))
                MarkAsOpenedImmediate();
            else
                SetVisualState(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isOpened)
                return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other))
                return;

            currentPlayer = player;
            ShowInteractPrompt();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (isOpened || currentPlayer == null)
                return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other))
                return;

            if (player.Input == null || !player.Input.InteractPressed)
                return;

            if (Time.time < nextInteractTime)
                return;

            nextInteractTime = Time.time + interactCooldown;
            OpenChest();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other))
                return;

            HideInteractPrompt();
            currentPlayer = null;
        }

        private void OpenChest()
        {
            if (isOpened)
                return;

            isOpened = true;
            openedChests.Add(uniqueId);
            HideInteractPrompt();
            SetVisualState(true);
            SpawnMoneyPickups();
            DisableTrigger();
            currentPlayer = null;
        }

        private void SpawnMoneyPickups()
        {
            int totalAmount = Mathf.Max(0, goldAmount);
            int count = Mathf.Max(1, pickupCount);
            if (totalAmount <= 0 || moneyPrefab == null)
                return;

            Transform spawnTransform = rewardSpawnPoint != null ? rewardSpawnPoint : transform;
            int baseAmount = totalAmount / count;
            int remainder = totalAmount % count;

            for (int i = 0; i < count; i++)
            {
                int pickupAmount = baseAmount + (i < remainder ? 1 : 0);
                if (pickupAmount <= 0)
                    continue;

                Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, spawnScatterRadius);
                Vector3 spawnPosition = spawnTransform.position + new Vector3(scatter.x, scatter.y, 0f);
                MoneyPickup pickup = MoneyPickupPoolManager.Instance.Spawn(moneyPrefab, spawnPosition, Quaternion.identity);
                if (pickup != null)
                    pickup.SetAmount(pickupAmount);
            }
        }

        private void SetVisualState(bool opened)
        {
            if (closedVisual != null)
                closedVisual.SetActive(!opened);

            if (openedVisual != null)
                openedVisual.SetActive(opened);
        }

        private void MarkAsOpenedImmediate()
        {
            isOpened = true;
            SetVisualState(true);
            DisableTrigger();
        }

        private void DisableTrigger()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;
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

        private void OnDisable()
        {
            HideInteractPrompt();
        }

        private void OnDestroy()
        {
            HideInteractPrompt();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueId))
                uniqueId = System.Guid.NewGuid().ToString();

            goldAmount = Mathf.Max(0, goldAmount);
            pickupCount = Mathf.Max(1, pickupCount);
        }
#endif
    }
}
