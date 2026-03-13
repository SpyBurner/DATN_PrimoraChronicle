using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ExampleView : MonoBehaviour
{
    [SerializeField] Button _toggleButton;
    [SerializeField] Text _counterText;

    [Inject] private readonly IExampleSubsystem _subsystem;

    void OnEnable()
    {
        _subsystem.IsActiveChanged += OnActiveChanged;
        _subsystem.CounterChanged += OnCounterChanged;

        if (_toggleButton != null)
            _toggleButton.onClick.AddListener(OnToggleClicked);
    }

    void OnDisable()
    {
        _subsystem.IsActiveChanged -= OnActiveChanged;
        _subsystem.CounterChanged -= OnCounterChanged;

        if (_toggleButton != null)
            _toggleButton.onClick.RemoveListener(OnToggleClicked);
    }

    void OnActiveChanged(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    void OnCounterChanged(int value)
    {
        if (_counterText != null)
            _counterText.text = value.ToString();
    }

    // Event handlers may be async void — appropriate for UI callbacks.
    async void OnToggleClicked()
    {
        try
        {
            // await the async controller work
            await _subsystem.ToggleActive();
        }
        catch (Exception ex)
        {
            // Always catch to avoid unobserved exceptions from async void
            Debug.LogException(ex);
        }

        // Continue with synchronous call after await completes
        _subsystem.Increment();
    }
}