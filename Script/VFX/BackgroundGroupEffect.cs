using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.VFX
{
    /// <summary>
    /// Gắn script này vào object CHA chứa tất cả sprite background.
    /// Tự động tìm toàn bộ SpriteRenderer trong children và áp hiệu ứng:
    ///   - Tối màu (darkening) : luôn hoạt động, không cần shader
    ///   - Làm mờ  (blur)      : cần gán BlurMaterial vào Inspector
    /// 
    /// Cách dùng:
    ///   effect.SetEffect(true);   // Bật làm mờ/tối
    ///   effect.SetEffect(false);  // Tắt, trả về trạng thái gốc
    ///   effect.SetEffectImmediate(true); // Bật ngay không transition
    /// </summary>
    [DisallowMultipleComponent]
    public class BackgroundGroupEffect : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Inspector
        // ----------------------------------------------------------------
        [Header("Darkness (luôn hoạt động)")]
        [SerializeField, Range(0f, 1f)]
        private float darknessAmount = 0.45f;   // 0 = không tối, 1 = đen hoàn toàn

        [SerializeField, Range(0f, 1f)]
        private float targetAlpha = 1f;         // Alpha mong muốn khi bật hiệu ứng

        [Header("Blur (cần gán BlurMaterial)")]
        [SerializeField]
        private Material blurMaterial;          // Kéo material dùng shader blur vào đây

        [Header("Transition")]
        [SerializeField]
        private float transitionSpeed = 5f;     // Tốc độ fade in/out

        [Header("Advanced")]
        [SerializeField]
        private bool applyOnAwake = true;       // Tự bật hiệu ứng ngay khi chạy?
        [SerializeField]
        private bool includeInactive = false;   // Kể cả child đang bị tắt (SetActive false)?

        // ----------------------------------------------------------------
        // Runtime
        // ----------------------------------------------------------------
        private struct SpriteSnapshot
        {
            public SpriteRenderer Renderer;
            public Material       OriginalMaterial;
            public Color          OriginalColor;
        }

        private List<SpriteSnapshot> snapshots = new List<SpriteSnapshot>();
        private bool effectActive;
        private float currentBlend;   // 0 = gốc, 1 = hiệu ứng đầy đủ

        // ----------------------------------------------------------------
        // Lifecycle
        // ----------------------------------------------------------------
        private void Awake()
        {
            CacheChildRenderers();
        }

        private void Start()
        {
            if (applyOnAwake)
                SetEffectImmediate(true);
        }

        private void Update()
        {
            float target = effectActive ? 1f : 0f;
            if (Mathf.Approximately(currentBlend, target))
                return;

            currentBlend = Mathf.MoveTowards(currentBlend, target,
                                             transitionSpeed * Time.deltaTime);
            ApplyBlend(currentBlend);
        }

        // ----------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------

        /// <summary>Bật/tắt hiệu ứng với transition mượt.</summary>
        public void SetEffect(bool active)
        {
            effectActive = active;
        }

        /// <summary>Bật/tắt hiệu ứng ngay lập tức, không transition.</summary>
        public void SetEffectImmediate(bool active)
        {
            effectActive = active;
            currentBlend = active ? 1f : 0f;
            ApplyBlend(currentBlend);
        }

        /// <summary>Thay đổi mức độ tối (0–1) tức thời.</summary>
        public void SetDarknessAmount(float amount)
        {
            darknessAmount = Mathf.Clamp01(amount);
            if (effectActive)
                ApplyBlend(currentBlend);
        }

        /// <summary>Quét lại children (gọi khi spawn thêm object con lúc runtime).</summary>
        public void RefreshChildRenderers()
        {
            bool wasActive = effectActive;
            SetEffectImmediate(false);   // Reset về gốc trước khi quét lại
            CacheChildRenderers();
            if (wasActive)
                SetEffectImmediate(true);
        }

        // ----------------------------------------------------------------
        // Internal
        // ----------------------------------------------------------------
        private void CacheChildRenderers()
        {
            snapshots.Clear();

            SpriteRenderer[] renderers =
                GetComponentsInChildren<SpriteRenderer>(includeInactive);

            foreach (SpriteRenderer sr in renderers)
            {
                if (sr == null) continue;

                snapshots.Add(new SpriteSnapshot
                {
                    Renderer         = sr,
                    OriginalMaterial = sr.sharedMaterial,   // sharedMaterial: không tạo instance copy
                    OriginalColor    = sr.color
                });
            }

            Debug.Log($"[BackgroundGroupEffect] '{gameObject.name}': " +
                      $"đã cache {snapshots.Count} SpriteRenderer trong children.");
        }

        private void ApplyBlend(float blend)
        {
            // blend = 0 → trạng thái gốc
            // blend = 1 → hiệu ứng đầy đủ (tối + blur)

            foreach (SpriteSnapshot snap in snapshots)
            {
                if (snap.Renderer == null) continue;

                // --- Màu sắc (Darkness) ---
                Color original = snap.OriginalColor;
                float r = Mathf.Lerp(original.r, original.r * (1f - darknessAmount), blend);
                float g = Mathf.Lerp(original.g, original.g * (1f - darknessAmount), blend);
                float b = Mathf.Lerp(original.b, original.b * (1f - darknessAmount), blend);
                float a = Mathf.Lerp(original.a, original.a * targetAlpha,            blend);
                snap.Renderer.color = new Color(r, g, b, a);

                // --- Material (Blur) nếu có ---
                if (blurMaterial != null)
                {
                    // blend >= 0.5 → đổi sang blur material
                    // blend <  0.5 → trả về material gốc
                    // Dùng sharedMaterial để không tạo instance mới mỗi frame
                    snap.Renderer.sharedMaterial = blend >= 0.5f
                        ? blurMaterial
                        : snap.OriginalMaterial;
                }
            }
        }

        private void OnDestroy()
        {
            // Trả về trạng thái gốc nếu object bị destroy
            SetEffectImmediate(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Preview trong Editor khi kéo slider
            if (!Application.isPlaying && snapshots.Count == 0)
                CacheChildRenderers();

            if (Application.isPlaying)
                ApplyBlend(effectActive ? 1f : 0f);
        }
#endif
    }
}
