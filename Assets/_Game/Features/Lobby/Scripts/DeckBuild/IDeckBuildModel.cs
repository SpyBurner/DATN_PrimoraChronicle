using UnityObservables;
using System.Collections.Generic;

public interface IDeckBuildModel : IModel
{
    Observable<List<string>> DeckCards { get; }
    Observable<int> DeckSize { get; }
    Observable<bool> IsValid { get; }
}
