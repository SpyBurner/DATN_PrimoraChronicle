using System.Threading.Tasks;
using UnityEngine;
using Core;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace Core.Tests
{
    /// <summary>
    /// Mock implementation of IHttpServiceSubsystem for testing other subsystems.
    /// </summary>
    public class MockHttpServiceSubsystem : IHttpServiceSubsystem
    {
        public event UnityAction<int> RequestQueueCountChanged;
        public event UnityAction<bool> IsRequestingChanged;

        public Task<T> Get<T>(string url) => Task.FromResult(default(T));
        public Task<T> Post<T, TRequest>(string url, TRequest payload) where TRequest : class => Task.FromResult(default(T));
        public Task<string> Get(string url) => Task.FromResult("");
        public Task<string> Post<TRequest>(string url, TRequest payload) where TRequest : class => Task.FromResult("");
        public void SetAuthToken(string token) { }

        public void Initialize() { }
        public void Dispose() { }
    }

    /// <summary>
    /// Mock implementation of IUIManagerSubsystem for testing other subsystems.
    /// </summary>
    public class MockUIManagerSubsystem : IUIManagerSubsystem
    {
        public event UnityAction<Dictionary<Type, IUIPanel>> PanelsChanged;
        public event UnityAction<Dictionary<UILayer, List<IUIPanel>>> PanelsByLayerChanged;
        public event UnityAction<Stack<IUIPanel>> PopupStackChanged;

        public int TotalPanelCount => 0;

        public void RegisterPanel(IUIPanel panel) { }
        public void UnregisterPanel(IUIPanel panel) { }
        public void RegisterUIRoot(UIRoot uIRoot) { }
        public void UnregisterUIRoot() { }
        public UIRoot GetUIRoot() => null;
        public T GetPanel<T>() where T : class, IUIPanel => null;
        public Task Show<T>() where T : class, IUIPanel => Task.CompletedTask;
        public Task Show(GameObject prefab) => Task.CompletedTask;
        public Task ShowDefaultScreenForScene(string sceneName = null) => Task.CompletedTask;
        public Task Close(IUIPanel panel) => Task.CompletedTask;
        public Task ClosePopup() => Task.CompletedTask;
        public Task FadeIn() => Task.CompletedTask;
        public Task FadeOut() => Task.CompletedTask;

        public void Initialize() { }
        public void Dispose() { }
    }

    /// <summary>
    /// Mock panel for testing UI operations.
    /// </summary>
    public class MockPanel : MonoBehaviour, IUIPanel
    {
        public UIIdentifier Identifier => UIIdentifier.UNSPECIFIED;
        public UILayer Layer => UILayer.SCREEN;

        public void Hide() => gameObject.SetActive(false);
        public void Show() => gameObject.SetActive(true);
    }
}
