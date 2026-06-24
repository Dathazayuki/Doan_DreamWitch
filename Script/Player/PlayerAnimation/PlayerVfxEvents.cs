using UnityEngine;
using DreamKnight.Player.States;

namespace DreamKnight.Player
{
    public class PlayerVfxEvents : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private VfxPoolManager vfxPoolManager;
        [SerializeField] private Transform defaultSpawnPoint;
        [SerializeField] private bool parentToSpawner = false;

        [Header("Animation Event Prefabs")]
        [SerializeField] private GameObject se0Prefab;
        [SerializeField] private GameObject se1Prefab;
        [SerializeField] private GameObject se2Prefab;
        [SerializeField] private GameObject fx0Prefab;
        [SerializeField] private GameObject fx1Prefab;
        [SerializeField] private GameObject atkS1Prefab;
        [SerializeField] private GameObject atkS2Prefab;
        [SerializeField] private GameObject atkS3Prefab;
        [SerializeField] private GameObject atkSAirPrefab;
        [SerializeField] private GameObject cancelRPrefab;
        [SerializeField] private GameObject trg0Prefab;
        [SerializeField] private GameObject atkEPrefab;
        [SerializeField] private GameObject cancelSPrefab;
        [SerializeField] private GameObject cancelMovePrefab;
        [SerializeField] private GameObject cancelEPrefab;
        [SerializeField] private GameObject hitPrefab;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponentInParent<PlayerController>();
            }

            EnsureVfxPoolManager();
        }

        private void OnEnable()
        {
            EnsureVfxPoolManager();
        }

        public void SE0()
        {
            SpawnVfx(se0Prefab);
        }

        public void SE1()
        {
            SpawnVfx(se1Prefab);
        }

        public void SE2()
        {
            SpawnVfx(se2Prefab);
        }

        public void Fx0()
        {
            SpawnVfx(fx0Prefab != null ? fx0Prefab : se0Prefab);
        }

        public void Fx1()
        {
            SpawnVfx(fx1Prefab != null ? fx1Prefab : fx0Prefab != null ? fx0Prefab : se1Prefab);
        }

        public void AtkS()
        {
            AttackState attackState = playerController?.AttackState;
            GameObject prefab;
            if (attackState != null && attackState == playerController.StateMachine?.CurrentState)
            {
                if (attackState.IsUpAttack)
                    prefab = atkSAirPrefab != null ? atkSAirPrefab : atkS1Prefab;
                else if (attackState.ComboStep == 2)
                    prefab = atkS2Prefab != null ? atkS2Prefab : atkS1Prefab;
                else if (attackState.ComboStep == 3)
                    prefab = atkS3Prefab != null ? atkS3Prefab : atkS1Prefab;
                else
                    prefab = atkS1Prefab;
            }
            else
            {
                prefab = atkS1Prefab;
            }
            SpawnVfx(prefab);
        }

        public void CancelR()
        {
            SpawnVfx(cancelRPrefab);
            playerController?.OnAttackEnd();
        }

        public void Trg0()
        {
            // Chỉ gọi melee damage khi đang trong AttackState (Human hoặc Form-specific)
            // Tránh trường hợp animation SKILL_FASTSHOT (ném dao) cũng phát Trg0 event
            // và vô tình kích hoạt melee hitbox cùng lúc với dao
            if (playerController == null) return;

            if (!playerController.IsInAnyAttackState) return;

            bool hitConnected = playerController.OnAttackHit();
            if (!hitConnected) return;
            SpawnVfx(trg0Prefab);
        }

        public void AtkE()
        {
            SpawnVfx(atkEPrefab);
            playerController?.OnAttackEnd();
        }

        public void CancelS()
        {
            SpawnVfx(cancelSPrefab);
        }

        public void CancelMove()
        {
            SpawnVfx(cancelMovePrefab);
        }

        public void CancelE()
        {
            SpawnVfx(cancelEPrefab);
        }

        public void Hit()
        {
            SpawnVfx(hitPrefab);
        }

        private void SpawnVfx(GameObject prefab)
        {
            if (prefab == null) return;

            EnsureVfxPoolManager();

            Transform spawnPoint = defaultSpawnPoint != null ? defaultSpawnPoint : transform;
            Transform parent = parentToSpawner ? spawnPoint : null;
            vfxPoolManager?.Spawn(prefab, spawnPoint.position, spawnPoint.rotation, parent);
        }

        private void EnsureVfxPoolManager()
        {
            if (vfxPoolManager == null)
                vfxPoolManager = VfxPoolManager.Instance;
        }
    }
}
