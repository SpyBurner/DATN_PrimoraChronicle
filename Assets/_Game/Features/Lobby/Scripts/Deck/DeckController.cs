using UnityEngine;
using Zenject;

internal class DeckController : IDeckController
{
    [Inject] private readonly IDeckModel _model;

    public void Initialize() { }

    public void EditDeck()
    {
        Debug.Log("Deck: Edit Deck");
    }
}
