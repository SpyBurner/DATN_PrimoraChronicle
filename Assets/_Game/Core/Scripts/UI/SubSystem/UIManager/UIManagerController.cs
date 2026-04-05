using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

internal class UIManagerController : IUIManagerController
{
    [Inject]
    private readonly IUIManagerModel _model;
    [Inject]
    private readonly UIMappingSO _uiMapping;

    private UIRoot _uiRoot;

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
    public void RegisterUIRoot(UIRoot uIRoot)
    {
        if (_uiRoot != null)
        {
            UnregisterUIRoot();
        }
        _uiRoot = uIRoot;
    }
    public void UnregisterUIRoot()
    {
        _uiRoot = null;
    }
    public UIRoot GetUIRoot() => _uiRoot;

    public T GetPanel<T>() where T : class, IUIPanel
    {
        if (_model.Panels.Value.TryGetValue(typeof(T), out var panel))
        {
            return panel as T;
        }
        throw new Exception($"Panel of type {typeof(T)} not found.");
    }

    public async Task ShowScreen<T>() where T : class, IUIPanel
    {
        var prefab = _uiMapping.GetPrefabByClassType(typeof(T));
        await ShowView(prefab.gameObject);
    }

    public async Task ShowDefaultScreenForScene(string sceneName = null)
    {
        Debug.Log($"Showing default screen for scene: {sceneName}");
        sceneName ??= SceneManager.GetActiveScene().name;
        var prefab = _uiMapping.GetDefaultPrefabBySceneName(sceneName);
        await ShowView(prefab.gameObject);
    }

    private async Task ShowView(GameObject prefab)
    {
        if (prefab == null)
        {
            throw new Exception("Prefab cannot be null.");
        }
        var uiPanel = prefab.GetComponent<IUIPanel>();
        Debug.Log($"uiroot is null: {_uiRoot == null}, prefab has UIPanel: {uiPanel != null}");
        var parent = uiPanel != null ? _uiRoot.GetLayerParent(uiPanel.Layer) : _uiRoot.transform;
        var result = await GameObject.InstantiateAsync(prefab, parent);
        var instance = result[0];
        instance.GetComponent<IUIPanel>()?.Show();
        await Task.Yield();
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

    public async Task FadeIn()
    {
        await Task.Yield();
    }

    public async Task FadeOut()
    {
        await Task.Yield();
    }

}

