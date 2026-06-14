using Mv;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealthBarLite : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MvEnemyBase enemy;

    [Header("Prefab")]
    [SerializeField] private SpriteHealthBar healthBarPrefab;

    [Header("World Offset")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Visibility")]
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private float visibleAfterHitDuration = 2f;

    private SpriteHealthBar runtimeHealthBar;
    private float visibleTimer;
    private float health01 = 1f;
    private float currentHealth;
    private float maxHealth;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<MvEnemyBase>();

        CreateRuntimeHealthBar();

        if (enemy != null)
        {
            currentHealth = enemy.CurrentHealth;
            maxHealth = enemy.MaxHealth;
            health01 = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            enemy.OnHealthChanged += OnEnemyHealthChanged;
            enemy.OnDeath += OnEnemyDeath;

            visibleTimer = hideWhenFull && health01 >= 0.999f ? 0f : visibleAfterHitDuration;
        }

        RefreshVisibility();
        RefreshHealthBar();
    }

    private void OnDestroy()
    {
        if (enemy != null)
        {
            enemy.OnHealthChanged -= OnEnemyHealthChanged;
            enemy.OnDeath -= OnEnemyDeath;
        }
    }

    private void Update()
    {
        if (visibleTimer > 0f)
            visibleTimer -= Time.deltaTime;

        RefreshVisibility();
    }

    private void OnEnemyHealthChanged(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
        health01 = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        visibleTimer = hideWhenFull && health01 >= 0.999f ? 0f : visibleAfterHitDuration;

        RefreshVisibility();
        RefreshHealthBar();
    }

    private void OnEnemyDeath()
    {
        currentHealth = 0f;
        health01 = 0f;
        visibleTimer = 0f;
        RefreshVisibility();
        RefreshHealthBar();
    }

    private void CreateRuntimeHealthBar()
    {
        if (healthBarPrefab == null)
        {
            Debug.LogWarning($"[EnemyHealthBarLite] {name} chua gan Health Bar Sprite Prefab.");
            return;
        }

        runtimeHealthBar = Instantiate(healthBarPrefab, transform);
        runtimeHealthBar.transform.localPosition = localOffset;
        runtimeHealthBar.transform.localRotation = Quaternion.identity;
    }

    private void RefreshHealthBar()
    {
        if (runtimeHealthBar != null)
            runtimeHealthBar.SetHealth(currentHealth, maxHealth);
    }

    private void RefreshVisibility()
    {
        bool shouldShow = enemy != null
            && enemy.IsAlive
            && runtimeHealthBar != null
            && (!hideWhenFull || health01 < 0.999f || visibleTimer > 0f);

        if (runtimeHealthBar != null)
            runtimeHealthBar.SetVisible(shouldShow);
    }
}
