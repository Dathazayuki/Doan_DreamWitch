using System.Collections;
using DreamKnight.Interfaces;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    [DisallowMultipleComponent]
    public class DestructibleBlock : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 1f;

        [Header("Visual")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private GameObject breakVfxObject;
        [SerializeField] private bool disableVfxOnAwake = true;
        [SerializeField] private float vfxDuration = 1f;

        [Header("Collision")]
        [SerializeField] private Collider2D[] blockColliders;
        [SerializeField] private bool disableCollidersOnBreak = true;

        private SpriteRenderer[] spriteRenderers;
        private float currentHealth;
        private bool broken;
        private Coroutine vfxRoutine;

        public bool IsAlive => !broken;
        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            currentHealth = Mathf.Max(1f, maxHealth);

            if (blockColliders == null || blockColliders.Length == 0)
                blockColliders = GetComponentsInChildren<Collider2D>();

            spriteRenderers = visualRoot != null
                ? visualRoot.GetComponentsInChildren<SpriteRenderer>(true)
                : GetComponentsInChildren<SpriteRenderer>(true);

            if (disableVfxOnAwake && breakVfxObject != null)
                breakVfxObject.SetActive(false);
        }

        public void TakeDamage(float damage, GameObject damageSource = null)
        {
            if (broken)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, damage));

            if (currentHealth > 0f)
                return;

            Break();
        }

        private void Break()
        {
            broken = true;
            currentHealth = 0f;

            SetVisualVisible(false);
            SetCollidersEnabled(false);

            if (vfxRoutine != null)
                StopCoroutine(vfxRoutine);

            vfxRoutine = StartCoroutine(PlayBreakVfxRoutine());
        }

        private IEnumerator PlayBreakVfxRoutine()
        {
            if (breakVfxObject == null)
                yield break;

            breakVfxObject.SetActive(true);

            float duration = Mathf.Max(0f, vfxDuration);
            if (duration > 0f)
                yield return new WaitForSeconds(duration);

            breakVfxObject.SetActive(false);
            vfxRoutine = null;
        }

        private void SetVisualVisible(bool visible)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(visible);
                return;
            }

            if (spriteRenderers == null)
                return;

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer == null)
                    continue;

                if (breakVfxObject != null && spriteRenderer.transform.IsChildOf(breakVfxObject.transform))
                    continue;

                spriteRenderer.enabled = visible;
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (!disableCollidersOnBreak || blockColliders == null)
                return;

            for (int i = 0; i < blockColliders.Length; i++)
            {
                if (blockColliders[i] != null)
                    blockColliders[i].enabled = enabled;
            }
        }
    }
}
