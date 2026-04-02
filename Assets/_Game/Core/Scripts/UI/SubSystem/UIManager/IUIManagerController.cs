using System;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public interface IUIManagerController : IController
{
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