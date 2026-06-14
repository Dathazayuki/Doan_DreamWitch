using UnityEngine;

namespace DreamKnight.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public class PersistentPlayerRoot : MonoBehaviour
    {
        private static PersistentPlayerRoot instance;

        public static PersistentPlayerRoot Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (gameObject.scene.name != "DontDestroyOnLoad")
                DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
