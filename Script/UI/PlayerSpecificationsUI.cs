using UnityEngine;

namespace DreamKnight.UI
{
    [DisallowMultipleComponent]
    public class PlayerSpecificationsUI : MonoBehaviour
    {
        private static PlayerSpecificationsUI instance;

        public static PlayerSpecificationsUI Instance => instance;

        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private PlayerHudUI playerHud;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            if (playerHud == null)
                playerHud = GetComponentInChildren<PlayerHudUI>(true);
        }

        private void OnEnable()
        {
            UIManager.PlayerSpecificationsUpdated += HandlePlayerSpecificationsUpdated;

            if (UIManager.TryGetLastSnapshot(out UIManager.PlayerSpecificationsSnapshot snapshot))
                ApplySnapshot(snapshot);
        }

        private void OnDisable()
        {
            UIManager.PlayerSpecificationsUpdated -= HandlePlayerSpecificationsUpdated;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void HandlePlayerSpecificationsUpdated(UIManager.PlayerSpecificationsSnapshot snapshot)
        {
            ApplySnapshot(snapshot);
        }

        private void ApplySnapshot(UIManager.PlayerSpecificationsSnapshot snapshot)
        {
            if (playerHud == null)
                return;

            playerHud.SetHealth(snapshot.currentHealth, snapshot.maxHealth);
            playerHud.SetStamina(snapshot.currentStamina, snapshot.maxStamina);
            playerHud.SetMana(snapshot.currentMana, snapshot.maxMana);
        }
    }
}
