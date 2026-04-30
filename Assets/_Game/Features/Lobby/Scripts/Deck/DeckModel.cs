using System.Collections.Generic;
using Core;

internal class DeckModel : IDeckModel
{
    private readonly List<DeckSO> _decks = new();

    public IReadOnlyList<DeckSO> Decks => _decks;

    public void Initialize() { }
    public void Dispose() { }

    public void SetDecks(IReadOnlyList<DeckSO> decks)
    {
        _decks.Clear();

        if (decks == null)
        {
            return;
        }

        _decks.AddRange(decks);
    }
}
