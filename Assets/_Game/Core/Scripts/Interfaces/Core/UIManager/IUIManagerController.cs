using System;
using System.Threading.Tasks;
using Core;
using UnityEngine;
using Zenject;

public interface IUIManagerController : IController
{
    void RegisterPanel(IUIPanel panel);
    void UnregisterPanel(IUIPanel panel);
    void RegisterUIRoot(IUIRoot uIRoot);
    void UnregisterUIRoot();
    IUIRoot GetUIRoot();
    T GetPanel<T>() where T : class, IUIPanel;
    Task Show<T>() where T : class, IUIPanel;
    Task ShowDefaultScreenForScene(string sceneName = null);
    Task Show(GameObject prefab);
    Task Close(IUIPanel panel);
    Task ClosePopup();
    Task FadeIn();
    Task FadeOut();
}