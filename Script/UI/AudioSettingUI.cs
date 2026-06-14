using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DreamKnight.Systems.Audio;

namespace DreamKnight.UI
{
    [DisallowMultipleComponent]
    public class AudioSettingUI : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Percentage Text (Optional)")]
        [SerializeField] private TMP_Text masterPercentageText;
        [SerializeField] private TMP_Text bgmPercentageText;
        [SerializeField] private TMP_Text sfxPercentageText;

        [Header("SFX Preview Feedback")]
        [SerializeField] private AudioClip previewSfxClip;
        [SerializeField] private float previewDelay = 0.15f;
        private float lastPreviewTime;

        private void OnEnable()
        {
            InitializeSliders();
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void InitializeSliders()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("[AudioSettingUI] AudioManager.Instance is null, cannot initialize volumes.");
                return;
            }

            if (masterSlider != null)
            {
                masterSlider.value = AudioManager.Instance.MasterVolume;
                UpdatePercentageText(masterPercentageText, masterSlider.value);
            }

            if (bgmSlider != null)
            {
                bgmSlider.value = AudioManager.Instance.BgmVolume;
                UpdatePercentageText(bgmPercentageText, bgmSlider.value);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = AudioManager.Instance.SfxVolume;
                UpdatePercentageText(sfxPercentageText, sfxSlider.value);
            }
        }

        private void BindEvents()
        {
            if (masterSlider != null)
                masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (bgmSlider != null)
                bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        private void UnbindEvents()
        {
            if (masterSlider != null)
                masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

            if (bgmSlider != null)
                bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
            UpdatePercentageText(masterPercentageText, value);
        }

        private void OnBgmVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBgmVolume(value);
            }
            UpdatePercentageText(bgmPercentageText, value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxVolume(value);
            }
            UpdatePercentageText(sfxPercentageText, value);
            PlayPreviewSound();
        }

        private void PlayPreviewSound()
        {
            if (AudioManager.Instance == null || previewSfxClip == null) return;

            if (Time.unscaledTime >= lastPreviewTime + previewDelay)
            {
                AudioManager.Instance.PlaySFX(previewSfxClip);
                lastPreviewTime = Time.unscaledTime;
            }
        }

        private void UpdatePercentageText(TMP_Text textComponent, float value)
        {
            if (textComponent != null)
            {
                int percentage = Mathf.RoundToInt(value * 100f);
                textComponent.text = $"{percentage}%";
            }
        }
    }
}
