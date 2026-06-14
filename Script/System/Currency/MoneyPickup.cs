using DreamKnight.Player;
using DreamKnight.UI;
using UnityEngine;

namespace DreamKnight.Systems.Currency
{
    [DisallowMultipleComponent]
    public class MoneyPickup : MonoBehaviour
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private float magnetDelay = 0.12f;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float collectDistance = 0.35f;
        [SerializeField] private Vector2 spawnScatterVelocity = new Vector2(1.2f, 2.2f);

        private Transform target;
        private float aliveTime;
        private bool collected;
        private bool magnetActive;
        private Rigidbody2D rb;
        private Collider2D[] cachedColliders;
        private MoneyPickupPoolManager poolManager;
        private int sourcePrefabId;

        public int Amount => amount;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            cachedColliders = GetComponentsInChildren<Collider2D>(true);
        }

        private void OnEnable()
        {
            collected = false;
            aliveTime = 0f;
            target = null;
            magnetActive = false;
            SetCollidersEnabled(true);

            if (rb != null)
            {
                float dir = Random.value < 0.5f ? -1f : 1f;
                rb.linearVelocity = new Vector2(spawnScatterVelocity.x * dir, spawnScatterVelocity.y);
            }
        }

        private void Update()
        {
            if (collected)
                return;

            aliveTime += Time.deltaTime;
            if (target == null)
                ResolveTarget();

            if (target == null || aliveTime < magnetDelay)
                return;

            if (!magnetActive)
            {
                magnetActive = true;
                SetCollidersEnabled(false);
            }

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            Vector3 current = transform.position;
            Vector3 destination = target.position;
            float step = Mathf.Max(0.01f, moveSpeed) * Time.deltaTime;
            transform.position = Vector3.MoveTowards(current, destination, step);

            if (Vector3.SqrMagnitude(destination - transform.position) <= collectDistance * collectDistance)
                Collect();
        }

        public void SetAmount(int value)
        {
            amount = Mathf.Max(1, value);
        }

        public void InitializePool(MoneyPickupPoolManager manager, int prefabId)
        {
            poolManager = manager;
            sourcePrefabId = prefabId;
        }

        private void ResolveTarget()
        {
            if (PersistentPlayerRoot.Instance != null)
            {
                target = PersistentPlayerRoot.Instance.transform;
                return;
            }

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                target = player.transform;
        }

        private void Collect()
        {
            if (collected)
                return;

            collected = true;
            UIManager.Instance?.EnqueueMoneyPickup(amount);

            if (poolManager != null)
                poolManager.Release(this, sourcePrefabId);
            else
                Destroy(gameObject);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (cachedColliders == null)
                return;

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                Collider2D col = cachedColliders[i];
                if (col != null)
                    col.enabled = enabled;
            }
        }
    }
}
