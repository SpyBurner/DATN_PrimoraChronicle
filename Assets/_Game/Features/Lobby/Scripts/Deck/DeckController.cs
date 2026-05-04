using System;
using System.Collections.Generic;
using System.IO;
using Core;
using UnityEngine;
using Zenject;

internal class DeckController : IDeckController
{
    [Inject] private readonly IDeckModel _model;
    [Inject] private readonly IDeckEditSubsystem _deckEdit;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    public void Initialize() { }
    public void Dispose() { }

    public void EditDeck(DeckSO deckSO)
    {
        _deckEdit.StoreSelectedDeck(deckSO);
        _uiManager.CloseView(_uiManager.GetPanel<DeckPanel>());
        _uiManager.ShowScreen<DeckEditPanel>();
    }

    public IReadOnlyList<DeckSO> LoadDecks()
    {
        List<DeckSO> loadedDecks = new();
        string deckDirectoryPath = DeckSO.GetDeckListDirectoryPath();

        if (Directory.Exists(deckDirectoryPath))
        {
            string[] deckFilePaths = Directory.GetFiles(deckDirectoryPath, "*.json");
            Array.Sort(deckFilePaths, StringComparer.OrdinalIgnoreCase);

            foreach (string deckFilePath in deckFilePaths)
            {
                try
                {
                    string deckJson = File.ReadAllText(deckFilePath);
                    DeckSO deck = ScriptableObject.CreateInstance<DeckSO>();
                    bool migratedLegacyStructure = deck.LoadFromJson(deckJson);

                    if (migratedLegacyStructure)
                    {
                        File.WriteAllText(deckFilePath, deck.ToJson());
                    }

                    loadedDecks.Add(deck);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to load deck from '{deckFilePath}'. {exception.Message}");
                }
            }
        }

        _model.SetDecks(loadedDecks);
        return _model.Decks;
    }
}
