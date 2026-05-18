using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using UnityEngine;
using UnityEngine.Events;

public interface IUIManagerSubsystem : ISubsystem
{
    // Model change notifications exposed as UnityAction events (only notifications are exposed)
    event UnityAction<Dictionary<Type, IUIPanel>> PanelsChanged;
    event UnityAction<Dictionary<UILayer, List<IUIPanel>>> PanelsByLayerChanged;
    event UnityAction<Stack<IUIPanel>> PopupStackChanged;
    event UnityAction<Core.IUIRoot> UIRootChanged;
    
    int TotalPanelCount { get; }

    // Controller Methods (forwarded by the subsystem forwarders)
    void RegisterPanel(IUIPanel panel);
    void UnregisterPanel(IUIPanel panel);
    void RegisterUIRoot(IUIRoot uIRoot);
    void UnregisterUIRoot();

    IUIRoot GetUIRoot();

    T GetPanel<T>() where T : class, IUIPanel;
    Task Show<T>() where T : class, IUIPanel;
    Task Show(GameObject prefab);
    Task ShowDefaultScreenForScene(string sceneName = null);
    Task Close(IUIPanel panel);
    Task ClosePopup();
    Task FadeIn();
    Task FadeOut();
}