using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Player
{
    public class PlayerFormConfig : MonoBehaviour
    {
        [Tooltip("Danh sách các form biến hình. Gán các PlayerFormDataSO asset vào đây.")]
        public List<PlayerFormDataSO> forms = new List<PlayerFormDataSO>();

        // ── Runtime state (không lưu vào asset) ──────────────────────────────
        private readonly Dictionary<PlayerFormId, float> runtimeHealthByForm
            = new Dictionary<PlayerFormId, float>();

        private PlayerFormDataSO unlockedForm;

        // ── Queries ───────────────────────────────────────────────────────────

        public PlayerFormDataSO GetUnlockedForm()
        {
            return unlockedForm;
        }

        public float GetFormMaxHealth(PlayerFormId formId)
        {
            PlayerFormDataSO entry = GetFormById(formId);
            return entry != null ? Mathf.Max(0.0001f, entry.maxHealth) : 0f;
        }

        public float GetFormRuntimeHealth(PlayerFormId formId)
        {
            if (runtimeHealthByForm.TryGetValue(formId, out float hp))
                return hp;

            return GetFormMaxHealth(formId);
        }

        public void SetFormRuntimeHealth(PlayerFormId formId, float currentHealth)
        {
            if (formId == PlayerFormId.Human) return;
            runtimeHealthByForm[formId] = Mathf.Max(0f, currentHealth);
        }

        public bool HasUnlockedForm => unlockedForm != null;

        public bool IsFormUnlocked(PlayerFormDataSO formData)
        {
            return unlockedForm == formData;
        }

        public bool UnlockForm(PlayerFormDataSO formData)
        {
            if (formData == null) return false;
            if (!forms.Contains(formData)) return false;

            unlockedForm = formData;
            return true;
        }

        public void ClearUnlockedForm()
        {
            unlockedForm = null;
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private PlayerFormDataSO GetFormById(PlayerFormId formId)
        {
            for (int i = 0; i < forms.Count; i++)
            {
                PlayerFormDataSO entry = forms[i];
                if (entry != null && entry.formId == formId)
                    return entry;
            }
            return null;
        }

    }
}
