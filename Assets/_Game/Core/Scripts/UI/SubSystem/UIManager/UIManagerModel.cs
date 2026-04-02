using System;
using System.Collections.Generic;
using UnityObservables;
using Zenject;

internal class UIManagerModel : IUIManagerModel
{
    [Inject] private readonly UIPrefabRegistrySO _prefabRegistry;

    private Observable<Dictionary<Type, IUIPanel>> _panels = new(new());
    private Observable<Dictionary<UILayer, List<IUIPanel>>> _panelsByLayer = new(new());
    private Observable<Stack<IUIPanel>> _popupStack = new(new());

    public Observable<Dictionary<Type, IUIPanel>> Panels { get => _panels; }
    public Observable<Dictionary<UILayer, List<IUIPanel>>> PanelsByLayer { get => _panelsByLayer; }
    public Observable<Stack<IUIPanel>> PopupStack { get => _popupStack; }
    public UIPrefabRegistrySO PrefabRegistry => _prefabRegistry;

    // Zenject will call this when bound with BindInterfacesAndSelfTo<UIManagerModel>().AsSingle()
    public void Initialize()
    {
    }

    public void Dispose()
    {
        // Defensive null checks and safe disposal
        _panels?.Value?.Clear();
        _panelsByLayer?.Value?.Clear();
        _popupStack?.Value?.Clear();

        // Optionally release references
        _panels = null;
        _panelsByLayer = null;
        _popupStack = null;
    }
}