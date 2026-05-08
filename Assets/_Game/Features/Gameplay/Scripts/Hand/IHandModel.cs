using System.Collections.Generic;
using UnityObservables;

public interface IHandModel : IModel
{
    Observable<List<string>> Cards { get; }
    void RequestPlayCard(string cardId);
}
