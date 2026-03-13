using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zenject;

public class UIManagerController : IUIManagerController
{
    [Inject]
    private readonly IUIManagerModel _model;

    public void RegisterPanel(IUIPanel panel)
    {    
        var type = panel.GetType();
        if (_model.Panels.Value.ContainsKey(type))
        {
            throw new Exception($"Panel of type {type} is already registered.");
        }
        _model.Panels.Value[type] = panel;
        if (!_model.PanelsByLayer.Value.ContainsKey(panel.Layer))
        {
            _model.PanelsByLayer.Value[panel.Layer] = new List<IUIPanel>();
        }
        _model.PanelsByLayer.Value[panel.Layer].Add(panel);
    }

    public void UnregisterPanel(IUIPanel panel)
    {
        var type = panel.GetType();
        if (_model.Panels.Value.Remove(type))
        {
            if (_model.PanelsByLayer.Value.TryGetValue(panel.Layer, out var panelsInLayer))
            {
                panelsInLayer.Remove(panel);
                if (panelsInLayer.Count == 0)
                {
                    _model.PanelsByLayer.Value.Remove(panel.Layer);
                }
            }
        }
        else
        {
            throw new Exception($"Panel of type {type} is not registered.");
        }
    }

    public T GetPanel<T>() where T : class, IUIPanel
    {
        if (_model.Panels.Value.TryGetValue(typeof(T), out var panel))
        {
            return panel as T;
        }
        throw new Exception($"Panel of type {typeof(T)} not found.");
    }

    public Task ShowScreen<T>() where T : class, IUIPanel
    {
        if (_model.Panels.Value.TryGetValue(typeof(T), out var panel))
        {
            // Hide all panels in the same layer
            if (_model.PanelsByLayer.Value.TryGetValue(panel.Layer, out var panelsInLayer))
            {
                foreach (var p in panelsInLayer)
                {
                    p.Hide();
                }
            }
            panel.Show();
            return Task.CompletedTask;
        }
        throw new Exception($"Panel of type {typeof(T)} not found.");
    }

    public Task ShowPopup<T>() where T : class, IUIPanel
    {
        if (_model.Panels.Value.TryGetValue(typeof(T), out var panel))
        {
            panel.Show();
            _model.PopupStack.Value.Push(panel);
            return Task.CompletedTask;
        }
        throw new Exception($"Panel of type {typeof(T)} not found.");
    }

    public Task ClosePopup()
    {
        if (_model.PopupStack.Value.Count > 0)
        {
            var panel = _model.PopupStack.Value.Pop();
            panel.Hide();
            return Task.CompletedTask;
        }
        throw new Exception("No popups to close.");
    }

    public Task FadeIn()
    {
        throw new NotImplementedException();
    }

    public Task FadeOut()
    {
        throw new NotImplementedException();
    }
}

