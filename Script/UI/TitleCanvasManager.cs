using System.Collections;
using TMPro;
using DreamKnight.Systems.Scene;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DreamKnight.UI
{
	[DisallowMultipleComponent]
	public class TitleCanvasManager : MonoBehaviour
	{
		private enum MenuType
		{
			Top = 0,
			Save = 1,
			Setting = 2
		}

		private enum TitlePhase
		{
			IntroWait = 0,
			IntroFade = 1,
			Menu = 2
		}

		[Header("Common")]
		[SerializeField] private TextMeshProUGUI versionText;
		[SerializeField] private CanvasGroup nowLoadingGroup;
		[SerializeField] private CanvasGroup introNoticeGroup;
		[SerializeField] private CanvasGroup menuTopGroup;
		[SerializeField] private CanvasGroup menuSaveGroup;
		[SerializeField] private CanvasGroup menuSettingGroup;
		[SerializeField] private GameObject firstSelectedTop;
		[SerializeField] private bool setVersionFromApplication = true;

		[Header("Top Buttons")]
		[SerializeField] private Button startButton;
		[SerializeField] private Button settingButton;
		[SerializeField] private Button exitButton;

		[Header("Setting")]
		[SerializeField] private TitleSettingMenuController settingMenuController;

		[Header("Save")]
		[SerializeField] private Button[] saveSlotButtons;
		[SerializeField] private Button saveBackButton;

		[Header("Intro")]
		[SerializeField] private bool useIntro = true;
		[SerializeField] private float introWaitSeconds = 1.2f;
		[SerializeField] private float introFadeSeconds = 0.3f;

		[Header("Start Action")]
		[SerializeField] private bool autoLoadSceneOnStart;
		[SerializeField] private string gameplaySceneName;
		[SerializeField] private int gameplaySceneIndex = -1;
		[SerializeField] private float minLoadingScreenSeconds = 0.5f;
		[SerializeField] private UnityEvent onStartPressed;
		[SerializeField] private UnityEvent onSettingPressed;
		[SerializeField] private UnityEvent onExitPressed;
		[SerializeField] private UnityEvent<int> onSaveSlotSelected;

		private MenuType currentMenu;
		private TitlePhase titlePhase;
		private float phaseTimer;
		private UnityAction[] saveSlotHandlers;
		private bool isLoadingScene;

		private void Awake()
		{
			GlobalLoadingOverlay.BootstrapFromTemplate(nowLoadingGroup);

			if (settingMenuController == null)
				settingMenuController = GetComponentInChildren<TitleSettingMenuController>(true);

			settingMenuController?.Initialize(this);

			if (setVersionFromApplication && versionText != null)
				versionText.text = $"v{Application.version}";

			if (useIntro && introNoticeGroup == null)
				useIntro = false;

			if (useIntro)
			{
				titlePhase = TitlePhase.IntroWait;
				phaseTimer = introWaitSeconds;
				SetCanvasGroupVisible(introNoticeGroup, true);
				SetCanvasGroupVisible(menuTopGroup, false);
				SetCanvasGroupVisible(menuSaveGroup, false);
				SetCanvasGroupVisible(menuSettingGroup, false);
			}
			else
			{
				EnterMenuPhase();
			}

			SetCanvasGroupVisible(nowLoadingGroup, false);
			GlobalLoadingOverlay.Instance?.HideImmediate();
		}

		private void Start()
		{
			// Fallback to avoid locked UI when intro/menu references are misconfigured.
			if (titlePhase != TitlePhase.Menu && !useIntro)
				EnterMenuPhase();

			if (titlePhase == TitlePhase.Menu && currentMenu == MenuType.Top && firstSelectedTop != null)
				EventSystem.current?.SetSelectedGameObject(firstSelectedTop);
		}

		private void OnEnable()
		{
			BindButtonEvents();
		}

		private void OnDisable()
		{
			UnbindButtonEvents();
		}

		private void Update()
		{
			if (!useIntro)
				return;

			switch (titlePhase)
			{
				case TitlePhase.IntroWait:
					phaseTimer -= Time.unscaledDeltaTime;
					if (phaseTimer <= 0f)
					{
						titlePhase = TitlePhase.IntroFade;
						phaseTimer = Mathf.Max(0.01f, introFadeSeconds);
					}
					break;

				case TitlePhase.IntroFade:
					phaseTimer -= Time.unscaledDeltaTime;
					if (introNoticeGroup != null)
					{
						float alpha = Mathf.Clamp01(phaseTimer / Mathf.Max(0.01f, introFadeSeconds));
						introNoticeGroup.alpha = alpha;
					}

					if (phaseTimer <= 0f)
						EnterMenuPhase();
					break;
			}
		}

		public void SetMenuTop(bool ignoreLoading = false)
		{
			SetMenu(MenuType.Top, ignoreLoading);
		}

		public void SetMenuSave(bool ignoreLoading = false)
		{
			SetMenu(MenuType.Save, ignoreLoading);
		}

		public void SetMenuSetting(bool ignoreLoading = false)
		{
			SetMenu(MenuType.Setting, ignoreLoading);
		}

		public void RefreshSaveSlot(int selectedIndex)
		{
			if (saveSlotButtons == null || saveSlotButtons.Length == 0)
				return;

			selectedIndex = Mathf.Clamp(selectedIndex, 0, saveSlotButtons.Length - 1);
			Button slot = saveSlotButtons[selectedIndex];
			if (slot != null)
				EventSystem.current?.SetSelectedGameObject(slot.gameObject);
		}

		private void EnterMenuPhase()
		{
			titlePhase = TitlePhase.Menu;
			SetCanvasGroupVisible(introNoticeGroup, false);
			SetMenu(MenuType.Top, true);
			if (firstSelectedTop != null)
				EventSystem.current?.SetSelectedGameObject(firstSelectedTop);
		}

		[ContextMenu("Force Show Top Menu")]
		private void ForceShowTopMenu()
		{
			useIntro = false;
			EnterMenuPhase();
		}

		private void SetMenu(MenuType menuType, bool ignoreLoading)
		{
			currentMenu = menuType;
			SetCanvasGroupVisible(menuTopGroup, menuType == MenuType.Top);
			SetCanvasGroupVisible(menuSaveGroup, menuType == MenuType.Save);
			SetCanvasGroupVisible(menuSettingGroup, menuType == MenuType.Setting);

			if (menuType == MenuType.Setting)
				settingMenuController?.ShowMain(resetHistory: true);
			else
				settingMenuController?.HideAll();

			if (!ignoreLoading)
				SetCanvasGroupVisible(nowLoadingGroup, false);
		}

		private void BindButtonEvents()
		{
			if (startButton != null)
				startButton.onClick.AddListener(HandleStartButton);

			if (settingButton != null)
				settingButton.onClick.AddListener(HandleSettingButton);

			if (exitButton != null)
				exitButton.onClick.AddListener(HandleExitButton);

			if (saveBackButton != null)
				saveBackButton.onClick.AddListener(HandleSaveBackButton);

			if (saveSlotButtons == null)
				return;

			if (saveSlotHandlers == null || saveSlotHandlers.Length != saveSlotButtons.Length)
				saveSlotHandlers = new UnityAction[saveSlotButtons.Length];

			for (int i = 0; i < saveSlotButtons.Length; i++)
			{
				if (saveSlotButtons[i] == null)
					continue;

				int slot = i;
				saveSlotHandlers[i] = () => HandleSaveSlotButton(slot);
				saveSlotButtons[i].onClick.AddListener(saveSlotHandlers[i]);
			}
		}

		private void UnbindButtonEvents()
		{
			if (startButton != null)
				startButton.onClick.RemoveListener(HandleStartButton);

			if (settingButton != null)
				settingButton.onClick.RemoveListener(HandleSettingButton);

			if (exitButton != null)
				exitButton.onClick.RemoveListener(HandleExitButton);

			if (saveBackButton != null)
				saveBackButton.onClick.RemoveListener(HandleSaveBackButton);

			if (saveSlotButtons == null)
				return;

			for (int i = 0; i < saveSlotButtons.Length; i++)
			{
				Button slotButton = saveSlotButtons[i];
				if (slotButton == null)
					continue;

				if (saveSlotHandlers != null && i < saveSlotHandlers.Length && saveSlotHandlers[i] != null)
					slotButton.onClick.RemoveListener(saveSlotHandlers[i]);
			}
		}

		private void HandleStartButton()
		{
			onStartPressed?.Invoke();
			SetMenu(MenuType.Save, true);
			RefreshSaveSlot(0);
		}

		private void HandleSettingButton()
		{
			onSettingPressed?.Invoke();
			SetMenu(MenuType.Setting, true);
		}

		private void HandleExitButton()
		{
			onExitPressed?.Invoke();
			Application.Quit();
		}

		private void HandleSaveSlotButton(int slot)
		{
			if (isLoadingScene)
				return;

			onSaveSlotSelected?.Invoke(slot);

			if (!autoLoadSceneOnStart)
				return;

			TryLoadGameplayScene();
		}

		private void HandleSaveBackButton()
		{
			SetMenu(MenuType.Top, true);
			if (firstSelectedTop != null)
				EventSystem.current?.SetSelectedGameObject(firstSelectedTop);
		}

		private void TryLoadGameplayScene()
		{
			if (isLoadingScene)
				return;

			StartCoroutine(LoadGameplaySceneRoutine());
		}

		public void LoadGameplaySceneFromSaveMenu()
		{
			TryLoadGameplayScene();
		}

		private IEnumerator LoadGameplaySceneRoutine()
		{
			isLoadingScene = true;
			SetCanvasGroupVisible(nowLoadingGroup, true);
			GlobalLoadingOverlay.Instance?.Show();
			SetCanvasGroupVisible(menuTopGroup, false);
			SetCanvasGroupVisible(menuSaveGroup, false);
			SetCanvasGroupVisible(menuSettingGroup, false);
			EventSystem.current?.SetSelectedGameObject(null);

			// Give UI one frame to render the loading panel before scene swap.
			yield return null;

			AsyncOperation loadOperation = null;
			if (!string.IsNullOrWhiteSpace(gameplaySceneName))
				loadOperation = SceneManager.LoadSceneAsync(gameplaySceneName);
			else if (gameplaySceneIndex >= 0)
				loadOperation = SceneManager.LoadSceneAsync(gameplaySceneIndex);

			if (loadOperation == null)
			{
				Debug.LogWarning("TitleCanvasManager: Gameplay scene is not configured.");
				SetCanvasGroupVisible(nowLoadingGroup, false);
				GlobalLoadingOverlay.Instance?.Hide();
				SetMenu(MenuType.Save, true);
				isLoadingScene = false;
				yield break;
			}

			loadOperation.allowSceneActivation = false;
			float elapsed = 0f;
			float minWait = Mathf.Max(0f, minLoadingScreenSeconds);

			while (loadOperation.progress < 0.9f || elapsed < minWait)
			{
				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}

			loadOperation.allowSceneActivation = true;

			while (!loadOperation.isDone)
				yield return null;

			GlobalLoadingOverlay.Instance?.Hide();
			isLoadingScene = false;
		}

		private static void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
		{
			if (canvasGroup == null)
				return;

			canvasGroup.alpha = visible ? 1f : 0f;
			canvasGroup.interactable = visible;
			canvasGroup.blocksRaycasts = visible;
		}

		public void ReturnFromSettingToTop()
		{
			SetMenu(MenuType.Top, true);
			if (firstSelectedTop != null)
				EventSystem.current?.SetSelectedGameObject(firstSelectedTop);
		}
	}
}
