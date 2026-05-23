using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private int _size = 4;

    private Dictionary<string, Tile> _tiles = new();
    private float _tileSize;

    public int Size => _size;
    public event Action<Tile> TileClicked;

    public void GenerateBoard()
    {
        RectTransform prefabRect = _tilePrefab.GetComponent<RectTransform>();
        _tileSize = prefabRect.sizeDelta.x;

        for (int p = -_size; p <= _size; p++)
        {
            int qMin = Mathf.Max(-_size, -p - _size);
            int qMax = Mathf.Min(_size, -p + _size);

            for (int q = qMin; q <= qMax; q++)
            {
                int r = -p - q;
                Tile tile = Instantiate(_tilePrefab, transform);
                tile.name = $"Tile_{p}_{q}_{r}";
                tile.SetCoordinate(p, q, r);
                tile.SetOnClick(OnTileClicked);

                RectTransform rect = tile.GetComponent<RectTransform>();
                rect.anchoredPosition = AxialToPosition(p, q);

                string key = $"{p}-{q}-{r}";
                _tiles[key] = tile;
            }
        }
    }

    private Vector2 AxialToPosition(int p, int q)
    {
        float spacing = _tileSize * 0.75f;
        float x = _tileSize * (Mathf.Sqrt(3f) * p + Mathf.Sqrt(3f) / 2f * q) * 0.5f;
        float y = spacing * q;
        return new Vector2(x, y);
    }

    public Tile GetTile(int p, int q, int r)
    {
        string key = $"{p}-{q}-{r}";
        _tiles.TryGetValue(key, out Tile tile);
        return tile;
    }

    private void OnTileClicked(Tile tile)
    {
        TileClicked?.Invoke(tile);
    }
}
