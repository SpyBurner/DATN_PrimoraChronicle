using System;
using System.Collections.Generic;
using UnityObservables;
public interface IUIManagerModel : IModel
{
    public Observable<Dictionary<Type, IUIPanel>> Panels { get; }
    public Observable<Dictionary<UILayer, List<IUIPanel>>> PanelsByLayer { get; }
    public Observable<Stack<IUIPanel>> PopupStack { get; }
    UIPrefabRegistrySO PrefabRegistry { get; }
}