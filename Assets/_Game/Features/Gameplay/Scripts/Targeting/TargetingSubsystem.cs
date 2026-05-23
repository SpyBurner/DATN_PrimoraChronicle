using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Zenject;

public class TargetingSubsystem : ITargetingSubsystem
{
    [Inject] private readonly IBoardSubsystem _board;
    [Inject] private readonly IUnitSubsystem _unit;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoading;

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

        _highlighted.Clear();

        if (!string.IsNullOrEmpty(_currentRequest.DisplayPattern)
            && _cardLoading.TryGetSkillData(_currentRequest.DisplayPattern, out var skillData)
            && skillData.display_pattern != null && skillData.display_pattern.Count > 0)
        {
            _highlighted.AddRange(HexPatternResolver.ResolveAll(coord, skillData.display_pattern, _board));
        }
        else
        {
            _highlighted.Add(coord);
        }

        try { HighlightedTilesChanged?.Invoke(_highlighted); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
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

        if (!_unit.TryGetPublic(_currentRequest.Caster, out var casterData))
        {
            try { HighlightedTilesChanged?.Invoke(_highlighted); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            return;
        }

        _highlighted.AddRange(_board.GetTilesInRange(casterData.Position, _currentRequest.Range));

        try { HighlightedTilesChanged?.Invoke(_highlighted); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
