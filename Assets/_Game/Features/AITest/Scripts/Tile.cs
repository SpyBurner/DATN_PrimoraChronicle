using System;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public int P { get; private set; }
    public int Q { get; private set; }
    public int R { get; private set; }
    public Unit OccupiedBy { get; set; }

    private Button _button;
    private Image _image;
    private Color _defaultColor;
    private Action<Tile> _onClick;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
        _defaultColor = _image.color;
        _button.onClick.AddListener(() => _onClick?.Invoke(this));
    }

    public void SetHighlight(bool on, Color? color = null)
    {
        _image.color = on ? (color ?? Color.green) : _defaultColor;
    }

    public void SetCoordinate(int p, int q, int r)
    {
        P = p;
        Q = q;
        R = r;
    }

    public void SetOnClick(Action<Tile> onClick)
    {
        _onClick = onClick;
    }
}
