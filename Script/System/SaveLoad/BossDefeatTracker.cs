using Mv;
using UnityEngine;

namespace DreamKnight.Systems.SaveLoad
{
    [DisallowMultipleComponent]
    public class BossDefeatTracker : MonoBehaviour
    {
        [SerializeField] private string bossId;
        [SerializeField] private MvEnemyBase boss;
        [SerializeField] private bool disableIfAlreadyDefeated = true;

        public string BossId => string.IsNullOrWhiteSpace(bossId) ? gameObject.scene.name + "/" + gameObject.name : bossId;

        private void Reset()
        {
            boss = GetComponent<MvEnemyBase>();
            GenerateIdIfEmpty();
        }

        private void Awake()
        {
            if (boss == null)
                boss = GetComponent<MvEnemyBase>();
        }

        private void OnEnable()
        {
            BossDefeatSaveService.OnDefeatStateLoaded += HandleDefeatStateLoaded;

            if (RefreshSavedDefeatState())
                return;

            if (boss != null)
                boss.OnDeath += HandleBossDeath;
        }

        private void OnDisable()
        {
            BossDefeatSaveService.OnDefeatStateLoaded -= HandleDefeatStateLoaded;

            if (boss != null)
                boss.OnDeath -= HandleBossDeath;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (boss == null)
                boss = GetComponent<MvEnemyBase>();

            GenerateIdIfEmpty();
        }
#endif

        private void HandleBossDeath()
        {
            BossDefeatSaveService.MarkDefeated(BossId);
            GameAutoSave.Request("boss_defeated");
        }

        private void HandleDefeatStateLoaded()
        {
            RefreshSavedDefeatState();
        }

        private bool RefreshSavedDefeatState()
        {
            if (!disableIfAlreadyDefeated || !BossDefeatSaveService.IsDefeated(BossId))
                return false;

            gameObject.SetActive(false);
            return true;
        }

        private void GenerateIdIfEmpty()
        {
            if (!string.IsNullOrWhiteSpace(bossId))
                return;

            bossId = $"{gameObject.scene.name}/{gameObject.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }
    }
}
