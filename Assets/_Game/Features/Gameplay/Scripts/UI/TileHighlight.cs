using UnityEngine;

public class TileHighlight : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;

    public void SetColor(Color color)
    {
        if (_renderer != null)
            _renderer.material.color = color;
    }
}
