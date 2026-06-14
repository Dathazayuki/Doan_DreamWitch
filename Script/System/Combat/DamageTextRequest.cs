using UnityEngine;

namespace DreamKnight.Systems.Combat
{
	public readonly struct DamageTextRequest
	{
		public DamageTextRequest(float amount, Vector3 worldPosition, Color color, float scaleMultiplier = 1f)
		{
			Amount = amount;
			WorldPosition = worldPosition;
			Color = color;
			ScaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
		}

		public float Amount { get; }
		public Vector3 WorldPosition { get; }
		public Color Color { get; }
		public float ScaleMultiplier { get; }
		public string Text => Mathf.CeilToInt(Mathf.Max(0f, Amount)).ToString();
	}
}
