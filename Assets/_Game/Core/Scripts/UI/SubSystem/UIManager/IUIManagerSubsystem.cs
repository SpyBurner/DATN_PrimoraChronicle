using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    T GetPanel<T>() where T : class, IUIPanel;
    GameObject GetPrefab(UIIdentifier uiid);
    Task ShowScreen<T>() where T : class, IUIPanel;
    Task ShowPopup<T>() where T : class, IUIPanel;
    Task ClosePopup();
    Task FadeIn();
    Task FadeOut();
}