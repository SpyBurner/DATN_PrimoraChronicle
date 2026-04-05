using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Core
{
    [System.Serializable]
    public class UILayerParentEntry
    {
        public UILayer Layer;
        public Transform Parent;
    }

    public class UIRoot : MonoBehaviour
    {
        [Inject] private readonly IUIManagerSubsystem _uiManagerSubsystem;
        [SerializeField] private List<UILayerParentEntry> _layerParents = new();

        private void Awake()
        {
            Debug.Log($"[UIRoot] Awake called. _uiManagerSubsystem is null: {_uiManagerSubsystem == null}");
            if (_uiManagerSubsystem == null)
            {
                Debug.LogError("[UIRoot] _uiManagerSubsystem is null — missing SceneContext in this scene or UIRoot was not in the scene at load time.");
                return;
            }
            _uiManagerSubsystem.RegisterUIRoot(this);
            Debug.Log("[UIRoot] RegisterUIRoot called successfully.");
        }

        private void OnDestroy()
        {
            _uiManagerSubsystem.UnregisterUIRoot();
        }

        public Transform GetLayerParent(UILayer layer)
        {
            foreach (var entry in _layerParents)
                if (entry.Layer == layer && entry.Parent != null)
                    return entry.Parent;
            return transform;
        }
    }
}
