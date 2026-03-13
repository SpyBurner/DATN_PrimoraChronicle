using UnityEngine.Events;
using UnityObservables;

public interface IExampleModel
{
    Observable<bool> IsActive { get; }
    Observable<int> Counter { get; }
}