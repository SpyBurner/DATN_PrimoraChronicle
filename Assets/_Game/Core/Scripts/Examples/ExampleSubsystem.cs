using System;
using UnityEngine.Events;
using Zenject;

public class ExampleSubsystem : IExampleSubsystem, IInitializable, IDisposable
{
    [Inject] readonly IExampleController _controller;
    [Inject] readonly IExampleModel _model;

    public event UnityAction<bool> IsActiveChanged;
    public event UnityAction<int> CounterChanged;

    public void Initialize()
    {
        if (_model?.IsActive != null)
            _model.IsActive.OnChanged += HandleIsActiveChanged;
        if (_model?.Counter != null)
            _model.Counter.OnChanged += HandleCounterChanged;
    }

    public void Dispose()
    {
        if (_model?.IsActive != null)
            _model.IsActive.OnChanged -= HandleIsActiveChanged;
        if (_model?.Counter != null)
            _model.Counter.OnChanged -= HandleCounterChanged;
    }

    // Forwarded controller methods
    public System.Threading.Tasks.Task ToggleActive() => _controller.ToggleActive();
    public void Increment() => _controller.Increment();
    public int GetCounter() => _controller.GetCounter();

    // Internal handlers to forward model changes
    void HandleIsActiveChanged()
    {
        try { IsActiveChanged?.Invoke(_model.IsActive.Value); } catch { }
    }
    void HandleCounterChanged()
    {
        try { CounterChanged?.Invoke(_model.Counter.Value); } catch { }
    }
}