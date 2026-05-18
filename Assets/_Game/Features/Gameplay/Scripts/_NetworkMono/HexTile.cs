using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Axial Coordinates")]
    public int p;
    public int q;

    private Renderer _renderer;
    private Color _originalColor;
    private bool _hasOriginalColor = false;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null && _renderer.material != null)
        {
            _originalColor = _renderer.material.color;
            _hasOriginalColor = true;
        }
    }

    public void SetCoordinates(int pCoord, int qCoord)
    {
        p = pCoord;
        q = qCoord;
        gameObject.name = $"HexTile_P{p}_Q{q}";
    }

    public void SetVisualColor(Color color)
    {
        if (_renderer != null && _renderer.material != null)
        {
            _renderer.material.color = color;
        }
    }

    public void ResetVisualColor()
    {
        if (_hasOriginalColor && _renderer != null && _renderer.material != null)
        {
            _renderer.material.color = _originalColor;
        }
    }
}
