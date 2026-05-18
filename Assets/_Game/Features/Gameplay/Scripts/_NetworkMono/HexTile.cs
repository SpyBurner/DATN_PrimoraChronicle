using Fusion;
using UnityEngine;

public class HexTile : NetworkBehaviour
{
    [Header("Axial Coordinates")]
    [Networked] public int p { get; set; }
    [Networked] public int q { get; set; }

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
