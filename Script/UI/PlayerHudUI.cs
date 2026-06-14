using DreamKnight.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.UI
{
	[DisallowMultipleComponent]
	public class PlayerHudUI : MonoBehaviour
	{
		[SerializeField] private Image healthFill;
		[SerializeField] private TextMeshProUGUI healthText;
		[SerializeField] private Image staminaFill;
		[SerializeField] private TextMeshProUGUI staminaText;
		[SerializeField] private Image manaFill;
		[SerializeField] private TextMeshProUGUI manaText;
		[SerializeField] private GameObject transformHudRoot;
		[SerializeField] private Image transformHealthFill;
		[SerializeField] private TextMeshProUGUI transformHealthText;

		private PlayerController playerController;
		private bool lastTransformedState;

		private void Awake()
		{
			ResolveReferences();
		}

		private void OnEnable()
		{
			ResolveReferences();
			RefreshTransformHudVisibility();
		}

		private void LateUpdate()
		{
			if (playerController == null)
				ResolveReferences();

			RefreshTransformHudVisibility();
		}

		private void ResolveReferences()
		{
			if (playerController == null)
				playerController = FindAnyObjectByType<PlayerController>();
		}

		private void RefreshTransformHudVisibility()
		{
			bool transformed = playerController != null && playerController.IsTransformed;
			if (transformed == lastTransformedState && transformHudRoot != null && transformHudRoot.activeSelf == transformed)
				return;

			lastTransformedState = transformed;
			if (transformHudRoot != null)
				transformHudRoot.SetActive(transformed);
		}

		public void SetHealth(float current, float max)
		{
			ApplyBarValue(healthFill, healthText, current, max, "HP");
		}

		public void SetStamina(float current, float max)
		{
			ApplyBarValue(staminaFill, staminaText, current, max, "Stamina");
		}

		public void SetMana(float current, float max)
		{
			ApplyBarValue(manaFill, manaText, current, max, "Mana");
		}

		public void SetTransformHealth(float current, float max)
		{
			ApplyBarValue(transformHealthFill, transformHealthText, current, max, "HP");
		}

		private static void ApplyBarValue(Image fillImage, TextMeshProUGUI valueText, float current, float max, string label)
		{
			float clampedCurrent = Mathf.Max(0f, current);
			float clampedMax = Mathf.Max(0.0001f, max);
			float ratio = Mathf.Clamp01(clampedCurrent / clampedMax);

			if (fillImage != null)
				fillImage.fillAmount = ratio;

			if (valueText != null)
				valueText.text = $"{label}: {Mathf.RoundToInt(clampedCurrent)}/{Mathf.RoundToInt(clampedMax)}";
		}
	}
}
