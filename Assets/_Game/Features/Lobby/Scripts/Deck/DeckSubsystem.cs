using System;
using System.Collections.Generic;
using Core;
using Zenject;

public class DeckSubsystem : IDeckSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IDeckController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public void EditDeck(DeckSO deckSO) => _controller.EditDeck(deckSO);
    public IReadOnlyList<DeckSO> LoadDecks() => _controller.LoadDecks();
}
