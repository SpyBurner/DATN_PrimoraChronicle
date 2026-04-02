using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class UIManagerSubsystem : IUIManagerSubsystem
{
    [Inject] private readonly IUIManagerController _controller;
    [Inject] private readonly IUIManagerModel _model;

    // Expose only the model-change notifications as UnityAction events
    public event UnityAction<Dictionary<Type, IUIPanel>> PanelsChanged;
    public event UnityAction<Dictionary<UILayer, List<IUIPanel>>> PanelsByLayerChanged;
    public event UnityAction<Stack<IUIPanel>> PopupStackChanged;

    public void Initialize()
    {
        if (_model?.Panels != null)
            _model.Panels.OnChanged += HandlePanelsChanged;

        if (_model?.PanelsByLayer != null)
            _model.PanelsByLayer.OnChanged += HandlePanelsByLayerChanged;

        if (_model?.PopupStack != null)
            _model.PopupStack.OnChanged += HandlePopupStackChanged;
    }

    public void Dispose()
    {
        if (_model?.Panels != null)
            _model.Panels.OnChanged -= HandlePanelsChanged;

        if (_model?.PanelsByLayer != null)
            _model.PanelsByLayer.OnChanged -= HandlePanelsByLayerChanged;

        if (_model?.PopupStack != null)
            _model.PopupStack.OnChanged -= HandlePopupStackChanged;
    }

    // Controller surface - forward calls to the controller
    public void RegisterPanel(IUIPanel panel) => _controller.RegisterPanel(panel);

    public void UnregisterPanel(IUIPanel panel) => _controller.UnregisterPanel(panel);

    public T GetPanel<T>() where T : class, IUIPanel => _controller.GetPanel<T>();

    public GameObject GetPrefab(UIIdentifier uiid) => _controller.GetPrefab(uiid);

    public Task ShowScreen<T>() where T : class, IUIPanel => _controller.ShowScreen<T>();

    public Task ShowPopup<T>() where T : class, IUIPanel => _controller.ShowPopup<T>();

    public Task ClosePopup() => _controller.ClosePopup();

    public Task FadeIn() => _controller.FadeIn();

    public Task FadeOut() => _controller.FadeOut();

    // Local handlers that forward the model state to subscribers via UnityAction events
    private void HandlePanelsChanged()
    {
        try
        {
            PanelsChanged?.Invoke(_model.Panels.Value);
        }
        catch (Exception)
        {
            // swallow to avoid breaking sender
        }
    }

    private void HandlePanelsByLayerChanged()
    {
        try
        {
            PanelsByLayerChanged?.Invoke(_model.PanelsByLayer.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }

    private void HandlePopupStackChanged()
    {
        try
        {
            PopupStackChanged?.Invoke(_model.PopupStack.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }
}