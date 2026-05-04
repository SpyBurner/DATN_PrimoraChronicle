using System;
using System.Threading.Tasks;
using Core;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class DeckSubsystem : IDeckSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IDeckController _controller;
    [Inject] private readonly IDeckModel _model;

    public event UnityAction<int> DeckCountChanged;

    public void Initialize()
    {
        if (_model?.DeckCount != null)
            _model.DeckCount.OnChanged += HandleDeckCountChanged;
    }

    public void Dispose()
    {
        if (_model?.DeckCount != null)
            _model.DeckCount.OnChanged -= HandleDeckCountChanged;
    }

    public Task LoadDecks() => _controller.LoadDecks();
    public void SelectDeck(DeckSO deckSO) => _controller.SelectDeck(deckSO);

    private void HandleDeckCountChanged()
    {
        try { DeckCountChanged?.Invoke(_model.DeckCount.Value); } catch { }
    }
}

