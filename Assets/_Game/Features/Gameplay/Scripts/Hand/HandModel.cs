using System.Collections.Generic;
using UnityObservables;

public class HandModel : IHandModel
{
    private Observable<List<string>> _cards = new(new List<string>());
    public Observable<List<string>> Cards => _cards;

    public void Initialize() { }

    public void Dispose()
    {
        _cards.Value = new List<string>();
    }

    public void ApplyState(HandStateData data)
    {
        _cards.Value = new List<string>(data.Cards);
    }
}
