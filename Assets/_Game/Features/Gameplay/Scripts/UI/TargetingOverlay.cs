using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Zenject;

public class TargetingOverlay : MonoBehaviour
{
    [Inject] private readonly ITargetingSubsystem _targeting;
    [Inject] private readonly IBoardSubsystem _board;
    [Inject] private readonly IUnitSubsystem _unit;
    [Inject] private readonly INetworkManagerSubsystem _network;

    [Header("Tile Highlight Prefab")]
    [SerializeField] private TileHighlight _tileHighlightPrefab;

    [Header("Colors")]
    [SerializeField] private Color _rangeColor = new Color(1f, 0.92f, 0.016f, 0.6f);
    [SerializeField] private Color _validTargetColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    [SerializeField] private Color _invalidTargetColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    [Header("Settings")]
    [SerializeField] private float _highlightYOffset = 0.05f;
    [SerializeField] private LayerMask _tileLayerMask = ~0;

    private readonly List<TileHighlight> _highlights = new();
    private readonly Dictionary<HexCoord, TileHighlight> _highlightMap = new();

    private void Awake()
    {
        if (_tileHighlightPrefab == null) throw new System.Exception("[TargetingOverlay._tileHighlightPrefab] Not assigned in Inspector — see wiring-F4.md F4.6");
    }
    private TargetingRequest _activeRequest;
    private HexCoord _hoveredCoord;
    private bool _isActive;
    private Camera _mainCamera;
    private PlayerRef _localPlayer;

    private void OnEnable()
    {
        _mainCamera = Camera.main;
        _localPlayer = _network.Runner != null ? _network.Runner.LocalPlayer : default;

        _targeting.TargetingStarted += OnTargetingStarted;
        _targeting.HighlightedTilesChanged += OnHighlightedTilesChanged;
        _targeting.TargetConfirmed += OnTargetConfirmed;
        _targeting.TargetingCancelled += OnTargetingCancelled;
    }

    private void OnDisable()
    {
        _targeting.TargetingStarted -= OnTargetingStarted;
        _targeting.HighlightedTilesChanged -= OnHighlightedTilesChanged;
        _targeting.TargetConfirmed -= OnTargetConfirmed;
        _targeting.TargetingCancelled -= OnTargetingCancelled;
        ClearHighlights();
    }

    private void Update()
    {
        if (!_isActive) return;
        if (_mainCamera == null) return;

        HandleHoverInput();
        HandleClickInput();
        HandleCancelInput();
    }

    private void OnTargetingStarted(TargetingRequest request)
    {
        try
        {
            _activeRequest = request;
            _isActive = true;
            _hoveredCoord = HexCoord.Invalid;
            ShowRangeHighlights();
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnHighlightedTilesChanged(IReadOnlyList<HexCoord> tiles)
    {
        try
        {
            ClearHighlights();
            if (tiles == null) return;

            foreach (var coord in tiles)
                SpawnHighlight(coord, _rangeColor);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnTargetConfirmed(HexCoord coord)
    {
        try
        {
            _isActive = false;
            ClearHighlights();
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnTargetingCancelled()
    {
        try
        {
            _isActive = false;
            ClearHighlights();
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void HandleHoverInput()
    {
        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 200f, _tileLayerMask)) return;

        if (!_board.TryResolveWorldToHex(hit.point, out var coord)) return;
        if (coord == _hoveredCoord) return;

        _hoveredCoord = coord;
        _targeting.HoverTile(coord);
        RefreshHighlightColors();
    }

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 200f, _tileLayerMask)) return;
        if (!_board.TryResolveWorldToHex(hit.point, out var coord)) return;

        if (!IsValidTarget(coord)) return;

        _targeting.ConfirmTarget(coord);
    }

    private void HandleCancelInput()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            _targeting.Cancel();
    }

    private void ShowRangeHighlights()
    {
        ClearHighlights();

        var highlightedTiles = _targeting.HighlightedTiles;
        if (highlightedTiles == null) return;

        foreach (var coord in highlightedTiles)
            SpawnHighlight(coord, _rangeColor);
    }

    private void RefreshHighlightColors()
    {
        var highlightedTiles = _targeting.HighlightedTiles;
        if (highlightedTiles == null) return;

        foreach (var coord in highlightedTiles)
        {
            if (!_highlightMap.TryGetValue(coord, out var highlight)) continue;

            Color color;
            if (coord == _hoveredCoord)
                color = IsValidTarget(coord) ? _validTargetColor : _invalidTargetColor;
            else
                color = _rangeColor;

            highlight.SetColor(color);
        }
    }

    private bool IsValidTarget(HexCoord coord)
    {
        var mask = _activeRequest.Mask;

        if (mask == TargetMask.Self) return false;

        bool isEmpty = _board.IsEmpty(coord);

        if ((mask & TargetMask.EmptyTile) != 0 && isEmpty)
            return true;

        if (!isEmpty)
        {
            bool isEnemyUnit = IsEnemyAt(coord);
            bool isAllyUnit = !isEnemyUnit;

            if ((mask & TargetMask.Enemy) != 0 && isEnemyUnit) return true;
            if ((mask & TargetMask.Ally) != 0 && isAllyUnit) return true;
        }

        return false;
    }

    private bool IsEnemyAt(HexCoord coord)
    {
        foreach (var netId in _unit.AllUnits)
        {
            if (!_unit.TryGetPublic(netId, out var unitData)) continue;
            if (unitData.Position == coord)
                return unitData.Owner != _localPlayer;
        }
        return false;
    }

    private void SpawnHighlight(HexCoord coord, Color color)
    {
        if (_tileHighlightPrefab == null) return;

        Vector3 worldPos = _board.GetWorldPosition(coord);
        worldPos.y += _highlightYOffset;

        var highlight = Instantiate(_tileHighlightPrefab, worldPos, Quaternion.identity, transform);
        highlight.SetColor(color);

        _highlights.Add(highlight);
        _highlightMap[coord] = highlight;
    }

    private void ClearHighlights()
    {
        foreach (var highlight in _highlights)
            if (highlight != null) Destroy(highlight.gameObject);
        _highlights.Clear();
        _highlightMap.Clear();
        _hoveredCoord = HexCoord.Invalid;
    }
}
