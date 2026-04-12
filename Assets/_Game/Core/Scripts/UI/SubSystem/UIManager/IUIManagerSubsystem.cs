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

    // Controller Methods (forwarded by the subsystem)
    void RegisterPanel(IUIPanel panel);
    void UnregisterPanel(IUIPanel panel);
    void RegisterUIRoot(UIRoot uIRoot);
    void UnregisterUIRoot();

    UIRoot GetUIRoot();

    T GetPanel<T>() where T : class, IUIPanel;
    Task ShowView(GameObject prefab);
    Task CloseView(IUIPanel panel);
    Task ShowScreen<T>() where T : class, IUIPanel;
    Task ShowDefaultScreenForScene(string sceneName = null);
    Task ShowPopup<T>() where T : class, IUIPanel;
    Task ClosePopup();
    Task FadeIn();
    Task FadeOut();
}