using TMPro;
using System;
using UnityEngine;

namespace DreamKnight.UI
{
	[DisallowMultipleComponent]
	public class DamageTextView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI textLabel;
		[SerializeField] private CanvasGroup canvasGroup;
		[SerializeField] private float lifetime = 0.8f;
		[SerializeField] private float riseSpeed = 48f;
		[SerializeField] private AnimationCurve alphaCurve = null;
		[SerializeField] private AnimationCurve scaleCurve = null;

		private RectTransform rectTransform;
		private Camera worldCamera;
		private Vector3 worldPosition;
		private Vector2 screenOffset;
		private Vector2 drift;
		private float scaleMultiplier = 1f;
		private float elapsed;
		private bool initialized;
		private Action<DamageTextView> releaseCallback;

		private void Awake()
		{
			rectTransform = transform as RectTransform;
			if (textLabel == null)
				textLabel = GetComponentInChildren<TextMeshProUGUI>();
			if (canvasGroup == null)
				canvasGroup = GetComponent<CanvasGroup>();
			if (alphaCurve == null || alphaCurve.length == 0)
				alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			if (scaleCurve == null || scaleCurve.length == 0)
				scaleCurve = new AnimationCurve(
					new Keyframe(0f, 0.85f),
					new Keyframe(0.2f, 1.1f),
					new Keyframe(1f, 1f));
		}

		public void Initialize(string valueText, Color color, Vector3 trackedWorldPosition, Camera camera, Vector2 baseScreenOffset, Vector2 randomDrift, float textScaleMultiplier = 1f, Action<DamageTextView> onRelease = null)
		{
			worldPosition = trackedWorldPosition;
			worldCamera = camera;
			screenOffset = baseScreenOffset;
			drift = randomDrift;
			scaleMultiplier = Mathf.Max(0.01f, textScaleMultiplier);
			releaseCallback = onRelease;
			elapsed = 0f;
			initialized = true;

			if (textLabel != null)
			{
				textLabel.text = valueText;
				textLabel.color = color;
			}

			UpdateVisual();
		}

		public void ResetForPool()
		{
			initialized = false;
			elapsed = 0f;
			releaseCallback = null;
			if (canvasGroup != null)
				canvasGroup.alpha = 1f;
		}

		private void Update()
		{
			if (!initialized)
				return;

			elapsed += Time.deltaTime;
			UpdateVisual();

			if (elapsed >= lifetime)
				Release();
		}

		private void Release()
		{
			initialized = false;
			if (releaseCallback != null)
			{
				Action<DamageTextView> callback = releaseCallback;
				releaseCallback = null;
				callback(this);
			}
			else
			{
				Destroy(gameObject);
			}
		}

		private void UpdateVisual()
		{
			float t = lifetime <= 0.0001f ? 1f : Mathf.Clamp01(elapsed / lifetime);
			if (canvasGroup != null)
				canvasGroup.alpha = alphaCurve.Evaluate(t);

			float scale = scaleCurve.Evaluate(t);
			if (rectTransform != null)
				rectTransform.localScale = Vector3.one * scale * scaleMultiplier;

			UpdateScreenPosition(t);
		}

		private void UpdateScreenPosition(float normalizedTime)
		{
			if (rectTransform == null)
				return;

			Camera camera = worldCamera != null ? worldCamera : Camera.main;
			Vector3 screenPoint = camera != null
				? camera.WorldToScreenPoint(worldPosition)
				: worldPosition;

			screenPoint += (Vector3)screenOffset;
			screenPoint += new Vector3(drift.x * normalizedTime, drift.y * normalizedTime + riseSpeed * normalizedTime, 0f);
			rectTransform.position = screenPoint;
		}

		public static DamageTextView CreateRuntime(Transform parent)
		{
			GameObject root = new GameObject("DamageText", typeof(RectTransform), typeof(CanvasGroup));
			root.transform.SetParent(parent, false);
			RectTransform rootRect = root.GetComponent<RectTransform>();
			rootRect.sizeDelta = new Vector2(180f, 48f);

			GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
			labelObject.transform.SetParent(root.transform, false);
			RectTransform labelRect = labelObject.GetComponent<RectTransform>();
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.offsetMin = Vector2.zero;
			labelRect.offsetMax = Vector2.zero;

			TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
			label.alignment = TextAlignmentOptions.Center;
			label.fontSize = 28f;
			label.enableAutoSizing = false;
			label.raycastTarget = false;
			label.outlineWidth = 0.2f;
			label.outlineColor = new Color(0f, 0f, 0f, 0.85f);

			DamageTextView view = root.AddComponent<DamageTextView>();
			view.textLabel = label;
			view.canvasGroup = root.GetComponent<CanvasGroup>();
			return view;
		}
	}
}
