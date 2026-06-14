using UnityEngine;

[DisallowMultipleComponent]
public class SpriteHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer fillRenderer;

    [Header("Fill")]
    [SerializeField] private Transform fillRoot;

    private Vector3 fillStartScale = Vector3.one;
    private bool hasFillStartScale;

    private void Awake()
    {
        ResolveReferences();
        CacheStartScale();
    }

    public void SetHealth(float current, float max)
    {
        float value = max > 0f ? current / max : 0f;
        SetNormalized(value);
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
    }

    private void SetNormalized(float value)
    {
        ResolveReferences();
        CacheStartScale();

        if (fillRoot == null)
            return;

        Vector3 scale = fillStartScale;
        scale.x *= Mathf.Clamp01(value);
        fillRoot.localScale = scale;
    }

    private void ResolveReferences()
    {
        if (fillRenderer == null)
            fillRenderer = FindFillRenderer();

        if (fillRoot == null && fillRenderer != null)
            fillRoot = fillRenderer.transform;

    }

    private void CacheStartScale()
    {
        if (hasFillStartScale || fillRoot == null)
            return;

        fillStartScale = fillRoot.localScale;
        hasFillStartScale = true;
    }

    private SpriteRenderer FindFillRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer != null && renderer.name.ToLowerInvariant().Contains("fill"))
                return renderer;
        }

        return renderers.Length > 0 ? renderers[renderers.Length - 1] : null;
    }
}
