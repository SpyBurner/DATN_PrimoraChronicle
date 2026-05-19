using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
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
    public event UnityAction<Core.IUIRoot> UIRootChanged;

    public int TotalPanelCount => _model.Panels.Value.Count;

    public void Initialize()
    {
        if (_model?.Panels != null)
            _model.Panels.OnChanged += HandlePanelsChanged;

        if (_model?.PanelsByLayer != null)
            _model.PanelsByLayer.OnChanged += HandlePanelsByLayerChanged;

        if (_model?.PopupStack != null)
            _model.PopupStack.OnChanged += HandlePopupStackChanged;

        if (_model?.UIRoot != null)
            _model.UIRoot.OnChanged += HandleUIRootChanged;
    }

    public void Dispose()
    {
        if (_model?.Panels != null)
            _model.Panels.OnChanged -= HandlePanelsChanged;

        if (_model?.PanelsByLayer != null)
            _model.PanelsByLayer.OnChanged -= HandlePanelsByLayerChanged;

        if (_model?.PopupStack != null)
            _model.PopupStack.OnChanged -= HandlePopupStackChanged;

        if (_model?.UIRoot != null)
            _model.UIRoot.OnChanged -= HandleUIRootChanged;
    }

    // Controller surface - forward calls to the controller
    public void RegisterPanel(IUIPanel panel) => _controller.RegisterPanel(panel);

    public void UnregisterPanel(IUIPanel panel) => _controller.UnregisterPanel(panel);
    public void RegisterUIRoot(IUIRoot uIRoot) => _controller.RegisterUIRoot(uIRoot);
    public void UnregisterUIRoot() => _controller.UnregisterUIRoot();
    public IUIRoot GetUIRoot() => _controller.GetUIRoot();
    public T GetPanel<T>() where T : class, IUIPanel => _controller.GetPanel<T>();

    public Task Show<T>() where T : class, IUIPanel => _controller.Show<T>();
    public Task Show(GameObject prefab) => _controller.Show(prefab);
    public Task ShowDefaultScreenForScene(string sceneName = null) => _controller.ShowDefaultScreenForScene(sceneName);
    public Task Close(IUIPanel panel) => _controller.Close(panel);
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

    private void HandleUIRootChanged()
    {
        try
        {
            UIRootChanged?.Invoke(_model.UIRoot.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }
}