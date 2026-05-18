using UnityObservables;

public interface IHttpServiceModel : IModel
{
    Observable<int> RequestQueueCount { get; }
    Observable<bool> IsRequesting { get; }
}
