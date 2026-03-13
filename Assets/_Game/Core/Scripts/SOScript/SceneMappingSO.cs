using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneMappingSO", menuName = "ScriptableObjects/SceneMappingSO", order = 1)]
public class SceneMappingSO : ScriptableObject
{
    [Serializable]
    public class SceneMapping
    {
        public string sceneName = string.Empty;
        public UIIdentifier UIIdentifier = UIIdentifier.UNSPECIFIED;
    }
    public List<SceneMapping> sceneMappings;

    public string GetSceneNameByUIID(UIIdentifier uiid)
    {
        foreach (var mapping in sceneMappings)
        {
            if (mapping.UIIdentifier == uiid)
            {
                return mapping.sceneName;
            }
        }
        Debug.LogError("SceneMappingSO: No scene found for UIIdentifier" + uiid.ToString());
        return null;
    }
}