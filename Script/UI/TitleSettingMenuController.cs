using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DreamKnight.UI
{
	[DisallowMultipleComponent]
	public class TitleSettingMenuController : MonoBehaviour
	{
		private enum SettingView
		{
			Main = 0,
			Game = 1,
			Audio = 2,
			Graphic = 3,
			KeyboardActions = 4,
			KeyboardUI = 5
		}

		[Header("Setting Buttons")]
		[SerializeField] private Button settingGameButton;
		[SerializeField] private Button settingAudioButton;
		[SerializeField] private Button settingGraphicButton;
		[SerializeField] private Button settingKeyboardActionsButton;
		[SerializeField] private Button settingKeyboardUIButton;
		[SerializeField] private Button settingMainBackButton;
		[SerializeField] private Button settingDetailBackButton;
		[SerializeField] private Button settingBackButton;

		[Header("Setting Panels")]
		[SerializeField] private GameObject settingMainRoot;
		[SerializeField] private GameObject settingSelectRoot;
		[SerializeField] private GameObject settingPanelGame;
		[SerializeField] private GameObject settingPanelAudio;
		[SerializeField] private GameObject settingPanelGraphic;
		[SerializeField] private GameObject settingPanelKeyboardActions;
		[SerializeField] private GameObject settingPanelKeyboardUI;
		[SerializeField] private GameObject firstSelectedSettingMain;
		[SerializeField] private GameObject firstSelectedSettingDetail;

		private readonly Stack<SettingView> settingHistory = new Stack<SettingView>();
		private SettingView currentSettingView = SettingView.Main;
		private TitleCanvasManager owner;

		public void Initialize(TitleCanvasManager ownerManager)
		{
			owner = ownerManager;
		}

		private void OnEnable()
		{
			BindButtonEvents();
		}

		private void OnDisable()
		{
			UnbindButtonEvents();
		}

		public void ShowMain(bool resetHistory)
{
    Debug.Log("[TitleSettingMenuController] ShowMain called! resetHistory=" + resetHistory);
    Debug.Log("[TitleSettingMenuController] settingMainRoot = " + (settingMainRoot != null ? settingMainRoot.name : "NULL"));
    
    if (resetHistory)
        settingHistory.Clear();
    
    // Ensure all parents of settingMainRoot are active, otherwise activeInHierarchy will be false
    if (settingMainRoot != null)
    {
        Transform current = settingMainRoot.transform.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
    }

    ShowSettingView(SettingView.Main, true);
}

		public void HideAll()
		{
			if (settingMainRoot != null)
				settingMainRoot.SetActive(false);

			if (settingSelectRoot != null)
				settingSelectRoot.SetActive(false);

			SetActiveIfNotNull(settingPanelGame, false);
			SetActiveIfNotNull(settingPanelAudio, false);
			SetActiveIfNotNull(settingPanelGraphic, false);
			SetActiveIfNotNull(settingPanelKeyboardActions, false);
			SetActiveIfNotNull(settingPanelKeyboardUI, false);
		}

		private void BindButtonEvents()
		{
			if (settingGameButton != null)
				settingGameButton.onClick.AddListener(HandleSettingGameButton);

			if (settingAudioButton != null)
				settingAudioButton.onClick.AddListener(HandleSettingAudioButton);

			if (settingGraphicButton != null)
				settingGraphicButton.onClick.AddListener(HandleSettingGraphicButton);

			if (settingKeyboardActionsButton != null)
				settingKeyboardActionsButton.onClick.AddListener(HandleSettingKeyboardActionsButton);

			if (settingKeyboardUIButton != null)
				settingKeyboardUIButton.onClick.AddListener(HandleSettingKeyboardUIButton);

			if (settingMainBackButton != null)
				settingMainBackButton.onClick.AddListener(HandleSettingMainBackButton);

			if (settingDetailBackButton != null)
				settingDetailBackButton.onClick.AddListener(HandleSettingDetailBackButton);

			if (settingBackButton != null && settingBackButton != settingMainBackButton && settingBackButton != settingDetailBackButton)
				settingBackButton.onClick.AddListener(HandleSettingBackButtonFallback);
		}

		private void UnbindButtonEvents()
		{
			if (settingGameButton != null)
				settingGameButton.onClick.RemoveListener(HandleSettingGameButton);

			if (settingAudioButton != null)
				settingAudioButton.onClick.RemoveListener(HandleSettingAudioButton);

			if (settingGraphicButton != null)
				settingGraphicButton.onClick.RemoveListener(HandleSettingGraphicButton);

			if (settingKeyboardActionsButton != null)
				settingKeyboardActionsButton.onClick.RemoveListener(HandleSettingKeyboardActionsButton);

			if (settingKeyboardUIButton != null)
				settingKeyboardUIButton.onClick.RemoveListener(HandleSettingKeyboardUIButton);

			if (settingMainBackButton != null)
				settingMainBackButton.onClick.RemoveListener(HandleSettingMainBackButton);

			if (settingDetailBackButton != null)
				settingDetailBackButton.onClick.RemoveListener(HandleSettingDetailBackButton);

			if (settingBackButton != null && settingBackButton != settingMainBackButton && settingBackButton != settingDetailBackButton)
				settingBackButton.onClick.RemoveListener(HandleSettingBackButtonFallback);
		}

		private void HandleSettingGameButton()
		{
			OpenSettingDetail(SettingView.Game);
		}

		private void HandleSettingAudioButton()
		{
			OpenSettingDetail(SettingView.Audio);
		}

		private void HandleSettingGraphicButton()
		{
			OpenSettingDetail(SettingView.Graphic);
		}

		private void HandleSettingKeyboardActionsButton()
		{
			OpenSettingDetail(SettingView.KeyboardActions);
		}

		private void HandleSettingKeyboardUIButton()
		{
			OpenSettingDetail(SettingView.KeyboardUI);
		}

		private void HandleSettingMainBackButton()
		{
			// If we have a TitleCanvasManager owner, use it to return to Top menu.
			// Otherwise just hide the setting panels locally so the UI closes correctly.
			if (owner != null)
				owner.ReturnFromSettingToTop();
			else
				// Deactivate the whole GameObject this component is attached to
				gameObject.SetActive(false);
		}

		private void HandleSettingDetailBackButton()
		{
			ShowMain(true);
		}

		private void HandleSettingBackButtonFallback()
		{
			if (settingHistory.Count > 0)
			{
				ShowSettingView(settingHistory.Pop(), true);
				return;
			}

			if (currentSettingView != SettingView.Main)
			{
				ShowMain(true);
				return;
			}

			owner?.ReturnFromSettingToTop();
		}

		private void OpenSettingDetail(SettingView detailView)
		{
			if (currentSettingView != detailView)
				settingHistory.Push(currentSettingView);

			ShowSettingView(detailView, true);
		}

		private void ShowSettingView(SettingView view, bool setSelection)
		{
			currentSettingView = view;

			if (settingMainRoot != null)
				settingMainRoot.SetActive(view == SettingView.Main);

			if (settingSelectRoot != null)
				settingSelectRoot.SetActive(view != SettingView.Main);

			SetActiveIfNotNull(settingPanelGame, view == SettingView.Game);
			SetActiveIfNotNull(settingPanelAudio, view == SettingView.Audio);
			SetActiveIfNotNull(settingPanelGraphic, view == SettingView.Graphic);
			SetActiveIfNotNull(settingPanelKeyboardActions, view == SettingView.KeyboardActions);
			SetActiveIfNotNull(settingPanelKeyboardUI, view == SettingView.KeyboardUI);

			if (!setSelection)
				return;

			if (view == SettingView.Main && firstSelectedSettingMain != null)
				EventSystem.current?.SetSelectedGameObject(firstSelectedSettingMain);
			else if (view != SettingView.Main && firstSelectedSettingDetail != null)
				EventSystem.current?.SetSelectedGameObject(firstSelectedSettingDetail);
		}

		private static void SetActiveIfNotNull(GameObject target, bool active)
		{
			if (target != null)
				target.SetActive(active);
		}
	}
}
