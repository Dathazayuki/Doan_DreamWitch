using System.Collections.Generic;
using UnityEngine;
using DreamKnight.Player;
using DreamKnight.UI;

namespace DreamKnight.Systems.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class SpellBookChest : MonoBehaviour
    {
        [Header("Identity & Persistence")]
        [SerializeField] private string uniqueId;

        [Header("Randomization")]
        [SerializeField] private bool randomizeFromDatabase = true;
        [SerializeField] private SpellBookDatabaseSO spellBookDatabase;

        [Header("Spell Books (Exactly 3 if not random)")]
        [SerializeField] private SpellBookSO[] spellBooks = new SpellBookSO[3];

        [Header("Interaction Prompt")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private string promptFormat = "<sprite=192> Open Grimoire Chest";
        [SerializeField] private float interactCooldown = 0.5f;

        [Header("Visual States")]
        [SerializeField] private GameObject closedVisual;
        [SerializeField] private GameObject openedVisual;

        private PlayerController currentPlayer;
        private float nextInteractTime;
        private bool isOpened;

        // Persistent tracking of opened chests in this session
        private static readonly HashSet<string> openedChests = new HashSet<string>();

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

            // Restore opened state from persistence
            if (openedChests.Contains(uniqueId))
            {
                MarkAsOpenedImmediate();
            }
            else
            {
                SetVisualState(false);
                // Perform randomization if selected
                if (randomizeFromDatabase && spellBookDatabase != null && spellBookDatabase.SpellBooks.Count >= 3)
                {
                    List<SpellBookSO> allBooks = new List<SpellBookSO>(spellBookDatabase.SpellBooks);
                    List<SpellBookSO> selected = new List<SpellBookSO>();
                    for (int i = 0; i < 3; i++)
                    {
                        int randomIndex = Random.Range(0, allBooks.Count);
                        selected.Add(allBooks[randomIndex]);
                        allBooks.RemoveAt(randomIndex); // Prevent duplicates
                    }
                    spellBooks = selected.ToArray();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isOpened) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other)) return;

            currentPlayer = player;
            ShowInteractPrompt();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (isOpened || currentPlayer == null) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other)) return;

            if (player.Input == null || !player.Input.InteractPressed) return;
            if (Time.time < nextInteractTime) return;

            nextInteractTime = Time.time + interactCooldown;
            OpenChest();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != currentPlayer || !player.IsBodyCollider(other)) return;

            HideInteractPrompt();
            currentPlayer = null;
        }

        private void OpenChest()
        {
            if (SpellBookSelectionUI.Instance == null)
            {
                Debug.LogError("[SpellBookChest] SpellBookSelectionUI.Instance is not found in the scene.");
                return;
            }

            HideInteractPrompt();

            // Open Selection UI
            SpellBookSelectionUI.Instance.Open(spellBooks, currentPlayer, OnBookSelected);
        }

        private void OnBookSelected(int selectedIndex)
        {
            isOpened = true;
            openedChests.Add(uniqueId);
            SetVisualState(true);

            // Disable trigger collider to prevent further interactions
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;

            currentPlayer = null;
        }

        private void SetVisualState(bool opened)
        {
            if (closedVisual != null) closedVisual.SetActive(!opened);
            if (openedVisual != null) openedVisual.SetActive(opened);
        }

        private void MarkAsOpenedImmediate()
        {
            isOpened = true;
            SetVisualState(true);

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;
        }

        private void ShowInteractPrompt()
        {
            if (UIManager.Instance == null) return;
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
        }
#endif
    }
}
