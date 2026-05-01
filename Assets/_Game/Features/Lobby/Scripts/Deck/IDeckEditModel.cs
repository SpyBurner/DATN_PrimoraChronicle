using System.Collections.Generic;
using Core;

public interface IDeckEditModel : IModel
{
    DeckSO SelectedDeck { get; }
    IReadOnlyList<CardSO> DeckCards { get; }
    IReadOnlyList<CardSO> ChampionCards { get; }
    IReadOnlyList<CardSO> AvailableCards { get; }
    void SetSelectedDeck(DeckSO deckSO);
    void SetRenderData(IEnumerable<CardSO> deckCards, IEnumerable<CardSO> championCards, IEnumerable<CardSO> availableCards);
}