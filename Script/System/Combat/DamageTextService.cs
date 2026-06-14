using DreamKnight.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamKnight.Systems.Combat
{
	[DisallowMultipleComponent]
	public class DamageTextService : MonoBehaviour
	{
		private static DamageTextService instance;

		[SerializeField] private DamageTextView damageTextPrefab;
		[SerializeField] private Canvas targetCanvas;
		[SerializeField] private Camera worldCamera;
		[SerializeField] private Vector2 screenOffset = new Vector2(0f, 56f);
		[SerializeField] private Vector2 randomHorizontalRange = new Vector2(-18f, 18f);
		[SerializeField] private Vector2 randomVerticalRange = new Vector2(8f, 26f);
		[SerializeField] private Color enemyDamageColor = new Color(1f, 0.93f, 0.45f, 1f);
		[SerializeField] private Color playerDamageColor = new Color(1f, 0.45f, 0.45f, 1f);
		[SerializeField] private Color criticalDamageColor = new Color(1f, 0.25f, 0.08f, 1f);
		[SerializeField] private float criticalDamageScaleMultiplier = 1.45f;
		[SerializeField] private int maxPoolSize = 48;

		private readonly Queue<DamageTextView> inactivePool = new Queue<DamageTextView>();
		private bool nextEnemyDamageCritical;

		public static void ShowEnemyDamage(float damage, Vector3 worldPosition)
		{
			DamageTextService service = EnsureInstance();
			bool critical = service.nextEnemyDamageCritical;
			service.nextEnemyDamageCritical = false;
			service.ShowEnemyDamageInternal(damage, worldPosition, critical);
		}

		public static void ShowEnemyDamage(float damage, Vector3 worldPosition, bool critical)
		{
			EnsureInstance().ShowEnemyDamageInternal(damage, worldPosition, critical);
		}

		public static void ShowPlayerDamage(float damage, Vector3 worldPosition)
		{
			EnsureInstance().Show(new DamageTextRequest(damage, worldPosition, EnsureInstance().playerDamageColor));
		}

		public static void MarkNextEnemyDamageCritical()
		{
			EnsureInstance().nextEnemyDamageCritical = true;
		}

		private static DamageTextService EnsureInstance()
		{
			if (instance != null)
				return instance;

			instance = FindFirstObjectByType<DamageTextService>();
			if (instance != null)
				return instance;

			GameObject root = new GameObject(nameof(DamageTextService));
			instance = root.AddComponent<DamageTextService>();
			return instance;
		}

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				Destroy(gameObject);
				return;
			}

			instance = this;
			EnsureCanvas();
			if (worldCamera == null)
				worldCamera = Camera.main;
		}

		private void Show(DamageTextRequest request)
		{
			EnsureCanvas();
			if (worldCamera == null)
				worldCamera = Camera.main;

			DamageTextView view = GetViewFromPool();

			Vector2 drift = new Vector2(
				Random.Range(randomHorizontalRange.x, randomHorizontalRange.y),
				Random.Range(randomVerticalRange.x, randomVerticalRange.y));

			view.Initialize(request.Text, request.Color, request.WorldPosition, worldCamera, screenOffset, drift, request.ScaleMultiplier, ReleaseViewToPool);
		}

		private void ShowEnemyDamageInternal(float damage, Vector3 worldPosition, bool critical)
		{
			Color color = critical ? criticalDamageColor : enemyDamageColor;
			float scale = critical ? criticalDamageScaleMultiplier : 1f;
			Show(new DamageTextRequest(damage, worldPosition, color, scale));
		}

		private void EnsureCanvas()
		{
			if (targetCanvas != null)
				return;

			targetCanvas = GetComponentInChildren<Canvas>();
			if (targetCanvas != null)
				return;

			GameObject canvasObject = new GameObject("DamageTextCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvasObject.transform.SetParent(transform, false);

			targetCanvas = canvasObject.GetComponent<Canvas>();
			targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

			CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.matchWidthOrHeight = 0.5f;
		}

		private DamageTextView GetViewFromPool()
		{
			DamageTextView view = null;
			while (inactivePool.Count > 0 && view == null)
				view = inactivePool.Dequeue();

			if (view == null)
			{
				view = damageTextPrefab != null
					? Instantiate(damageTextPrefab, targetCanvas.transform)
					: DamageTextView.CreateRuntime(targetCanvas.transform);
			}

			view.transform.SetParent(targetCanvas.transform, false);
			view.gameObject.SetActive(true);
			return view;
		}

		private void ReleaseViewToPool(DamageTextView view)
		{
			if (view == null)
				return;

			view.ResetForPool();

			if (inactivePool.Count >= Mathf.Max(1, maxPoolSize))
			{
				Destroy(view.gameObject);
				return;
			}

			view.gameObject.SetActive(false);
			view.transform.SetParent(targetCanvas.transform, false);
			inactivePool.Enqueue(view);
		}
	}
}
