using System;
using UnityEngine;
using UnityObservables;
using Zenject;

public class ExampleModel : IExampleModel, IInitializable, IDisposable
{
    private Observable<bool> _isActive;
    private Observable<int> _counter;

    public Observable<bool> IsActive { get => _isActive; }
    public Observable<int> Counter { get => _counter; }

    public void Initialize()
    {
        _isActive = new Observable<bool>();
        _counter = new Observable<int>();
        _isActive.Value = false;
        _counter.Value = 0;
    }

    public void Dispose()
    {
        // Clear values to sensible defaults
        _isActive.Value = false;
        _counter.Value = 0;
    }
}