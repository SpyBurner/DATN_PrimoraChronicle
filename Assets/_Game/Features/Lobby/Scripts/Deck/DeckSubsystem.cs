using System;
using Zenject;

public class DeckSubsystem : IDeckSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IDeckController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public void EditDeck() => _controller.EditDeck();
}
