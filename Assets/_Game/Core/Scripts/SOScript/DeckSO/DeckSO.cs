using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "DeckSO", menuName = "ScriptableObjects/DeckSO")]
    public class DeckSO : ScriptableObject
    {
        private const int CardSlotCount = 20;
        private const string DeckListFolderName = "DeckList";

        public int DeckID;
        public string DeckName;
        public string ChampionCardID;
        public List<string> TroopCardsID = new(CardSlotCount);
        public List<string> SpellCardsID = new(CardSlotCount);

        [Serializable]
        private class DeckData
        {
            public int DeckID;
            public string DeckName;
            public string ChampionCardID;
            public List<string> TroopCardsID;
            public List<string> SpellCardsID;
            public string[] CardsID;
        }

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(ToDeckData(), prettyPrint);
        }

        public static string GetDeckListDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, DeckListFolderName);
        }

        public bool LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Deck json cannot be null or empty.", nameof(json));
            }

            DeckData deckData = JsonUtility.FromJson<DeckData>(json);
            if (deckData == null)
            {
                throw new InvalidOperationException("Failed to deserialize deck json.");
            }

            return ApplyDeckData(deckData);
        }

        public string SaveToJsonFile(bool prettyPrint = true)
        {
            if (string.IsNullOrWhiteSpace(DeckName))
            {
                throw new InvalidOperationException("DeckName cannot be null or empty when saving a deck.");
            }

            string directoryPath = GetDeckListDirectoryPath();
            Directory.CreateDirectory(directoryPath);

            string filePath = Path.Combine(directoryPath, GetDeckFileName());

            File.WriteAllText(filePath, ToJson(prettyPrint));
            return filePath;
        }

        private DeckData ToDeckData()
        {
            return new DeckData
            {
                DeckID = DeckID,
                DeckName = DeckName,
                ChampionCardID = ChampionCardID,
                TroopCardsID = NormalizeCards(TroopCardsID),
                SpellCardsID = NormalizeCards(SpellCardsID),
            };
        }

        private bool ApplyDeckData(DeckData deckData)
        {
            DeckID = deckData.DeckID;
            DeckName = deckData.DeckName;
            ChampionCardID = deckData.ChampionCardID;

            if ((deckData.TroopCardsID != null && deckData.TroopCardsID.Count > 0)
                || (deckData.SpellCardsID != null && deckData.SpellCardsID.Count > 0)
                || deckData.CardsID == null)
            {
                TroopCardsID = NormalizeCards(deckData.TroopCardsID);
                SpellCardsID = NormalizeCards(deckData.SpellCardsID);
                return false;
            }

            SplitLegacyCards(deckData.CardsID, out List<string> troopCardsId, out List<string> spellCardsId);
            TroopCardsID = troopCardsId;
            SpellCardsID = spellCardsId;
            return true;
        }

        private static List<string> NormalizeCards(IEnumerable<string> source)
        {
            List<string> normalizedCards = new(CardSlotCount);
            if (source == null)
            {
                return normalizedCards;
            }

            foreach (string cardId in source)
            {
                if (string.IsNullOrWhiteSpace(cardId))
                {
                    continue;
                }

                normalizedCards.Add(cardId);
            }

            return normalizedCards;
        }

        private static void SplitLegacyCards(IEnumerable<string> source, out List<string> troopCardsId, out List<string> spellCardsId)
        {
            troopCardsId = new List<string>(CardSlotCount);
            spellCardsId = new List<string>(CardSlotCount);

            if (source == null)
            {
                return;
            }

            foreach (string cardId in source)
            {
                if (string.IsNullOrWhiteSpace(cardId))
                {
                    continue;
                }

                if (cardId.StartsWith("S", StringComparison.OrdinalIgnoreCase))
                {
                    spellCardsId.Add(cardId);
                    continue;
                }

                troopCardsId.Add(cardId);
            }
        }

        private string GetDeckFileName()
        {
            string sanitizedDeckName = DeckName;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                sanitizedDeckName = sanitizedDeckName.Replace(invalidChar, '_');
            }

            return sanitizedDeckName + ".json";
        }
    }
}
