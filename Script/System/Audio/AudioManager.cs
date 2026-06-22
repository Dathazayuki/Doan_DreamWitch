using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Audio
{
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [System.Serializable]
        public struct BgmEntry
        {
            public string key;
            public AudioClip clip;
        }

        [System.Serializable]
        public struct SfxEntry
        {
            public string key;
            public int id; // Maps _AudioSE from MvFx
            public AudioClip clip;
        }

        [Header("Audio Library")]
        [SerializeField] private List<BgmEntry> bgmList = new List<BgmEntry>();
        [SerializeField] private List<SfxEntry> sfxList = new List<SfxEntry>();

        [Header("SFX Pool Settings")]
        [SerializeField] private int sfxPoolSize = 16;

        // PlayerPrefs Keys
        private const string Pref_MasterVolume = "Volume_Master";
        private const string Pref_BgmVolume = "Volume_BGM";
        private const string Pref_SfxVolume = "Volume_SFX";

        // Volumes (0.0f to 1.0f)
        private float masterVolume = 1.0f;
        private float bgmVolume = 0.8f;
        private float sfxVolume = 0.8f;

        public float MasterVolume => masterVolume;
        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;

        // Audio Sources
        private AudioSource[] bgmSources = new AudioSource[2]; // For crossfading
        private int activeBgmIndex = -1;
        private Coroutine crossfadeCoroutine;

        private List<AudioSource> sfxSourcesPool = new List<AudioSource>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioSources()
        {
            // Create BGM sources
            for (int i = 0; i < 2; i++)
            {
                GameObject bgmObj = new GameObject($"BGM_Source_{i}");
                bgmObj.transform.SetParent(transform);
                AudioSource src = bgmObj.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = true;
                src.spatialBlend = 0f; // 2D BGM
                bgmSources[i] = src;
            }

            // Create SFX pool
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFX_Source_{i}");
                sfxObj.transform.SetParent(transform);
                AudioSource src = sfxObj.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f; // Default 2D
                sfxSourcesPool.Add(src);
            }
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(Pref_MasterVolume, 1.0f);
            bgmVolume = PlayerPrefs.GetFloat(Pref_BgmVolume, 0.8f);
            sfxVolume = PlayerPrefs.GetFloat(Pref_SfxVolume, 0.8f);

            ApplyBgmVolume();
            ApplySfxVolume();
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(Pref_MasterVolume, masterVolume);
            PlayerPrefs.SetFloat(Pref_BgmVolume, bgmVolume);
            PlayerPrefs.SetFloat(Pref_SfxVolume, sfxVolume);
            PlayerPrefs.Save();
        }

        #region Volume Control APIs

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyBgmVolume();
            ApplySfxVolume();
            SaveSettings();
        }

        public void SetBgmVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            ApplyBgmVolume();
            SaveSettings();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            ApplySfxVolume();
            SaveSettings();
        }

        private void ApplyBgmVolume()
        {
            float targetVolume = masterVolume * bgmVolume;
            if (activeBgmIndex >= 0 && crossfadeCoroutine == null)
            {
                bgmSources[activeBgmIndex].volume = targetVolume;
            }
        }

        private void ApplySfxVolume()
        {
            float targetVolume = masterVolume * sfxVolume;
            // Update currently active SFX pool sources
            for (int i = 0; i < sfxSourcesPool.Count; i++)
            {
                if (sfxSourcesPool[i].isPlaying)
                {
                    sfxSourcesPool[i].volume = targetVolume;
                }
            }
        }

        #endregion

        #region BGM Playback

        public void PlayBGM(AudioClip clip, float fadeDuration = 1.0f)
        {
            if (clip == null) return;

            // Check if same clip is already playing as BGM
            if (activeBgmIndex >= 0 && bgmSources[activeBgmIndex].clip == clip && bgmSources[activeBgmIndex].isPlaying)
            {
                return;
            }

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            crossfadeCoroutine = StartCoroutine(CrossfadeBGMRoutine(clip, fadeDuration));
        }

        public void PlayBGM(string key, float fadeDuration = 1.0f)
        {
            AudioClip clip = FindBgmClip(key);
            if (clip != null)
            {
                PlayBGM(clip, fadeDuration);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] BGM key '{key}' not found in library.");
            }
        }

        public void StopBGM(float fadeDuration = 1.0f)
        {
            if (activeBgmIndex < 0) return;

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            crossfadeCoroutine = StartCoroutine(CrossfadeBGMRoutine(null, fadeDuration));
        }

        private IEnumerator CrossfadeBGMRoutine(AudioClip newClip, float duration)
        {
            int newBgmIndex = (activeBgmIndex == 0) ? 1 : 0;
            AudioSource newSource = bgmSources[newBgmIndex];
            AudioSource oldSource = (activeBgmIndex >= 0) ? bgmSources[activeBgmIndex] : null;

            float targetMaxVolume = masterVolume * bgmVolume;

            if (newClip != null)
            {
                newSource.clip = newClip;
                newSource.volume = 0f;
                newSource.Play();
            }

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = duration > 0.001f ? Mathf.Clamp01(timer / duration) : 1f;

                if (oldSource != null)
                {
                    oldSource.volume = Mathf.Lerp(targetMaxVolume, 0f, progress);
                }

                if (newClip != null)
                {
                    newSource.volume = Mathf.Lerp(0f, targetMaxVolume, progress);
                }

                yield return null;
            }

            if (oldSource != null)
            {
                oldSource.Stop();
                oldSource.clip = null;
            }

            if (newClip != null)
            {
                newSource.volume = targetMaxVolume;
                activeBgmIndex = newBgmIndex;
            }
            else
            {
                activeBgmIndex = -1;
            }

            crossfadeCoroutine = null;
        }

        #endregion

        #region SFX Playback

        public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchRandomness = 0f)
        {
            if (clip == null) return;

            AudioSource source = GetFreeSfxSource();
            if (source != null)
            {
                source.gameObject.transform.SetParent(transform);
                source.spatialBlend = 0f; // 2D Sound
                source.clip = clip;
                source.loop = false;
                source.volume = masterVolume * sfxVolume * volumeScale;
                source.pitch = 1.0f + Random.Range(-pitchRandomness, pitchRandomness);
                source.Play();
            }
        }

        public void PlaySFX(string key, float volumeScale = 1f, float pitchRandomness = 0f)
        {
            AudioClip clip = FindSfxClip(key);
            if (clip != null)
            {
                PlaySFX(clip, volumeScale, pitchRandomness);
            }
        }

        public void PlaySFX(int seId, float volumeScale = 1f, float pitchRandomness = 0f)
        {
            AudioClip clip = FindSfxClipById(seId);
            if (clip != null)
            {
                PlaySFX(clip, volumeScale, pitchRandomness);
            }
        }

        public void PlaySFXAt(AudioClip clip, Vector3 position, float volumeScale = 1f, float pitchRandomness = 0f, bool ignoreDistLimit = false)
        {
            if (clip == null) return;

            if (ignoreDistLimit)
            {
                PlaySFX(clip, volumeScale, pitchRandomness);
                return;
            }

            AudioSource source = GetFreeSfxSource();
            if (source != null)
            {
                source.gameObject.transform.position = position;
                source.gameObject.transform.SetParent(null); // Detach so it stays at the world position
                source.spatialBlend = 1f; // 3D Spatial Sound
                source.clip = clip;
                source.loop = false;
                source.volume = masterVolume * sfxVolume * volumeScale;
                source.pitch = 1.0f + Random.Range(-pitchRandomness, pitchRandomness);
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 2f;
                source.maxDistance = 25f;
                source.Play();
            }
        }

        public void PlaySFXAt(string key, Vector3 position, float volumeScale = 1f, float pitchRandomness = 0f, bool ignoreDistLimit = false)
        {
            AudioClip clip = FindSfxClip(key);
            if (clip != null)
            {
                PlaySFXAt(clip, position, volumeScale, pitchRandomness, ignoreDistLimit);
            }
        }

        public void PlaySFXAt(int seId, Vector3 position, float volumeScale = 1f, float pitchRandomness = 0f, bool ignoreDistLimit = false)
        {
            AudioClip clip = FindSfxClipById(seId);
            if (clip != null)
            {
                PlaySFXAt(clip, position, volumeScale, pitchRandomness, ignoreDistLimit);
            }
        }

        private AudioSource GetFreeSfxSource()
        {
            // Find a source that isn't playing
            for (int i = 0; i < sfxSourcesPool.Count; i++)
            {
                if (!sfxSourcesPool[i].isPlaying)
                {
                    return sfxSourcesPool[i];
                }
            }

            // Fallback: spawn a new one and grow the pool
            GameObject sfxObj = new GameObject($"SFX_Source_{sfxSourcesPool.Count}");
            sfxObj.transform.SetParent(transform);
            AudioSource src = sfxObj.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            sfxSourcesPool.Add(src);
            return src;
        }

        #endregion

        #region Helpers

        private AudioClip FindBgmClip(string key)
        {
            for (int i = 0; i < bgmList.Count; i++)
            {
                if (bgmList[i].key == key)
                    return bgmList[i].clip;
            }
            return null;
        }

        private AudioClip FindSfxClip(string key)
        {
            for (int i = 0; i < sfxList.Count; i++)
            {
                if (sfxList[i].key == key)
                    return sfxList[i].clip;
            }
            return null;
        }

        private AudioClip FindSfxClipById(int id)
        {
            for (int i = 0; i < sfxList.Count; i++)
            {
                if (sfxList[i].id == id)
                    return sfxList[i].clip;
            }
            return null;
        }

        #endregion
    }
}
