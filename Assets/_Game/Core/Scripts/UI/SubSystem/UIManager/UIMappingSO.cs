using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "UIMappingSO", menuName = "Scriptable Objects/UIMappingSO")]
    public class UIMappingSO : ScriptableObject
    {
        [Serializable]
        private class UIMapping
        {
            public UIIdentifier uiIdentifier = UIIdentifier.UNSPECIFIED;
            public UIPanel prefab = null;
        }
        [Serializable]
        private class DefaultUIMapping
        {
            public string sceneName = SceneNames.BOOTSTRAP;
            public UIIdentifier uiIdentifier = UIIdentifier.UNSPECIFIED;
        }

        [SerializeField] private List<UIMapping> uiMappings = new();
        [SerializeField] private List<DefaultUIMapping> defaultUIMappings = new();
        public UIPanel GetPrefabByUIID(UIIdentifier uiid)
        {
            foreach (var mapping in uiMappings)
            {
                if (mapping.uiIdentifier == uiid)
                {
                    return mapping.prefab;
                }
            }
            Debug.LogError("UIMappingSO: No prefab found for UIIdentifier" + uiid.ToString());
            return null;
        }
        public UIPanel GetDefaultPrefabBySceneName(string sceneName)
        {
            foreach (var mapping in defaultUIMappings)
            {
                if (mapping.sceneName == sceneName)
                {
                    return GetPrefabByUIID(mapping.uiIdentifier);
                }
            }
            Debug.LogError("UIMappingSO: No default prefab found for scene name " + sceneName);
            return null;
        }
        public UIPanel GetPrefabByClassType(Type type)
        {
            foreach (var mapping in uiMappings)
            {
                if (mapping.prefab.GetType() == type)
                {
                    return mapping.prefab;
                }
            }
            Debug.LogError("UIMappingSO: No prefab found for class type " + type.ToString());
            return null;
        }
    }
}
