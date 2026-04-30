using System.Collections.Generic;
using Core;

internal class DeckEditModel : IDeckEditModel
{
    private List<CardSO> _deckCards = new();
    private List<CardSO> _championCards = new();

    public DeckSO SelectedDeck { get; private set; }
    public IReadOnlyList<CardSO> DeckCards => _deckCards;
    public IReadOnlyList<CardSO> ChampionCards => _championCards;

    public void Initialize() { }
    public void Dispose() { }

    public void SetSelectedDeck(DeckSO deckSO)
    {
        SelectedDeck = deckSO;
    }

    public void SetRenderData(IEnumerable<CardSO> deckCards, IEnumerable<CardSO> championCards)
    {
        _deckCards = deckCards == null ? new List<CardSO>() : new List<CardSO>(deckCards);
        _championCards = championCards == null ? new List<CardSO>() : new List<CardSO>(championCards);
    }
}