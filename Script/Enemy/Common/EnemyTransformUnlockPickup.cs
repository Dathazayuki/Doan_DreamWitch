using DreamKnight.Player;
using DreamKnight.Systems.SkillTree;
using DreamKnight.UI;
using UnityEngine;

namespace DreamKnight.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyTransformUnlockPickup : MonoBehaviour
    {
        [SerializeField] private PlayerFormDataSO unlockFormData;
        [SerializeField] private GameObject ownerToDisable;
        [Header("Prompt")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private string promptFormat = "<sprite=57> CanTranform";

        private bool unlocked;
        private bool hasRuntimeHealth;
        private float runtimeHealth;

        public PlayerFormDataSO UnlockFormData => unlockFormData;

        public float GetRuntimeHealth(float fallbackMaxHealth)
        {
            if (!hasRuntimeHealth)
                return fallbackMaxHealth;

            return Mathf.Clamp(runtimeHealth, 0f, fallbackMaxHealth);
        }

        public void SetRuntimeHealth(float currentHealth)
        {
            hasRuntimeHealth = true;
            runtimeHealth = Mathf.Max(0f, currentHealth);
        }

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnEnable()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        public void Initialize(PlayerFormDataSO formData, GameObject owner)
        {
            unlockFormData = formData;
            ownerToDisable = owner;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTriggerOverlap(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleTriggerOverlap(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other))
                return;

            player.SetTransformCorpseProximity(false, this);

            if (UIManager.Instance != null)
                UIManager.Instance.HideInteractPrompt(this);
        }

        private void HandleTriggerOverlap(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other) || player.FormManager == null)
            {
                return;
            }

            if (player.IsTransformed)
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.HideInteractPrompt(this);
                return;
            }

            SkillTreeManager skillTreeManager = SkillTreeManager.Instance;
            if (skillTreeManager == null || !skillTreeManager.IsTransformUnlocked())
            {
                player.SetTransformCorpseProximity(false, this);
                if (UIManager.Instance != null)
                    UIManager.Instance.HideInteractPrompt(this);
                return;
            }

            if (unlockFormData == null)
            {
                return;
            }

            if (!unlocked)
            {
                unlocked = true;

                if (ownerToDisable != null)
                {
                    player.RegisterTransformCorpse(ownerToDisable, this);
                }
            }

            player.SetTransformCorpseProximity(true, this);

            if (UIManager.Instance != null)
            {
                Transform anchor = promptAnchor != null ? promptAnchor : transform;
                UIManager.Instance.ShowInteractPrompt(this, anchor, PlayerInput.BindableAction.Transform, promptFormat);
            }
        }

        private void OnDisable()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.HideInteractPrompt(this);
        }
    }
}
