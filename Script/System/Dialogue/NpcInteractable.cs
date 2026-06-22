using DreamKnight.Player;
using DreamKnight.Systems.Inventory;
using DreamKnight.Systems.SaveLoad;
using DreamKnight.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace DreamKnight.Systems.Dialogue
{
    /// <summary>
    /// Gắn vào NPC GameObject.
    /// Khi Player bước vào vùng Trigger và bấm Interact → gọi Yarn DialogueRunner.StartDialogue() trực tiếp.
    /// 
    /// Setup trong Unity:
    ///   1. Thêm Collider2D (isTrigger = true) vào NPC
    ///   2. Gán component này
    ///   3. Điền yarnNodeTitle = tên node trong file .yarn
    ///   4. Tuỳ chọn: điền promptAnchor để gợi ý phím nổi trên đầu NPC
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class NpcInteractable : MonoBehaviour
    {
        private enum DialogueBranchCondition
        {
            HasItem,
            MissingItem,
            BossDefeated,
            BossNotDefeated
        }

        [Serializable]
        private class DialogueBranch
        {
            [SerializeField] private string branchNodeTitle;
            [SerializeField] private DialogueBranchCondition condition;
            [SerializeField] private string itemId;
            [SerializeField] private int requiredItemCount = 1;
            [SerializeField] private string bossId;

            public string BranchNodeTitle => branchNodeTitle;

            public bool IsMatch(InventoryStateSO inventoryState, ItemDatabaseSO itemDatabase)
            {
                switch (condition)
                {
                    case DialogueBranchCondition.HasItem:
                        return GetItemCount(inventoryState, itemDatabase) >= Mathf.Max(1, requiredItemCount);
                    case DialogueBranchCondition.MissingItem:
                        return GetItemCount(inventoryState, itemDatabase) < Mathf.Max(1, requiredItemCount);
                    case DialogueBranchCondition.BossDefeated:
                        return BossDefeatSaveService.IsDefeated(bossId);
                    case DialogueBranchCondition.BossNotDefeated:
                        return !BossDefeatSaveService.IsDefeated(bossId);
                    default:
                        return false;
                }
            }

            private int GetItemCount(InventoryStateSO inventoryState, ItemDatabaseSO itemDatabase)
            {
                if (inventoryState == null || itemDatabase == null || string.IsNullOrWhiteSpace(itemId))
                    return 0;

                ItemDefinitionSO item = itemDatabase.FindById(itemId);
                return item != null ? inventoryState.GetQuantity(item) : 0;
            }
        }
        [Header("Yarn Dialogue")]
        [Tooltip("Tên node bắt đầu trong file .yarn, VD: Merchant_Intro")]
        [SerializeField] private string yarnNodeTitle = "NPC_DefaultTalk";

        [Header("Dialogue Branching")]
        [SerializeField] private InventoryStateSO inventoryState;
        [SerializeField] private ItemDatabaseSO itemDatabase;
        [SerializeField] private List<DialogueBranch> dialogueBranches = new List<DialogueBranch>();

        [Header("Interaction Prompt")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private string promptFormat = "{icon} Nói chuyện";
        [SerializeField] private float interactCooldown = 0.5f;

        [Header("NPC Info (tuỳ chọn)")]
        [SerializeField] private string npcName = "Nhân vật";

        private PlayerController currentPlayer;
        private float nextInteractTime;
        private DialogueRunner yarnDialogueRunner;

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            // Find Yarn DialogueRunner in scene (could be on any GameObject)
            yarnDialogueRunner = FindObjectOfType<DialogueRunner>();
            if (yarnDialogueRunner == null)
                Debug.LogWarning($"[NpcInteractable] Không tìm thấy Yarn DialogueRunner trong scene!", this);
        }

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other)) return;

            currentPlayer = player;
            ShowInteractPrompt();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other)) return;

            // Không cho interact khi đang có hội thoại khác
            if (yarnDialogueRunner != null && yarnDialogueRunner.IsDialogueRunning)
                return;

            if (player.Input == null || !player.Input.InteractPressed) return;
            if (Time.time < nextInteractTime) return;

            nextInteractTime = Time.time + Mathf.Max(0.01f, interactCooldown);
            TriggerDialogue(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other)) return;

            UIManager.Instance?.HideInteractPrompt(this);
            currentPlayer = null;
        }

        // ─────────────────────────────────────────────────────────────

        private void TriggerDialogue(PlayerController player)
        {
            if (yarnDialogueRunner == null)
            {
                Debug.LogWarning($"[NpcInteractable] Yarn DialogueRunner không sẵn sàng!", this);
                return;
            }

            // Ẩn prompt khi vào hội thoại
            UIManager.Instance?.HideInteractPrompt(this);

            // Disable player input
            player.Input?.DisableInput();

            // Subscribe to dialogue complete event to re-enable input
            yarnDialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
            yarnDialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);

            // Start dialogue directly
            yarnDialogueRunner.StartDialogue(ResolveDialogueNodeTitle());
        }

        private void OnDialogueComplete()
        {
            // Re-enable input next frame to avoid the completed input being
            // re-read by player controller (e.g., Space closing dialogue also
            // triggering jump). Start a coroutine to delay by one frame.
            StartCoroutine(ReenableInputNextFrame());
        }

        private IEnumerator ReenableInputNextFrame()
        {
            yield return null;

            currentPlayer?.Input?.EnableInput();

            // Cleanup
            if (yarnDialogueRunner != null)
                yarnDialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
        }

        private void ShowInteractPrompt()
        {
            if (UIManager.Instance == null) return;

            Transform anchor = promptAnchor != null ? promptAnchor : transform;
            UIManager.Instance.ShowInteractPrompt(this, anchor, PlayerInput.BindableAction.Interact, promptFormat);
        }

        private string ResolveDialogueNodeTitle()
        {
            for (int i = 0; i < dialogueBranches.Count; i++)
            {
                DialogueBranch branch = dialogueBranches[i];
                if (branch == null || string.IsNullOrWhiteSpace(branch.BranchNodeTitle))
                    continue;

                if (branch.IsMatch(inventoryState, itemDatabase))
                    return branch.BranchNodeTitle;
            }

            return yarnNodeTitle;
        }

        // Expose để dialogue có thể đọc tên NPC qua Yarn command nếu cần
        public string NpcName => npcName;
        public string YarnNodeTitle => yarnNodeTitle;
    }
}
