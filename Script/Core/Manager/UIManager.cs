using DreamKnight.Player;
using DreamKnight.Systems.Currency;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamKnight.UI
{
	[DisallowMultipleComponent]
	public class UIManager : MonoBehaviour
	{
		public struct PlayerSpecificationsSnapshot
		{
			public float currentHealth;
			public float maxHealth;
			public float currentStamina;
			public float maxStamina;
			public float currentMana;
			public float maxMana;
		}

		private static UIManager instance;
		private static bool hasLastSnapshot;
		private static PlayerSpecificationsSnapshot lastSnapshot;

		public static UIManager Instance => instance;
		public static bool TryGetLastSnapshot(out PlayerSpecificationsSnapshot snapshot)
		{
			snapshot = lastSnapshot;
			return hasLastSnapshot;
		}

		public static event System.Action<PlayerSpecificationsSnapshot> PlayerSpecificationsUpdated;

		[SerializeField] private PlayerController playerController;
		[SerializeField] private PlayerHudUI playerHud;
		[SerializeField] private bool autoFindPlayer = true;
		[SerializeField] private bool dontDestroyOnLoad = true;

		[Header("Currency UI")]
		[SerializeField] private CurrencyWalletSO currencyWallet;
		[SerializeField] private TextMeshProUGUI moneyValueText;
		[SerializeField] private TextMeshProUGUI moneyGainText;
		[SerializeField] private CanvasGroup moneyGainCanvasGroup;
		[SerializeField] private float moneyGainDisplayDuration = 0.35f;

		[Header("Interact Prompt (World Space)")]
		[SerializeField] private Transform interactPromptRoot;
		[SerializeField] private CanvasGroup interactPromptCanvasGroup;
		[SerializeField] private TextMeshProUGUI interactPromptText;
		[SerializeField] private string interactPromptFormat = "{icon}";
		[SerializeField] private Vector3 interactPromptWorldOffset = new Vector3(0f, 1f, 0f);

		private PlayerStats boundStats;
		private Transform interactPromptTarget;
		private Object interactPromptOwner;
		private bool interactPromptVisible;
		private bool hasPromptAction;
		private PlayerInput.BindableAction currentPromptAction;
		private string currentPromptFallbackKeyName;
		private readonly Queue<int> pendingMoneyQueue = new Queue<int>();
		private Coroutine moneyQueueCoroutine;

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				Destroy(gameObject);
				return;
			}

			instance = this;

			if (dontDestroyOnLoad)
			{
				if (gameObject.scene.name != "DontDestroyOnLoad")
					DontDestroyOnLoad(gameObject);
			}

			if (playerHud == null)
				playerHud = GetComponentInChildren<PlayerHudUI>(true);
		}

		private void OnEnable()
		{
			SceneManager.sceneLoaded += HandleSceneLoaded;
			BindInputEvents();
			BindCurrencyEvents();
			TryBindPlayerStats();
		}

		private void Start()
		{
			TryBindPlayerStats();
		}

		private void OnDisable()
		{
			SceneManager.sceneLoaded -= HandleSceneLoaded;
			UnbindInputEvents();
			UnbindCurrencyEvents();
			UnbindStatsEvents();
		}

		private void OnDestroy()
		{
			if (instance == this)
				instance = null;
		}

		public static void ResetSessionState()
		{
			hasLastSnapshot = false;
			lastSnapshot = default;
			PlayerSpecificationsUpdated = null;
		}

		private void Update()
		{
			if (!autoFindPlayer)
			{
				UpdateInteractPromptTransform();
				return;
			}

			if (playerController == null || boundStats == null || playerController.Stats != boundStats)
				TryBindPlayerStats();

			UpdateInteractPromptTransform();
		}

		public void SetPlayerController(PlayerController controller)
		{
			UnbindInputEvents();
			playerController = controller;
			BindInputEvents();
			TryBindPlayerStats();
			RefreshInteractPromptText();
		}

		public void ShowInteractPrompt(Object owner, Transform target, PlayerInput.BindableAction action, string customFormat = null)
		{
			if (owner == null || target == null)
				return;

			interactPromptOwner = owner;
			interactPromptTarget = target;
			hasPromptAction = true;
			currentPromptAction = action;
			currentPromptFallbackKeyName = string.Empty;

			if (!string.IsNullOrWhiteSpace(customFormat))
				interactPromptFormat = customFormat;

			interactPromptVisible = true;
			SetInteractPromptVisible(true);
			RefreshInteractPromptText();
			UpdateInteractPromptTransform();
		}

		public void ShowInteractPromptByKeyName(Object owner, Transform target, string keyName, string customFormat = null)
		{
			if (owner == null || target == null)
				return;

			interactPromptOwner = owner;
			interactPromptTarget = target;
			hasPromptAction = false;
			currentPromptFallbackKeyName = keyName;

			if (!string.IsNullOrWhiteSpace(customFormat))
				interactPromptFormat = customFormat;

			interactPromptVisible = true;
			SetInteractPromptVisible(true);
			RefreshInteractPromptText();
			UpdateInteractPromptTransform();
		}

		public void HideInteractPrompt(Object owner)
		{
			if (owner == null)
				return;

			if (interactPromptOwner != owner)
				return;

			ClearInteractPrompt();
		}

		public void EnqueueMoneyPickup(int amount)
		{
			if (amount <= 0)
				return;

			pendingMoneyQueue.Enqueue(amount);
			if (moneyQueueCoroutine == null)
				moneyQueueCoroutine = StartCoroutine(ProcessMoneyQueueRoutine());
		}

		private void TryBindPlayerStats()
		{
			if (playerController == null && autoFindPlayer)
				playerController = FindAnyObjectByType<PlayerController>();

			PlayerStats nextStats = playerController != null ? playerController.Stats : null;
			if (nextStats == null)
			{
				UnbindStatsEvents();
				return;
			}

			if (boundStats == nextStats)
			{
				RefreshAll();
				return;
			}

			UnbindStatsEvents();
			boundStats = nextStats;
			boundStats.OnHealthChanged += HandleHealthChanged;
			boundStats.OnStaminaChanged += HandleStaminaChanged;
			boundStats.OnManaChanged += HandleManaChanged;
			RefreshAll();
		}

		private void UnbindStatsEvents()
		{
			if (boundStats == null)
				return;

			boundStats.OnHealthChanged -= HandleHealthChanged;
			boundStats.OnStaminaChanged -= HandleStaminaChanged;
			boundStats.OnManaChanged -= HandleManaChanged;
			boundStats = null;
		}

		private void RefreshAll()
		{
			if (boundStats == null)
				return;

			UpdateHudVisibilityAndFormHud();
			PublishSnapshot(boundStats.CurrentHealth, boundStats.MaxHealth, boundStats.CurrentStamina, boundStats.MaxStamina, boundStats.CurrentMana, boundStats.MaxMana);
		}

		private void HandleHealthChanged(float current, float max)
		{
			if (boundStats == null)
				return;

			UpdateHudVisibilityAndFormHud(current, max);
			PublishSnapshot(current, max, boundStats.CurrentStamina, boundStats.MaxStamina, boundStats.CurrentMana, boundStats.MaxMana);
		}

		private void HandleStaminaChanged(float current, float max)
		{
			if (boundStats == null)
				return;

			PublishSnapshot(boundStats.CurrentHealth, boundStats.MaxHealth, current, max, boundStats.CurrentMana, boundStats.MaxMana);
		}

		private void HandleManaChanged(float current, float max)
		{
			if (boundStats == null)
				return;

			PublishSnapshot(boundStats.CurrentHealth, boundStats.MaxHealth, boundStats.CurrentStamina, boundStats.MaxStamina, current, max);
		}

		private void PublishSnapshot(float currentHealth, float maxHealth, float currentStamina, float maxStamina, float currentMana, float maxMana)
		{
			PlayerSpecificationsSnapshot snapshot = new PlayerSpecificationsSnapshot
			{
				currentHealth = currentHealth,
				maxHealth = maxHealth,
				currentStamina = currentStamina,
				maxStamina = maxStamina,
				currentMana = currentMana,
				maxMana = maxMana
			};

			lastSnapshot = snapshot;
			hasLastSnapshot = true;

			playerHud?.SetHealth(snapshot.currentHealth, snapshot.maxHealth);
			playerHud?.SetStamina(snapshot.currentStamina, snapshot.maxStamina);
			playerHud?.SetMana(snapshot.currentMana, snapshot.maxMana);
			playerHud?.SetTransformHealth(snapshot.currentHealth, snapshot.maxHealth);

			PlayerSpecificationsUpdated?.Invoke(snapshot);
		}

		private void UpdateHudVisibilityAndFormHud(float currentHealth = -1f, float maxHealth = -1f)
		{
		}

		private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			TryBindPlayerStats();
			BindInputEvents();
			BindCurrencyEvents();

			if (interactPromptTarget == null)
				ClearInteractPrompt();
			else
				RefreshInteractPromptText();
		}

		private void BindCurrencyEvents()
		{
			if (currencyWallet == null)
				return;

			currencyWallet.OnBalanceChanged -= HandleCurrencyBalanceChanged;
			currencyWallet.OnBalanceChanged += HandleCurrencyBalanceChanged;
			HandleCurrencyBalanceChanged(currencyWallet.Balance);
			SetMoneyGainVisible(false);
		}

		private void UnbindCurrencyEvents()
		{
			if (currencyWallet == null)
				return;

			currencyWallet.OnBalanceChanged -= HandleCurrencyBalanceChanged;
		}

		private void HandleCurrencyBalanceChanged(int balance)
		{
			if (moneyValueText != null)
				moneyValueText.text = balance.ToString();
		}

		private IEnumerator ProcessMoneyQueueRoutine()
		{
			while (pendingMoneyQueue.Count > 0)
			{
				int amount = pendingMoneyQueue.Dequeue();
				ShowMoneyGain(amount);
				yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, moneyGainDisplayDuration));

				if (currencyWallet != null)
					currencyWallet.Add(amount);

				SetMoneyGainVisible(false);
			}

			moneyQueueCoroutine = null;
		}

		private void ShowMoneyGain(int amount)
		{
			if (moneyGainText != null)
				moneyGainText.text = $"+{amount}";

			SetMoneyGainVisible(true);
		}

		private void SetMoneyGainVisible(bool visible)
		{
			if (moneyGainCanvasGroup != null)
			{
				moneyGainCanvasGroup.alpha = visible ? 1f : 0f;
				moneyGainCanvasGroup.interactable = false;
				moneyGainCanvasGroup.blocksRaycasts = false;
			}

			if (moneyGainText != null && moneyGainCanvasGroup == null)
				moneyGainText.gameObject.SetActive(visible);
		}

		private void BindInputEvents()
		{
			PlayerInput input = playerController != null ? playerController.Input : null;
			if (input == null)
				return;

			input.OnBindingChanged -= HandleBindingChanged;
			input.OnBindingChanged += HandleBindingChanged;
		}

		private void UnbindInputEvents()
		{
			PlayerInput input = playerController != null ? playerController.Input : null;
			if (input == null)
				return;

			input.OnBindingChanged -= HandleBindingChanged;
		}

		private void HandleBindingChanged(PlayerInput.BindableAction action, string keyName, string iconTag)
		{
			if (!interactPromptVisible || !hasPromptAction)
				return;

			if (action != currentPromptAction)
				return;

			RefreshInteractPromptText();
		}

		private void RefreshInteractPromptText()
		{
			if (interactPromptText == null)
				return;

			string keyName = string.Empty;
			string icon = string.Empty;

			if (hasPromptAction && playerController != null && playerController.Input != null)
			{
				keyName = playerController.Input.GetBindingKeyName(currentPromptAction);
				icon = playerController.Input.GetBindingIconTag(currentPromptAction);
			}
			else if (!string.IsNullOrWhiteSpace(currentPromptFallbackKeyName))
			{
				keyName = currentPromptFallbackKeyName;
				icon = KeyboardIconMapper.GetSpriteTag(keyName);
			}

			if (string.IsNullOrWhiteSpace(keyName) && string.IsNullOrWhiteSpace(icon))
			{
				interactPromptText.text = string.Empty;
				return;
			}

			if (string.IsNullOrWhiteSpace(icon))
				icon = keyName;

			string format = string.IsNullOrWhiteSpace(interactPromptFormat) ? "{icon}" : interactPromptFormat;
			string output = format.Replace("{icon}", icon).Replace("{key}", keyName);
			interactPromptText.text = output;
		}

		private void UpdateInteractPromptTransform()
		{
			if (!interactPromptVisible)
				return;

			if (interactPromptTarget == null || interactPromptRoot == null)
			{
				ClearInteractPrompt();
				return;
			}

			interactPromptRoot.position = interactPromptTarget.position + interactPromptWorldOffset;
		}

		private void SetInteractPromptVisible(bool visible)
		{
			if (interactPromptRoot != null)
				interactPromptRoot.gameObject.SetActive(visible);

			if (interactPromptCanvasGroup != null)
			{
				interactPromptCanvasGroup.alpha = visible ? 1f : 0f;
				interactPromptCanvasGroup.interactable = false;
				interactPromptCanvasGroup.blocksRaycasts = false;
			}
		}

		private void ClearInteractPrompt()
		{
			interactPromptVisible = false;
			interactPromptTarget = null;
			interactPromptOwner = null;
			hasPromptAction = false;
			currentPromptFallbackKeyName = string.Empty;
			SetInteractPromptVisible(false);
		}
	}
}
