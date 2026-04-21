using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class DeckPanel : UIPanel
{
    [Inject] private readonly IDeckSubsystem _deck;


    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    private void OnEditDeck() => _deck.EditDeck();
}
