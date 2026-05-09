using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core
{
    public enum CardType
    {
        None,
        Champion,
        Troop,
        MainPhaseSpell,
        EquipSpell,
    }

    public abstract class CardSO : ScriptableObject
    {
        [Header("GDS Identification")]
        public string StringID; // Universal bridge to GDS JSON

        [Header("Visual Assets")]
        public Sprite CardIllustration;

        protected void EnsureCardSetup()
        {
#if UNITY_EDITOR
            string assetGuid = GetAssetGuid();
            if (!string.IsNullOrWhiteSpace(assetGuid) && ID != assetGuid)
            {
                ID = assetGuid;
                EditorUtility.SetDirty(this);
            }
#endif
        }

#if UNITY_EDITOR
        private string GetAssetGuid()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return string.Empty;
            }

            return AssetDatabase.AssetPathToGUID(assetPath);
        }
#endif
    }
}
