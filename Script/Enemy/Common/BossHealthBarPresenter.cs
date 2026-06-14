using DreamKnight.UI;
using Mv;
using UnityEngine;

[DisallowMultipleComponent]
public class BossHealthBarPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MvEnemyBase boss;
    [SerializeField] private Camera uiCamera;

    [Header("UI")]
    [SerializeField] private Canvas bossHealthBarCanvasPrefab;
    [SerializeField] private string bossName = "Boss";
    [SerializeField] private bool showOnAwake = true;
    [SerializeField] private bool hideOnDeath = true;

    private Canvas runtimeCanvas;
    private BossHealthBarUI runtimeView;
    private float currentHealth;
    private float maxHealth;

    private void Awake()
    {
        if (boss == null)
            boss = GetComponentInParent<MvEnemyBase>();

        if (uiCamera == null)
            uiCamera = Camera.main;

        CreateRuntimeView();
        SubscribeBoss();
        RefreshView();
    }

    private void OnDestroy()
    {
        UnsubscribeBoss();

        if (runtimeCanvas != null)
            Destroy(runtimeCanvas.gameObject);
    }

    private void CreateRuntimeView()
    {
        if (bossHealthBarCanvasPrefab == null)
        {
            Debug.LogWarning($"[BossHealthBarPresenter] {name} chua gan Boss Health Bar Canvas Prefab.");
            return;
        }

        runtimeCanvas = Instantiate(bossHealthBarCanvasPrefab);
        runtimeCanvas.worldCamera = uiCamera;
        runtimeView = runtimeCanvas.GetComponentInChildren<BossHealthBarUI>(true);

        if (runtimeView == null)
            Debug.LogWarning($"[BossHealthBarPresenter] {bossHealthBarCanvasPrefab.name} chua co component BossHealthBarUI.");
    }

    private void SubscribeBoss()
    {
        if (boss == null)
            return;

        currentHealth = boss.CurrentHealth;
        maxHealth = boss.MaxHealth;
        boss.OnHealthChanged += OnBossHealthChanged;
        boss.OnDeath += OnBossDeath;
    }

    private void UnsubscribeBoss()
    {
        if (boss == null)
            return;

        boss.OnHealthChanged -= OnBossHealthChanged;
        boss.OnDeath -= OnBossDeath;
    }

    private void OnBossHealthChanged(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
        RefreshView();
    }

    private void OnBossDeath()
    {
        currentHealth = 0f;

        if (runtimeView != null)
        {
            runtimeView.SetHealth(currentHealth, maxHealth);
            runtimeView.SetVisible(!hideOnDeath);
        }
    }

    private void RefreshView()
    {
        if (runtimeView == null)
            return;

        runtimeView.SetBossName(bossName);
        runtimeView.SetHealth(currentHealth, maxHealth);
        runtimeView.SetVisible(showOnAwake && boss != null && boss.IsAlive);
    }
}
