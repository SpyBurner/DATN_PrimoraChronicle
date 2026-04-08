using System;
using System.Threading.Tasks;
using Core;
using UnityEngine;
using Zenject;

public interface IUIManagerController : IController
{
    void RegisterPanel(IUIPanel panel);
    void UnregisterPanel(IUIPanel panel);
    void RegisterUIRoot(UIRoot uIRoot);
    void UnregisterUIRoot();
    UIRoot GetUIRoot();
    T GetPanel<T>() where T : class, IUIPanel;
    Task ShowScreen<T>() where T : class, IUIPanel;
    Task ShowDefaultScreenForScene(string sceneName = null);
    Task ShowPopup<T>() where T : class, IUIPanel;
    Task ClosePopup();
    Task FadeIn();
    Task FadeOut();
    Task ShowView(GameObject prefab);
    Task CloseView(IUIPanel panel);
}