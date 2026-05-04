using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class DeckBuildSubsystem : IDeckBuildSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IDeckBuildController _controller;
    [Inject] private readonly IDeckBuildModel _model;

    public event UnityAction<int> DeckSizeChanged;
    public event UnityAction<bool> IsValidChanged;

    public void Initialize()
    {
        if (_model?.DeckSize != null)
            _model.DeckSize.OnChanged += HandleDeckSizeChanged;

        if (_model?.IsValid != null)
            _model.IsValid.OnChanged += HandleIsValidChanged;
    }

    public void Dispose()
    {
        if (_model?.DeckSize != null)
            _model.DeckSize.OnChanged -= HandleDeckSizeChanged;

        if (_model?.IsValid != null)
            _model.IsValid.OnChanged -= HandleIsValidChanged;
    }

    public Task LoadDeck(string deckId) => _controller.LoadDeck(deckId);
    public void AddCardToDeck(string cardId) => _controller.AddCardToDeck(cardId);
    public void RemoveCardFromDeck(string cardId) => _controller.RemoveCardFromDeck(cardId);
    public Task SaveDeck(string deckName) => _controller.SaveDeck(deckName);

    private void HandleDeckSizeChanged()
    {
        try { DeckSizeChanged?.Invoke(_model.DeckSize.Value); } catch { }
    }

    private void HandleIsValidChanged()
    {
        try { IsValidChanged?.Invoke(_model.IsValid.Value); } catch { }
    }
}
