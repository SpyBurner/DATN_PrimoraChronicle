using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class HttpServiceSubsystem : IHttpServiceSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IHttpServiceController _controller;
    [Inject] private readonly IHttpServiceModel _model;

    public event UnityAction<int> RequestQueueCountChanged;
    public event UnityAction<bool> IsRequestingChanged;

    public void Initialize()
    {
        if (_model?.RequestQueueCount != null)
            _model.RequestQueueCount.OnChanged += HandleRequestQueueCountChanged;

        if (_model?.IsRequesting != null)
            _model.IsRequesting.OnChanged += HandleIsRequestingChanged;
    }

    public void Dispose()
    {
        if (_model?.RequestQueueCount != null)
            _model.RequestQueueCount.OnChanged -= HandleRequestQueueCountChanged;

        if (_model?.IsRequesting != null)
            _model.IsRequesting.OnChanged -= HandleIsRequestingChanged;
    }

    public Task<T> Get<T>(string url) => _controller.Get<T>(url);

    public Task<T> Post<T>(string url, object payload) => _controller.Post<T>(url, payload);

    public Task<string> Get(string url) => _controller.Get(url);

    public Task<string> Post(string url, object payload) => _controller.Post(url, payload);

    public void SetAuthToken(string token) => _controller.SetAuthToken(token);

    private void HandleRequestQueueCountChanged()
    {
        try
        {
            RequestQueueCountChanged?.Invoke(_model.RequestQueueCount.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }

    private void HandleIsRequestingChanged()
    {
        try
        {
            IsRequestingChanged?.Invoke(_model.IsRequesting.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }
}
