using DreamKnight.Player;
using DreamKnight.UI;
using System.Collections;
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
        [Header("Yarn Dialogue")]
        [Tooltip("Tên node bắt đầu trong file .yarn, VD: Merchant_Intro")]
        [SerializeField] private string yarnNodeTitle = "NPC_DefaultTalk";

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
            yarnDialogueRunner.StartDialogue(yarnNodeTitle);
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

        // Expose để dialogue có thể đọc tên NPC qua Yarn command nếu cần
        public string NpcName => npcName;
        public string YarnNodeTitle => yarnNodeTitle;
    }
}
