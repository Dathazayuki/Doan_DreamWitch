using DreamKnight.Player;
using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.SaveLoad;
using DreamKnight.UI;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    /// <summary>
    /// Base class for pickable items in the world.
    /// Inheritable and extensible for various item behaviors.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class ItemPickup : MonoBehaviour
    {
        [Header("Save")]
        [SerializeField] private string pickupId;
        [SerializeField] private bool persistCollectedState = true;

        [Header("Item Config")]
        [SerializeField] protected ItemDefinitionSO itemDefinition;
        [SerializeField] protected int quantity = 1;
        [SerializeField] protected InventoryStateSO inventoryState;

        [Header("Interaction Prompt")]
        [SerializeField] protected Transform promptAnchor;
        [SerializeField] protected string promptFormat = "<sprite=192> Pick up {0}";

        protected PlayerController currentPlayer;
        protected bool isPickedUp;
        private bool isPickingUp;

        protected virtual void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;

            GeneratePickupIdIfEmpty();
        }

        protected virtual void Awake()
        {
            RefreshSavedCollectedState();
        }

        protected virtual void OnEnable()
        {
            WorldPickupSaveService.OnCollectedStateLoaded += RefreshSavedCollectedState;
            RefreshSavedCollectedState();
        }

        protected virtual void OnDisable()
        {
            WorldPickupSaveService.OnCollectedStateLoaded -= RefreshSavedCollectedState;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            GeneratePickupIdIfEmpty();
        }
#endif

        private string ResolvedPickupId => string.IsNullOrWhiteSpace(pickupId) ? gameObject.scene.name + "/" + gameObject.name : pickupId;

        public void ConfigureRuntimePickupId(string runtimePickupId, bool persist)
        {
            pickupId = runtimePickupId;
            persistCollectedState = persist;
            isPickedUp = false;
            isPickingUp = false;

            if (!persistCollectedState || !WorldPickupSaveService.IsCollected(ResolvedPickupId))
            {
                if (!gameObject.activeSelf)
                    gameObject.SetActive(true);
                return;
            }

            isPickedUp = true;
            gameObject.SetActive(false);
        }

        private void GeneratePickupIdIfEmpty()
        {
            if (!string.IsNullOrWhiteSpace(pickupId))
                return;

            pickupId = $"{gameObject.scene.name}/{gameObject.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        private void RefreshSavedCollectedState()
        {
            if (!persistCollectedState || !WorldPickupSaveService.IsCollected(ResolvedPickupId))
                return;

            isPickedUp = true;
            gameObject.SetActive(false);
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (isPickedUp || isPickingUp) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other)) return;

            currentPlayer = player;
            ShowInteractPrompt();
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (isPickedUp || isPickingUp) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other)) return;

            if (player.Input == null || !player.Input.InteractPressed) return;

            // Ràng buộc chỉ có dạng Human mới nhặt được
            if (player.IsTransformed)
            {
                Debug.Log("[ItemPickup] Chỉ có dạng HumanForm mới có thể nhặt vật phẩm!");
                return;
            }

            TryPickUp(player);
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other)) return;

            HideInteractPrompt();
            currentPlayer = null;
        }

        protected virtual void ShowInteractPrompt()
        {
            if (UIManager.Instance == null || itemDefinition == null) return;

            Transform anchor = promptAnchor != null ? promptAnchor : transform;
            string displayName = itemDefinition.DisplayName;
            string promptText = string.Format(promptFormat, displayName);
            UIManager.Instance.ShowInteractPrompt(this, anchor, PlayerInput.BindableAction.Interact, promptText);
        }

        protected virtual void HideInteractPrompt()
        {
            UIManager.Instance?.HideInteractPrompt(this);
        }

        protected virtual void TryPickUp(PlayerController player)
        {
            if (itemDefinition == null || inventoryState == null) return;

            isPickingUp = true;
            HideInteractPrompt();

            // Khởi động sequence nhặt đồ (phát animation Take)
            bool started = player.TryStartPickUpSequence(() =>
            {
                OnPickUpComplete(player);
            },
            () =>
            {
                OnPickUpCancelled();
            });

            if (!started)
                OnPickUpCancelled();
        }

        protected virtual void OnPickUpComplete(PlayerController player)
        {
            if (isPickedUp)
                return;

            isPickingUp = false;
            isPickedUp = true;

            // Thêm vào inventory
            inventoryState.AddItem(itemDefinition, quantity);

            if (persistCollectedState)
                WorldPickupSaveService.MarkCollected(ResolvedPickupId);

            GameAutoSave.Request("item_pickup");
            
            // Xóa object khỏi scene
            Destroy(gameObject);
        }

        protected virtual void OnPickUpCancelled()
        {
            if (isPickedUp)
                return;

            isPickingUp = false;

            if (isActiveAndEnabled && currentPlayer != null)
                ShowInteractPrompt();
        }
    }
}
