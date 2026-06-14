using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DreamKnight.UI
{
	[DisallowMultipleComponent]
	public class MenuFocusVisualController : MonoBehaviour
	{
		[Header("Scan Roots")]
		[SerializeField] private Transform[] menuRoots;
		[SerializeField] private bool includeInactiveButtons = true;

		[Header("Focus Child Name Filters")]
		[SerializeField] private string[] focusNameKeywords = { "focus", "selected", "arrow" };

		[Header("Mouse Hover")]
		[SerializeField] private bool enableMouseHoverFocus = true;
		[SerializeField] private bool fallbackToSelectedWhenNoHover = false;

		private readonly List<FocusEntry> entries = new List<FocusEntry>();
		private readonly HashSet<Selectable> trackedSelectables = new HashSet<Selectable>();
		private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
		private GameObject lastFocusedObject;

		private void Awake()
		{
			RebuildCache();
		}

		private void OnEnable()
		{
			RebuildCache();
			RefreshAll();
		}

		private void Update()
		{
			GameObject nextFocusedObject = ResolveFocusedObject();
			if (nextFocusedObject == lastFocusedObject)
				return;

			lastFocusedObject = nextFocusedObject;
			RefreshAll();
		}

		[ContextMenu("Rebuild Focus Cache")]
		public void RebuildCache()
		{
			entries.Clear();
			trackedSelectables.Clear();
			HashSet<Selectable> seen = new HashSet<Selectable>();

			if (menuRoots == null || menuRoots.Length == 0)
				return;

			for (int i = 0; i < menuRoots.Length; i++)
			{
				Transform root = menuRoots[i];
				if (root == null)
					continue;

				Selectable[] selectables = root.GetComponentsInChildren<Selectable>(includeInactiveButtons);
				for (int s = 0; s < selectables.Length; s++)
				{
					Selectable selectable = selectables[s];
					if (selectable == null || !seen.Add(selectable))
						continue;

					List<GameObject> focusChildren = FindFocusChildren(selectable.transform);
					if (focusChildren.Count == 0)
						continue;

					entries.Add(new FocusEntry(selectable, focusChildren));
					trackedSelectables.Add(selectable);
				}
			}

			lastFocusedObject = null;
		}

		public void RefreshAll()
		{
			GameObject focusedObject = ResolveFocusedObject();
			for (int i = 0; i < entries.Count; i++)
			{
				FocusEntry entry = entries[i];
				if (entry.Selectable == null)
					continue;

				bool focused = focusedObject == entry.Selectable.gameObject;
				entry.SetFocused(focused);
			}
		}

		private GameObject ResolveFocusedObject()
		{
			GameObject hoverObject = enableMouseHoverFocus ? TryGetHoveredTrackedSelectableObject() : null;
			if (hoverObject != null)
				return hoverObject;

			if (!fallbackToSelectedWhenNoHover)
				return null;

			return EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
		}

		private GameObject TryGetHoveredTrackedSelectableObject()
		{
			if (EventSystem.current == null)
				return null;

			PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
			pointerEventData.position = Input.mousePosition;
			raycastResults.Clear();
			EventSystem.current.RaycastAll(pointerEventData, raycastResults);

			for (int i = 0; i < raycastResults.Count; i++)
			{
				GameObject hit = raycastResults[i].gameObject;
				if (hit == null)
					continue;

				Selectable selectable = hit.GetComponentInParent<Selectable>();
				if (selectable == null)
					continue;

				if (!trackedSelectables.Contains(selectable))
					continue;

				if (!selectable.IsInteractable() || !selectable.gameObject.activeInHierarchy)
					continue;

				return selectable.gameObject;
			}

			return null;
		}

		private List<GameObject> FindFocusChildren(Transform selectableRoot)
		{
			List<GameObject> result = new List<GameObject>();
			if (focusNameKeywords == null || focusNameKeywords.Length == 0)
				return result;

			Transform[] allChildren = selectableRoot.GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < allChildren.Length; i++)
			{
				Transform child = allChildren[i];
				if (child == selectableRoot)
					continue;

				if (NameMatches(child.name))
					result.Add(child.gameObject);
			}

			return result;
		}

		private bool NameMatches(string target)
		{
			if (string.IsNullOrWhiteSpace(target))
				return false;

			for (int i = 0; i < focusNameKeywords.Length; i++)
			{
				string keyword = focusNameKeywords[i];
				if (string.IsNullOrWhiteSpace(keyword))
					continue;

				if (target.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			return false;
		}

		private sealed class FocusEntry
		{
			public readonly Selectable Selectable;
			private readonly List<GameObject> visuals;

			public FocusEntry(Selectable selectable, List<GameObject> focusVisuals)
			{
				Selectable = selectable;
				visuals = focusVisuals;
			}

			public void SetFocused(bool focused)
			{
				for (int i = 0; i < visuals.Count; i++)
				{
					GameObject visual = visuals[i];
					if (visual != null)
						visual.SetActive(focused);
				}
			}
		}
	}
}