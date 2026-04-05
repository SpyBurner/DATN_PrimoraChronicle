using System;
using System.Threading.Tasks;
using Core;
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
}