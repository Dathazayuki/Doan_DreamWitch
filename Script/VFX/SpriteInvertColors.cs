using UnityEngine;

/// <summary>
/// Component tiện ích để bật/tắt hiệu ứng đảo màu (invert colors) trên SpriteRenderer.
/// Sử dụng shader Custom/InvertColors.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteInvertColors : MonoBehaviour
{
    [Header("Invert Settings")]
    [Tooltip("Bật/tắt hiệu ứng đảo màu")]
    [SerializeField] private bool _invertOnStart = false;

    [Tooltip("Cường độ đảo màu (0 = không đảo, 1 = đảo hoàn toàn)")]
    [Range(0f, 1f)]
    [SerializeField] private float _invertStrength = 1f;

    private SpriteRenderer _spriteRenderer;
    private Material _invertMaterial;
    private Material _originalMaterial;

    private static readonly int InvertStrengthId = Shader.PropertyToID("_InvertStrength");
    private static readonly string ShaderName = "Custom/InvertColors";

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalMaterial = _spriteRenderer.material;

        // Tạo material từ shader InvertColors
        Shader invertShader = Shader.Find(ShaderName);
        if (invertShader == null)
        {
            Debug.LogError($"[SpriteInvertColors] Không tìm thấy shader '{ShaderName}'. " +
                           "Hãy đảm bảo file InvertColors.shader tồn tại trong project.");
            enabled = false;
            return;
        }

        _invertMaterial = new Material(invertShader);
        _invertMaterial.name = "InvertColors_Instance";
    }

    private void Start()
    {
        if (_invertOnStart)
            SetInvert(true);
    }

    private void OnDestroy()
    {
        // Dọn dẹp material được tạo động
        if (_invertMaterial != null)
            Destroy(_invertMaterial);
    }

    /// <summary>
    /// Bật hoặc tắt hiệu ứng đảo màu.
    /// </summary>
    public void SetInvert(bool invert)
    {
        if (_invertMaterial == null) return;

        if (invert)
        {
            // Sao chép texture của sprite sang material đảo màu
            _invertMaterial.mainTexture = _spriteRenderer.sprite != null
                ? _spriteRenderer.sprite.texture
                : null;
            _invertMaterial.SetFloat(InvertStrengthId, _invertStrength);
            _spriteRenderer.material = _invertMaterial;
        }
        else
        {
            _spriteRenderer.material = _originalMaterial;
        }
    }

    /// <summary>
    /// Toggle hiệu ứng đảo màu.
    /// </summary>
    public void ToggleInvert()
    {
        bool isInverted = _spriteRenderer.material == _invertMaterial;
        SetInvert(!isInverted);
    }

    /// <summary>
    /// Đặt cường độ đảo màu (0-1).
    /// </summary>
    public void SetInvertStrength(float strength)
    {
        _invertStrength = Mathf.Clamp01(strength);
        if (_invertMaterial != null)
            _invertMaterial.SetFloat(InvertStrengthId, _invertStrength);
    }

    /// <summary>
    /// Kiểm tra xem hiệu ứng đảo màu có đang bật không.
    /// </summary>
    public bool IsInverted => _spriteRenderer != null && _spriteRenderer.material == _invertMaterial;
}
