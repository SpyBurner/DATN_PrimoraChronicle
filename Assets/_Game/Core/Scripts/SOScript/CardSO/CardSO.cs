using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core
{
    [Serializable]
    public enum CardType
    {
        None,
        Champion,
        Troop,
        Spell,
    }

    public enum CardNation
    {
        None,
        Hollow,
        Verdant,
        Ashen
    }
    public enum CardRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "CardSO", menuName = "ScriptableObjects/CardSO")]
    public class CardSO : ScriptableObject
    {
        [Header("Card Info")]
        public string ID;
        public string StringID; // Readable ID for backend sync
        public CardType CardType;
        public CardRarity Rarity;
        public string CardName;
        [TextArea(3, 10)]
        public string Description;
        public int Cost;
        public CardNation CardNation;
        public Sprite CardIllustration;

        protected void EnsureCardSetup(CardType expectedCardType)
        {
            CardType = expectedCardType;

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
