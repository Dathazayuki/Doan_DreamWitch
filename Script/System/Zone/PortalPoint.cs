using DreamKnight.Player;
using DreamKnight.Systems.Currency;
using DreamKnight.Systems.Map;
using DreamKnight.Systems.SaveLoad;
using DreamKnight.UI;
using Project.UI;
using UnityEngine;

namespace DreamKnight.Systems.Zone
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class PortalPoint : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string portalId = "portal_001";
        [SerializeField] private bool startsUnlocked;
        [SerializeField] private Transform portalArrivalPoint;

        [Header("Unlock")]
        [SerializeField] private CurrencyWalletSO currencyWallet;
        [SerializeField] private int unlockCost = 5;

        [Header("Teleport")]
        [SerializeField] private float interactCooldown = 0.2f;

        [Header("Prompt")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private string unlockPromptFormat = "{icon} Unlock Portal (-{cost})";
        [SerializeField] private string unlockedPromptFormat = "{icon} Open Full Map Teleport";
        [SerializeField] private string confirmUnlockMessage = "Spend {0} Gold to unlock this portal?";
        [SerializeField] private string notEnoughGoldMessage = "Not enough Gold. Requires {0} Gold (Current: {1}).";

        [Header("Visual")]
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private GameObject unlockedVisual;

        private PlayerController currentPlayer;
        private float nextInteractTime;
        private bool isConfirming;

        public string PortalId => portalId;
        public bool IsUnlocked => PortalCheckpointService.IsUnlocked(portalId);
        public Vector3 WorldPosition => transform.position;

        public Vector3 GetArrivalWorldPosition()
        {
            return portalArrivalPoint != null ? portalArrivalPoint.position : transform.position;
        }

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        private void Awake()
        {
            if (startsUnlocked)
                PortalCheckpointService.Unlock(portalId);

            UpdateVisualState();
        }

        private void OnEnable()
        {
            PortalCheckpointService.OnPortalUnlockChanged += HandlePortalUnlockChanged;
            PortalCheckpointService.RegisterPortal(this);
            UpdateVisualState();
        }

        private void OnDisable()
        {
            PortalCheckpointService.OnPortalUnlockChanged -= HandlePortalUnlockChanged;
            PortalCheckpointService.ClearActiveTeleportPortal(this);
            PortalCheckpointService.UnregisterPortal(this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other))
                return;

            currentPlayer = player;
            if (IsUnlocked)
                PortalCheckpointService.SetActiveTeleportPortal(this);
            RefreshPrompt();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other))
                return;

            if (player.Input == null)
                return;

            RefreshPrompt();

            if (isConfirming)
                return;

            if (Time.time < nextInteractTime)
                return;

            if (!IsUnlocked)
            {
                if (player.Input.InteractPressed)
                {
                    RequestUnlockConfirm();
                    nextInteractTime = Time.time + Mathf.Max(0.01f, interactCooldown);
                }
                return;
            }

            PortalCheckpointService.SetActiveTeleportPortal(this);
            if (player.Input.InteractPressed)
            {
                MapFullToggleInput fullMapInput = FindAnyObjectByType<MapFullToggleInput>();
                if (fullMapInput != null)
                    fullMapInput.OpenFullMapForPortalTeleport(this);

                nextInteractTime = Time.time + Mathf.Max(0.01f, interactCooldown);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other))
                return;

            UIManager.Instance?.HideInteractPrompt(this);
            PortalCheckpointService.ClearActiveTeleportPortal(this);

            if (isConfirming)
            {
                ConfirmPanelController confirmPanel = ResolveConfirmPanel();
                if (confirmPanel != null)
                    confirmPanel.Hide();
                isConfirming = false;
            }

            currentPlayer = null;
        }

        private void RequestUnlockConfirm()
        {
            if (IsUnlocked)
                return;

            int cost = Mathf.Max(0, unlockCost);
            int currentGold = currencyWallet != null ? currencyWallet.Balance : 0;
            ConfirmPanelController confirmPanel = ResolveConfirmPanel();

            if (confirmPanel == null)
            {
                TryUnlock();
                return;
            }

            isConfirming = true;
            if (currencyWallet == null || currentGold < cost)
            {
                confirmPanel.Show(
                    string.Format(notEnoughGoldMessage, cost, currentGold),
                    onConfirm: null,
                    onCancel: () => isConfirming = false,
                    showYesButton: false);
                return;
            }

            confirmPanel.Show(
                string.Format(confirmUnlockMessage, cost),
                onConfirm: () =>
                {
                    isConfirming = false;
                    TryUnlock();
                },
                onCancel: () => isConfirming = false,
                showYesButton: true);
        }

        private void TryUnlock()
        {
            if (IsUnlocked)
                return;

            int cost = Mathf.Max(0, unlockCost);
            if (cost > 0)
            {
                if (currencyWallet == null)
                    return;

                if (!currencyWallet.Spend(cost))
                    return;
            }

            PortalCheckpointService.Unlock(portalId);
            PortalCheckpointService.SetActiveTeleportPortal(this);
            GameAutoSave.Request("portal_unlock");
            MapRenderTextureController mapController = FindAnyObjectByType<MapRenderTextureController>();
            if (mapController != null)
                mapController.ForceRebuildRuntimeMarkers();
            UpdateVisualState();
            RefreshPrompt();
        }

        private ConfirmPanelController ResolveConfirmPanel()
        {
            ConfirmPanelController confirmPanel = Object.FindFirstObjectByType<ConfirmPanelController>();
            if (confirmPanel != null)
                return confirmPanel;

            ConfirmPanelController[] panels = Object.FindObjectsOfType<ConfirmPanelController>(true);
            if (panels != null && panels.Length > 0)
                return panels[0];

            return null;
        }

        private void RefreshPrompt()
        {
            if (currentPlayer == null || UIManager.Instance == null)
                return;

            Transform anchor = promptAnchor != null ? promptAnchor : transform;
            if (!IsUnlocked)
            {
                string format = BuildUnlockPrompt();
                UIManager.Instance.ShowInteractPrompt(this, anchor, PlayerInput.BindableAction.Interact, format);
                return;
            }

            UIManager.Instance.ShowInteractPrompt(this, anchor, PlayerInput.BindableAction.Interact, unlockedPromptFormat);
        }

        private string BuildUnlockPrompt()
        {
            int cost = Mathf.Max(0, unlockCost);
            if (string.IsNullOrEmpty(unlockPromptFormat))
                return "{icon}";

            return unlockPromptFormat.Replace("{cost}", cost.ToString());
        }

        private void HandlePortalUnlockChanged(string unlockedPortalId)
        {
            if (!string.Equals(unlockedPortalId, portalId, System.StringComparison.Ordinal))
                return;

            UpdateVisualState();
            RefreshPrompt();
        }

        private void UpdateVisualState()
        {
            bool unlocked = IsUnlocked;
            if (lockedVisual != null)
                lockedVisual.SetActive(!unlocked);
            if (unlockedVisual != null)
                unlockedVisual.SetActive(unlocked);
        }

    }
}
