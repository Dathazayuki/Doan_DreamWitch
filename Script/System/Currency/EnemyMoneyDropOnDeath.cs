using Mv;
using UnityEngine;

namespace DreamKnight.Systems.Currency
{
    [DisallowMultipleComponent]
    public class EnemyMoneyDropOnDeath : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MvEnemyBase enemy;
        [SerializeField] private MoneyPickup moneyPrefab;

        [Header("Money Range")]
        [SerializeField] private int minMoney = 1;
        [SerializeField] private int maxMoney = 5;

        [Header("Spawn")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);

        private bool dropped;

        private void Awake()
        {
            if (enemy == null)
                enemy = GetComponent<MvEnemyBase>();
        }

        private void OnEnable()
        {
            dropped = false;
            if (enemy != null)
                enemy.OnDeath += HandleEnemyDeath;
        }

        private void OnDisable()
        {
            if (enemy != null)
                enemy.OnDeath -= HandleEnemyDeath;
        }

        private void HandleEnemyDeath()
        {
            if (dropped || moneyPrefab == null)
                return;

            dropped = true;
            int value = Random.Range(Mathf.Min(minMoney, maxMoney), Mathf.Max(minMoney, maxMoney) + 1);

            MoneyPickup drop = MoneyPickupPoolManager.Instance.Spawn(moneyPrefab, transform.position + spawnOffset, Quaternion.identity);
            if (drop != null)
                drop.SetAmount(value);
        }
    }
}
