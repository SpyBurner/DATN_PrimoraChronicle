using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core;
using UnityEngine;
using Zenject;

internal class DeckController : IDeckController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IDeckModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;

    public void Initialize() { }
    public void Dispose() { }

    public async Task LoadDecks()
    {
        try
        {
            _debugLogger.Log("Deck: Loading decks from server");
            var response = await _httpService.Get<DecksListResponse>("https://api.example.com/user/decks");

            if (response != null && response.decks != null)
            {
                List<DeckSO> decks = new();
                foreach (var deckData in response.decks)
                {
                    DeckSO deck = ScriptableObject.CreateInstance<DeckSO>();
                    // Populate deck from deckData (simplified)
                    decks.Add(deck);
                }
                _model.SetDecks(decks);
                _debugLogger.Log($"Deck: Loaded {decks.Count} decks");
            }
            else
            {
                _debugLogger.LogError("Deck: Failed to load decks");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"Deck: LoadDecks failed: {ex.Message}");
        }
    }

    public void SelectDeck(DeckSO deckSO)
    {
        _debugLogger.Log($"Deck: Selected deck {deckSO.Name}");
    }
}

[System.Serializable]
internal class DeckData
{
    public string id;
    public string name;
    public string[] cards;
}

[System.Serializable]
internal class DecksListResponse
{
    public DeckData[] decks;
}

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
