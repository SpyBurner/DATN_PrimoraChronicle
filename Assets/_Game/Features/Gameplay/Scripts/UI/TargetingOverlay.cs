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
    [Inject] private readonly IDebugLogger _debugLogger;

    [Header("Tile Highlight Prefab")]
    [SerializeField] private TileHighlight _tileHighlightPrefab;

    [Header("Colors")]
    [SerializeField] private Color _rangeColor = new(1f, 0.92f, 0.016f, 0.6f);
    [SerializeField] private Color _validTargetColor = new(0.2f, 0.8f, 0.2f, 0.8f);
    [SerializeField] private Color _invalidTargetColor = new(1f, 0.2f, 0.2f, 0.6f);

    [Header("Settings")]
    [SerializeField] private float _highlightYOffset = 0.05f;
    [SerializeField] private LayerMask _tileLayerMask = 1 << 6;

    private readonly List<TileHighlight> _highlights = new();
    private readonly Dictionary<HexCoord, TileHighlight> _highlightMap = new();
    private TileHighlight _hoverHighlight; // separate highlight for the hovered tile (may be out of range)

    private void Awake()
    {
        if (_tileHighlightPrefab == null) throw new Exception("[TargetingOverlay._tileHighlightPrefab] Not assigned in Inspector — see wiring-F4.md F4.6");
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
        _debugLogger.Log("LOG_TARGETING", nameof(TargetingOverlay), $"OnEnable localPlayer={_localPlayer} camera={(_mainCamera != null ? "ok" : "NULL")}");

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
            // Re-read localPlayer here in case Runner wasn't ready at OnEnable (common on remote client).
            if (_network.Runner != null)
                _localPlayer = _network.Runner.LocalPlayer;
            _debugLogger.Log("LOG_TARGETING", nameof(TargetingOverlay), $"TargetingStarted mask={request.Mask} range={request.Range} caster={request.Caster} localPlayer={_localPlayer} camera={(_mainCamera != null ? "ok" : "NULL")}");
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

    private bool TileRaycast(Ray ray, out RaycastHit hit)
    {
        if (_network.Runner != null)
            return _network.Runner.GetPhysicsScene().Raycast(ray.origin, ray.direction, out hit, 100f, _tileLayerMask, QueryTriggerInteraction.Collide);
        return Physics.Raycast(ray, out hit, 100f, _tileLayerMask, QueryTriggerInteraction.Collide);
    }

    private static bool TryGetTileCoord(RaycastHit hit, out HexCoord coord)
    {
        var tile = hit.collider.GetComponentInParent<HexTile>();
        if (tile == null) { coord = HexCoord.Invalid; return false; }
        coord = new HexCoord(tile.p, tile.q);
        return true;
    }

    private void HandleHoverInput()
    {
        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!TileRaycast(ray, out var hit)) return;

        if (!TryGetTileCoord(hit, out var coord)) return;
        if (coord == _hoveredCoord) return;

        _hoveredCoord = coord;
        _targeting.HoverTile(coord);
        RefreshHighlightColors();
    }

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

        _debugLogger.Log("LOG_TARGETING", nameof(TargetingOverlay), $"Click — layerMask={_tileLayerMask.value} usingFusionScene={_network.Runner != null} ray={ray}");

        if (!TileRaycast(ray, out var hit))
        {
            // Fallback: check default physics scene unrestricted to diagnose layer/scene mismatch.
            var allHits = Physics.RaycastAll(ray, 100f, -1, QueryTriggerInteraction.Collide);
            var names = new System.Text.StringBuilder();
            foreach (var h in allHits) names.Append($"{h.collider.gameObject.name}(layer {h.collider.gameObject.layer}) ");
            _debugLogger.Log("LOG_TARGETING", nameof(TargetingOverlay), $"Click — Raycast missed. Default scene hit {allHits.Length}: {(names.Length > 0 ? names.ToString() : "none")}");
            return;
        }

        _debugLogger.Log("LOG_TARGETING", nameof(TargetingOverlay), $"Click — hit '{hit.collider.gameObject.name}' layer={hit.collider.gameObject.layer} point={hit.point}");

        if (!TryGetTileCoord(hit, out var coord))
        {
            _debugLogger.Log("LOG_TARGETING", nameof(TargetingOverlay), $"Click — no HexTile component on hit object '{hit.collider.gameObject.name}'");
            return;
        }

        bool valid = IsValidTarget(coord);
        _debugLogger.Log("LOG_TARGETING", nameof(TargetingOverlay), $"Click — coord={coord} valid={valid} mask={_activeRequest.Mask} isEmpty={_board.IsEmpty(coord)}");

        if (!valid) return;

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
        // Reset all range highlights to yellow.
        foreach (var h in _highlights)
            if (h != null) h.SetColor(_rangeColor);

        // Destroy stale out-of-range hover highlight.
        if (_hoverHighlight != null) { Destroy(_hoverHighlight.gameObject); _hoverHighlight = null; }

        if (!_hoveredCoord.IsValid) return;

        Color hoverColor = IsValidTarget(_hoveredCoord) ? _validTargetColor : _invalidTargetColor;

        if (_highlightMap.TryGetValue(_hoveredCoord, out var rangeHighlight))
        {
            // Tile is in range — recolor the existing range highlight directly.
            rangeHighlight.SetColor(hoverColor);
        }
        else
        {
            // Tile is outside range — spawn a temporary highlight so the player still
            // sees feedback (red = invalid, no green since out-of-range is always invalid).
            Vector3 worldPos = _board.GetWorldPosition(_hoveredCoord);
            if (worldPos != Vector3.zero)
            {
                worldPos.y += _highlightYOffset;
                _hoverHighlight = Instantiate(_tileHighlightPrefab, worldPos, Quaternion.identity, transform);
                _hoverHighlight.SetColor(_invalidTargetColor);
            }
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
            bool isAllyUnit = IsAllyAt(coord);

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
            {
                bool isEnemy = unitData.Owner != _localPlayer;
                _debugLogger.Log("LOG_TARGETING", nameof(TargetingOverlay), $"IsEnemyAt coord={coord} unitOwner={unitData.Owner} localPlayer={_localPlayer} isEnemy={isEnemy}");
                return isEnemy;
            }
        }
        _debugLogger.Log("LOG_TARGETING", nameof(TargetingOverlay), $"IsEnemyAt coord={coord} — no unit found at tile");
        return false;
    }

    private bool IsAllyAt(HexCoord coord)
    {
        foreach (var netId in _unit.AllUnits)
        {
            if (!_unit.TryGetPublic(netId, out var unitData)) continue;
            if (unitData.Position == coord)
                return unitData.Owner == _localPlayer;
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
        if (_hoverHighlight != null) { Destroy(_hoverHighlight.gameObject); _hoverHighlight = null; }
        _hoveredCoord = HexCoord.Invalid;
    }
}
