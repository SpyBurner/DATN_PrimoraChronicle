using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIPrefabRegistrySO", menuName = "ScriptableObjects/UIPrefabRegistrySO", order = 2)]
public class UIPrefabRegistrySO : ScriptableObject
{
    [Serializable]
    public class UIPrefabMapping
    {
        public UIIdentifier UIIdentifier = UIIdentifier.UNSPECIFIED;
        public GameObject Prefab;
    }

    public List<UIPrefabMapping> uiPrefabMappings;

    public GameObject GetPrefab(UIIdentifier uiid)
    {
        foreach (var mapping in uiPrefabMappings)
        {
            if (mapping.UIIdentifier == uiid)
            {
                return mapping.Prefab;
            }
        }
        Debug.LogError("UIPrefabRegistrySO: No prefab found for UIIdentifier " + uiid.ToString());
        return null;
    }
}
