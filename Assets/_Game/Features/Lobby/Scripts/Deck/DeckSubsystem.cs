using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;
using Zenject;

public class DeckSubsystem : IDeckSubsystem
{
    [Inject] private readonly IDeckController _controller;
    [Inject] private readonly IDeckModel _model;

    public event UnityAction<int> DeckCountChanged;
    public event UnityAction<IReadOnlyList<DeckSummaryData>> DecksChanged;

    public void Initialize()
    {
        if (_model?.DeckCount != null)
            _model.DeckCount.OnChanged += HandleDeckCountChanged;
        
        if (_model?.Decks != null)
            _model.Decks.OnChanged += HandleDecksChanged;
    }

    public void Dispose()
    {
        if (_model?.DeckCount != null)
            _model.DeckCount.OnChanged -= HandleDeckCountChanged;
            
        if (_model?.Decks != null)
            _model.Decks.OnChanged -= HandleDecksChanged;
    }

    public Task LoadDecks() => _controller.LoadDecks();

    private void HandleDeckCountChanged()
    {
        try { DeckCountChanged?.Invoke(_model.DeckCount.Value); } catch { }
    }

    private void HandleDecksChanged()
    {
        try { DecksChanged?.Invoke(_model.Decks.Value); } catch { }
    }
}

