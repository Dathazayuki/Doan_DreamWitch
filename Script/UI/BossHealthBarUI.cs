using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.UI
{
    [DisallowMultipleComponent]
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Image fillImage;

        private void Awake()
        {
            ResolveReferences();
        }

        public void SetBossName(string bossName)
        {
            ResolveReferences();

            if (bossNameText != null)
                bossNameText.text = bossName;
        }

        public void SetHealth(float current, float max)
        {
            ResolveReferences();

            float value = max > 0f ? current / max : 0f;
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(value);

            if (hpText != null)
                hpText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, current))}/{Mathf.CeilToInt(Mathf.Max(0f, max))}";
        }

        public void SetVisible(bool visible)
        {
            GameObject target = root != null ? root : gameObject;
            if (target.activeSelf != visible)
                target.SetActive(visible);
        }

        private void ResolveReferences()
        {
            if (root == null)
                root = gameObject;

            if (fillImage == null)
                fillImage = FindFillImage();

            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (bossNameText == null && texts.Length > 0)
                bossNameText = texts[0];

            if (hpText == null && texts.Length > 1)
                hpText = texts[1];
        }

        private Image FindFillImage()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.name.ToLowerInvariant().Contains("fill"))
                    return image;
            }

            return images.Length > 0 ? images[images.Length - 1] : null;
        }
    }
}
