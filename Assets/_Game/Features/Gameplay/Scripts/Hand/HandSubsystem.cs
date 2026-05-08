using System.Collections.Generic;
using UnityEngine.Events;
using Zenject;

public class HandSubsystem : IHandSubsystem
{
    [Inject] private readonly IHandController _controller;
    [Inject] private readonly IHandModel _model;

    public event UnityAction<List<string>> CardsChanged;

    public void Initialize()
    {
        if (_model?.Cards != null)
            _model.Cards.OnChanged += HandleCardsChanged;
    }

    public void Dispose()
    {
        if (_model?.Cards != null)
            _model.Cards.OnChanged -= HandleCardsChanged;
    }

    public void PlayCard(string cardId) => _controller.PlayCard(cardId);

    private void HandleCardsChanged()
    {
        try { CardsChanged?.Invoke(_model.Cards.Value); } catch { }
    }
}
