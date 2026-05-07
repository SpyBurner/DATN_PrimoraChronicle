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
    [Inject]
    private readonly DiContainer _container;
    [Inject]
    private readonly SceneContextRegistry _sceneContextRegistry;

    public void Initialize() { }
    public void Dispose() { }

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
    }
    public void RegisterUIRoot(UIRoot uIRoot)
    {
        if (_model.UIRoot.Value != null)
        {
            UnregisterUIRoot();
        }
        _model.UIRoot.Value = uIRoot;
        
        // BUG-8 Fix: Reverse responsibility. Trigger default screen when UIRoot is ready.
        ShowDefaultScreenForScene(); 
    }
    public void UnregisterUIRoot()
    {
        _model.UIRoot.Value = null;
    }
    public UIRoot GetUIRoot() => _model.UIRoot.Value;

    public T GetPanel<T>() where T : class, IUIPanel
    {
        if (_model.Panels.Value.TryGetValue(typeof(T), out var panel))
        {
            return panel as T;
        }
        throw new Exception($"Panel of type {typeof(T)} not found.");
    }

    public async Task Show<T>() where T : class, IUIPanel
    {
        var prefab = _uiMapping.GetPrefabByClassType(typeof(T));
        await Show(prefab.gameObject);
    }

    public async Task ShowDefaultScreenForScene(string sceneName = null)
    {
        Debug.Log($"Showing default screen for scene: {sceneName}");
        sceneName ??= SceneManager.GetActiveScene().name;
        var prefab = _uiMapping.GetDefaultPrefabBySceneName(sceneName);
        await Show(prefab.gameObject);
    }

    public async Task Show(GameObject prefab)
    {
        if (prefab == null)
        {
            throw new Exception("Prefab cannot be null.");
        }
        var uiPanel = prefab.GetComponent<IUIPanel>();
        var uiRoot = _model.UIRoot.Value;
        Debug.Log($"uiroot is null: {uiRoot == null}, prefab has UIPanel: {uiPanel != null}");

        if (uiRoot == null)
        {
            Debug.LogError("[UIManager] Cannot ShowView because UIRoot is null.");
            return;
        }

        // Prevent duplicate panels of the same type
        if (uiPanel != null && _model.Panels.Value.ContainsKey(uiPanel.GetType()))
        {
            Debug.LogWarning($"[UIManager] Panel of type {uiPanel.GetType().Name} is already shown. Skipping duplicate ShowView.");
            return;
        }

        // Destroy existing panels in the same layer (if not POPUP or HUD)
        if (uiPanel != null && uiPanel.Layer != UILayer.POPUP && uiPanel.Layer != UILayer.HUD &&
            _model.PanelsByLayer.Value.TryGetValue(uiPanel.Layer, out var existingPanels))
        {
            // Create a copy of the list to avoid modification during iteration
            var panelsToClose = new List<IUIPanel>(existingPanels);
            foreach (var panel in panelsToClose)
            {
                await Close(panel);
            }
        }

        var parent = uiPanel != null ? uiRoot.GetLayerParent(uiPanel.Layer) : uiRoot.transform;
        var activeScene = SceneManager.GetActiveScene();
        var containerToUse = _sceneContextRegistry.TryGetContainerForScene(activeScene) ?? _container;
        var instance = containerToUse.InstantiatePrefab(prefab, parent);
        var panelInstance = instance.GetComponent<IUIPanel>();
        panelInstance?.Show();
    }
    public async Task Close(IUIPanel panel)
    {
        panel.Hide();
        GameObject.Destroy((panel as MonoBehaviour).gameObject);
        await Task.Yield();
        // NOTE: Do not auto-show default screen here — the caller is responsible for
        // showing the next panel (e.g. via ShowScreen<T>()). The old fallback raced with
        // Start()-based RegisterPanel and caused duplicate panels to spawn.
    }

    public Task ShowPopup<T>() where T : class, IUIPanel
    {
        // For now, let's just use Show<T>(). The layer logic handles placement.
        return Show<T>();
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
        if (_model.Panels.Value.TryGetValue(typeof(LoadingScreenBlackPanel), out var panel))
        {
            await CloseView(panel);
        }
        else
        {
            // Fallback for generic UIPanel if the specialized class isn't used yet
            foreach (var p in _model.Panels.Value.Values)
            {
                if (p.Identifier == UIIdentifier.LOADING_SCREEN)
                {
                    await Close(p);
                    break;
                }
            }
        }
    }

    public async Task FadeOut()
    {
        var prefab = _uiMapping.GetPrefabByUIID(UIIdentifier.LOADING_SCREEN);
        if (prefab != null)
        {
            await Show(prefab.gameObject);
        }
        else
        {
            Debug.LogWarning("[UIManager] No prefab found for LOADING_SCREEN identifier.");
        }
    }

}

