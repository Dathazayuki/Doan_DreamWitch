using DreamKnight.Systems.Audio;
using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    public class PlayerAudioEvents : MonoBehaviour
    {
        [Header("SFX Ids")]
        [SerializeField] private int jumpSfxId;
        [SerializeField] private int dashSfxId;
        [SerializeField] private int deathSfxId;
        [SerializeField] private int shrineRespawnSfxId;
        [SerializeField] private int spellSfxId;
        [SerializeField] private int healSfxId;
        [SerializeField] private int toolSfxId;
        [SerializeField] private int potionSfxId;

        [Header("Playback")]
        [SerializeField, Range(0f, 2f)] private float volumeScale = 1f;
        [SerializeField, Range(0f, 0.3f)] private float pitchRandomness = 0.03f;

        public void PlayJump() => Play(jumpSfxId);
        public void PlayDash() => Play(dashSfxId);
        public void PlayDeath() => Play(deathSfxId);
        public void PlayShrineRespawn() => Play(shrineRespawnSfxId);
        public void PlaySpell() => Play(spellSfxId);
        public void PlayHeal() => Play(healSfxId);
        public void PlayTool() => Play(toolSfxId);
        public void PlayPotion() => Play(potionSfxId);

        private void Play(int sfxId)
        {
            if (sfxId <= 0 || AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(sfxId, volumeScale, pitchRandomness);
        }
    }
}
