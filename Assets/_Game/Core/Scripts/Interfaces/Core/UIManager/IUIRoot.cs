using UnityEngine;

namespace Core
{
    public interface IUIRoot
    {
        Transform GetLayerParent(UILayer layer);
        GameObject gameObject { get; }
        Transform transform { get; }
    }
}
