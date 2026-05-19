using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Zenject;

public class TargetingSubsystem : ITargetingSubsystem
{
    [Inject] private readonly IBoardSubsystem _board;

    public event UnityAction<TargetingRequest> TargetingStarted;
    public event UnityAction<IReadOnlyList<HexCoord>> HighlightedTilesChanged;
    public event UnityAction<HexCoord> TargetConfirmed;
    public event UnityAction TargetingCancelled;

    private TargetingRequest _currentRequest;
    private UnityAction<HexCoord> _onConfirmed;
    private readonly List<HexCoord> _highlighted = new();

    public bool IsTargeting { get; private set; }
    public IReadOnlyList<HexCoord> HighlightedTiles => _highlighted;

    public void Initialize() { }

    public void Dispose() => Cancel();

    public void BeginTargeting(TargetingRequest request, UnityAction<HexCoord> onConfirmed)
    {
        _currentRequest = request;
        _onConfirmed = onConfirmed;
        IsTargeting = true;

        RefreshRangeHighlights();

        try { TargetingStarted?.Invoke(request); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    public void HoverTile(HexCoord coord)
    {
        if (!IsTargeting) return;
        // Track B: tint hovered valid tile green, others yellow/red
    }

    public void ConfirmTarget(HexCoord coord)
    {
        if (!IsTargeting) return;

        IsTargeting = false;
        _highlighted.Clear();
        var cb = _onConfirmed;
        _onConfirmed = null;

        try { TargetConfirmed?.Invoke(coord); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }

        try { cb?.Invoke(coord); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    public void Cancel()
    {
        if (!IsTargeting) return;

        IsTargeting = false;
        _onConfirmed = null;
        _highlighted.Clear();

        try { HighlightedTilesChanged?.Invoke(_highlighted); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }

        try { TargetingCancelled?.Invoke(); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void RefreshRangeHighlights()
    {
        _highlighted.Clear();
        // Track B fills this with IBoardSubsystem.GetTilesInRange filtered by TargetMask
        try { HighlightedTilesChanged?.Invoke(_highlighted); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
