using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DreamKnight.UI
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Selectable))]
	public class ButtonFocusChildrenToggle : MonoBehaviour, ISelectHandler, IDeselectHandler
	{
		[SerializeField] private Selectable targetSelectable;
		[SerializeField] private GameObject[] focusChildren;
		[SerializeField] private bool hideOnEnable = true;

		private void Awake()
		{
			if (targetSelectable == null)
				targetSelectable = GetComponent<Selectable>();
		}

		private void OnEnable()
		{
			if (hideOnEnable)
				SetFocusedVisual(false);

			RefreshByCurrentSelection();
		}

		public void OnSelect(BaseEventData eventData)
		{
			SetFocusedVisual(true);
		}

		public void OnDeselect(BaseEventData eventData)
		{
			SetFocusedVisual(false);
		}

		public void RefreshByCurrentSelection()
		{
			if (targetSelectable == null)
			{
				SetFocusedVisual(false);
				return;
			}

			bool isFocused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == targetSelectable.gameObject;
			SetFocusedVisual(isFocused);
		}

		private void SetFocusedVisual(bool focused)
		{
			if (focusChildren == null)
				return;

			for (int i = 0; i < focusChildren.Length; i++)
			{
				if (focusChildren[i] != null)
					focusChildren[i].SetActive(focused);
			}
		}
	}
}