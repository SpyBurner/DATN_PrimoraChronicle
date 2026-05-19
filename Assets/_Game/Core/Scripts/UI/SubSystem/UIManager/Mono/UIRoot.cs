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

    public class UIRoot : MonoBehaviour, IUIRoot
    {
        [Inject] private readonly IDebugLogger _debugLogger;
        [Inject] private readonly IUIManagerSubsystem _uiManagerSubsystem;
        [SerializeField] private List<UILayerParentEntry> _layerParents = new();

        private void Awake()
        {
            _debugLogger.Log($"[UIRoot] Awake called. _uiManagerSubsystem is null: {_uiManagerSubsystem == null}");
            if (_uiManagerSubsystem == null)
            {
                _debugLogger.LogError("[UIRoot] _uiManagerSubsystem is null — missing SceneContext in this scene or UIRoot was not in the scene at load time.");
                return;
            }
            _debugLogger.Log("[UIRoot] RegisterUIRoot called successfully.");
            _uiManagerSubsystem.RegisterUIRoot(this);
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
            throw new System.Exception($"No parent found for UILayer {layer}. Please check the UIRoot hierarchy.");
        }
    }
}
