using Unity.Cinemachine;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerHitFeedback : MonoBehaviour
    {
        [Header("Hit Camera Shake")]
        [SerializeField] private CinemachineCamera hitShakeCinemachineCamera;
        [SerializeField] private float hitShakeAmplitude = 1.2f;
        [SerializeField] private float hitShakeFrequency = 2.2f;
        [SerializeField] private float hitShakeDuration = 0.12f;

        private Coroutine hitShakeCoroutine;
        private bool hasCachedPerlinValues;
        private float cachedAmplitude;
        private float cachedFrequency;

        public void Initialize(CinemachineCamera fallbackCamera)
        {
            if (hitShakeCinemachineCamera == null)
                hitShakeCinemachineCamera = fallbackCamera;
        }

        public void PlayHitCameraShake()
        {
            if (hitShakeCinemachineCamera == null)
                return;

            CinemachineBasicMultiChannelPerlin perlin = hitShakeCinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (perlin == null)
                return;

            if (hitShakeCoroutine != null)
            {
                StopCoroutine(hitShakeCoroutine);
                RestorePerlin(perlin);
            }

            CachePerlin(perlin);

            hitShakeCoroutine = StartCoroutine(HitCameraShakeCoroutine(perlin));
        }

        private System.Collections.IEnumerator HitCameraShakeCoroutine(CinemachineBasicMultiChannelPerlin perlin)
        {
            perlin.AmplitudeGain = hitShakeAmplitude;
            perlin.FrequencyGain = hitShakeFrequency;

            float timer = 0f;
            float duration = Mathf.Max(0.01f, hitShakeDuration);
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (perlin != null)
            {
                RestorePerlin(perlin);
            }

            hitShakeCoroutine = null;
        }

        private void CachePerlin(CinemachineBasicMultiChannelPerlin perlin)
        {
            if (perlin == null)
                return;

            cachedAmplitude = perlin.AmplitudeGain;
            cachedFrequency = perlin.FrequencyGain;
            hasCachedPerlinValues = true;
        }

        private void RestorePerlin(CinemachineBasicMultiChannelPerlin perlin)
        {
            if (perlin == null || !hasCachedPerlinValues)
                return;

            perlin.AmplitudeGain = cachedAmplitude;
            perlin.FrequencyGain = cachedFrequency;
        }
    }
}