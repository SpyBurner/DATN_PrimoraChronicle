using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using UnityEngine.Events;
using Zenject;

public class DeckBuildSubsystem : IDeckBuildSubsystem
{
    [Inject] private readonly IDeckBuildController _controller;
    [Inject] private readonly IDeckBuildModel _model;

    public event UnityAction<string> CurrentDeckIdChanged;
    public event UnityAction<string> CurrentDeckNameChanged;
    public event UnityAction<IReadOnlyList<CardSO>> DeckCardsChanged;
    public event UnityAction<IReadOnlyList<CardSO>> ChampionCardsChanged;
    public event UnityAction<IReadOnlyList<CardSO>> AvailableCardsChanged;
    public event UnityAction<int> DeckSizeChanged;
    public event UnityAction<bool> IsValidChanged;

    public void Initialize()
    {
        if (_model == null) return;

        _model.CurrentDeckId.OnChanged += HandleCurrentDeckIdChanged;
        _model.CurrentDeckName.OnChanged += HandleCurrentDeckNameChanged;
        _model.DeckCards.OnChanged += HandleDeckCardsChanged;
        _model.ChampionCards.OnChanged += HandleChampionCardsChanged;
        _model.AvailableCards.OnChanged += HandleAvailableCardsChanged;
        _model.DeckSize.OnChanged += HandleDeckSizeChanged;
        _model.IsValid.OnChanged += HandleIsValidChanged;
    }

    public void Dispose()
    {
        if (_model == null) return;

        _model.CurrentDeckId.OnChanged -= HandleCurrentDeckIdChanged;
        _model.CurrentDeckName.OnChanged -= HandleCurrentDeckNameChanged;
        _model.DeckCards.OnChanged -= HandleDeckCardsChanged;
        _model.ChampionCards.OnChanged -= HandleChampionCardsChanged;
        _model.AvailableCards.OnChanged -= HandleAvailableCardsChanged;
        _model.DeckSize.OnChanged -= HandleDeckSizeChanged;
        _model.IsValid.OnChanged -= HandleIsValidChanged;
    }

    public Task LoadDeck(string deckId) => _controller.LoadDeck(deckId);
    public Task CreateEmptyDeck() => _controller.CreateEmptyDeck();
    public void AddCardToDeck(CardSO card) => _controller.AddCardToDeck(card);
    public void RemoveCardFromDeck(CardSO card) => _controller.RemoveCardFromDeck(card);
    public Task SaveDeck() => _controller.SaveDeck();

    private void HandleCurrentDeckIdChanged() => CurrentDeckIdChanged?.Invoke(_model.CurrentDeckId.Value);
    private void HandleCurrentDeckNameChanged() => CurrentDeckNameChanged?.Invoke(_model.CurrentDeckName.Value);
    private void HandleDeckCardsChanged() => DeckCardsChanged?.Invoke(_model.DeckCards.Value);
    private void HandleChampionCardsChanged() => ChampionCardsChanged?.Invoke(_model.ChampionCards.Value);
    private void HandleAvailableCardsChanged() => AvailableCardsChanged?.Invoke(_model.AvailableCards.Value);
    private void HandleDeckSizeChanged() => DeckSizeChanged?.Invoke(_model.DeckSize.Value);
    private void HandleIsValidChanged() => IsValidChanged?.Invoke(_model.IsValid.Value);
}
