using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;
using Zenject;

public class MatchHistorySubsystem : IMatchHistorySubsystem
{
    [Inject] private readonly IMatchHistoryController _controller;
    [Inject] private readonly IMatchHistoryModel _model;

    public event UnityAction<List<MatchHistoryData>> MatchHistoryChanged;

    public void Initialize()
    {
        if (_model?.MatchHistory != null)
            _model.MatchHistory.OnChanged += HandleMatchHistoryChanged;
    }

    public void Dispose()
    {
        if (_model?.MatchHistory != null)
            _model.MatchHistory.OnChanged -= HandleMatchHistoryChanged;
    }

    public Task LoadMatchHistory() => _controller.LoadMatchHistory();

    private void HandleMatchHistoryChanged()
    {
        try { MatchHistoryChanged?.Invoke(_model.MatchHistory.Value); } catch { }
    }
}
