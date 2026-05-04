using UnityObservables;

internal class HttpServiceModel : IHttpServiceModel
{
    private Observable<int> _requestQueueCount = new(0);
    private Observable<bool> _isRequesting = new(false);

    public Observable<int> RequestQueueCount { get => _requestQueueCount; }
    public Observable<bool> IsRequesting { get => _isRequesting; }

    public void Initialize()
    {
    }

    public void Dispose()
    {
        _requestQueueCount.Value = 0;
        _isRequesting.Value = false;
    }
}
