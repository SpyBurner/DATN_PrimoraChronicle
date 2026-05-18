using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class UIManagerController : IUIManagerController
{
    [Inject] private readonly IUIManagerModel _model;
    [Inject] private readonly UIMappingSO _uiMapping;
    [Inject] private readonly DiContainer _container;
    [Inject] private readonly SceneContextRegistry _sceneContextRegistry;

    private bool _isInternalOperation = false;

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

        Debug.Log($"[UIManager] Registered panel {type.Name}. Total panels: {_model.Panels.Value.Count}");
    }

    public void UnregisterPanel(IUIPanel panel)
    {
        var type = panel.GetType();
        try {
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
                Debug.Log($"[UIManager] Unregistered panel {type.Name}. Total panels: {_model.Panels.Value.Count}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UIManager] Error unregistering panel {type.Name}: {e.Message}");
        }
    }

    public void RegisterUIRoot(IUIRoot uIRoot)
    {
        if (_model.UIRoot.Value != null)
        {
            UnregisterUIRoot();
        }
        _model.UIRoot.Value = uIRoot;
    }

    public void UnregisterUIRoot()
    {
        _model.UIRoot.Value = null;
    }

    public IUIRoot GetUIRoot() => _model.UIRoot.Value;

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
        if (prefab == null)
        {
            Debug.LogError($"[UIManager] No prefab found in UIMappingSO for class type {typeof(T).Name}");
            return;
        }
        await Show(prefab.gameObject);
    }

    public async Task ShowDefaultScreenForScene(string sceneName = null)
    {
        sceneName ??= SceneManager.GetActiveScene().name;
        Debug.Log($"[UIManager] Showing default screen for scene: {sceneName}");
        var prefab = _uiMapping.GetDefaultPrefabBySceneName(sceneName);
        if (prefab != null)
        {
            await Show(prefab.gameObject);
        }
        else
        {
            Debug.LogWarning($"[UIManager] No default UI mapping found for scene '{sceneName}'");
        }
    }

    public async Task Show(GameObject prefab)
    {
        if (prefab == null)
        {
            throw new Exception("Prefab cannot be null.");
        }

        _isInternalOperation = true;
        try
        {
            var uiPanel = prefab.GetComponent<IUIPanel>();
            var uiRoot = _model.UIRoot.Value;

            if (uiRoot == null)
            {
                Debug.LogError("[UIManager] Cannot ShowView because UIRoot is null in current scene.");
                return;
            }

            // Prevent duplicate panels of the same type
            if (uiPanel != null && _model.Panels.Value.TryGetValue(uiPanel.GetType(), out var existingPanel))
            {
                Debug.LogWarning($"[UIManager] Panel of type {uiPanel.GetType().Name} is already registered. Ensuring it is shown.");
                existingPanel.Show();
                return;
            }

            // Close existing panels in the same layer (if not POPUP or HUD)
            if (uiPanel != null && uiPanel.Layer != UILayer.POPUP && uiPanel.Layer != UILayer.HUD &&
                _model.PanelsByLayer.Value.TryGetValue(uiPanel.Layer, out var existingPanels))
            {
                var panelsToClose = new List<IUIPanel>(existingPanels);
                foreach (var panel in panelsToClose)
                {
                    await Close(panel);
                }
            }

            var parent = uiPanel != null ? uiRoot.GetLayerParent(uiPanel.Layer) : uiRoot.transform;
            var activeScene = SceneManager.GetActiveScene();
            var containerToUse = _sceneContextRegistry.TryGetContainerForScene(activeScene) ?? _container;

            Debug.Log($"[UIManager] Instantiating prefab '{prefab.name}' for scene '{activeScene.name}' (Container: {(containerToUse == _container ? "Global" : "Scene")})");

            var instance = containerToUse.InstantiatePrefab(prefab, parent);
            instance.SetActive(true); // Ensure instance is active

            var panelInstance = instance.GetComponent<IUIPanel>();
            if (panelInstance != null)
            {
                Debug.Log($"[UIManager] Calling Show() on panel '{panelInstance.GetType().Name}'");
                panelInstance.Show();
                Debug.Log($"[UIManager] {panelInstance.GetType().Name} state: activeSelf={instance.activeSelf}, activeInHierarchy={instance.activeInHierarchy}");
            }
            else
            {
                Debug.LogError($"[UIManager] Prefab '{prefab.name}' does not have an IUIPanel component on its root!");
            }
        }
        finally
        {
            _isInternalOperation = false;
        }
    }

    public async Task Close(IUIPanel panel)
    {
        if (panel == null) return;

        var typeName = panel.GetType().Name;
        panel.Hide();
        var go = (panel as MonoBehaviour).gameObject;

        // Immediately unregister to reflect accurate count for auto-show logic
        UnregisterPanel(panel);

        GameObject.Destroy(go);
        await Task.Yield();

        // If this wasn't part of an internal transition and no UI is left, show default
        if (!_isInternalOperation && _model.Panels.Value.Count == 0)
        {
            Debug.Log($"[UIManager] Last panel '{typeName}' closed and no UI panels left. Showing default screen for current scene.");
            await ShowDefaultScreenForScene();
        }
    }

    public Task ShowPopup<T>() where T : class, IUIPanel => Show<T>();

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
        _isInternalOperation = true;
        try
        {
            if (_model.Panels.Value.TryGetValue(typeof(LoadingScreenBlackPanel), out var panel))
            {
                await Close(panel);
            }
            else
            {
                foreach (var p in new List<IUIPanel>(_model.Panels.Value.Values))
                {
                    if (p.Identifier == UIIdentifier.LOADING_SCREEN)
                    {
                        await Close(p);
                        break;
                    }
                }
            }
        }
        finally
        {
            _isInternalOperation = false;
        }
    }

    public async Task FadeOut()
    {
        _isInternalOperation = true;
        try
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
        finally
        {
            _isInternalOperation = false;
        }
    }
}
