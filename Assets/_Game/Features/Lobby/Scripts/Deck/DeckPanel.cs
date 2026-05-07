using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class DeckPanel : UIPanel
{
    [Inject] private readonly IDeckSubsystem _deck;
    [Inject] private readonly IUIManagerSubsystem _uiManager;
    [Inject] private readonly IDeckBuildSubsystem _deckBuild;

    [SerializeField] private GameObject[] _deckSlot = new GameObject[8];
    [SerializeField] private DeckButton _deckButtonPrefab;

    private readonly List<DeckButton> _spawnedDeckButtons = new();

    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"[DeckPanel] Awake called. _deckButtonPrefab={_deckButtonPrefab != null}, _deckSlotCount={_deckSlot?.Length}");
    }

    protected override void OnEnable()
    {
        Debug.Log("[DeckPanel] OnEnable called.");
        base.OnEnable();
        _deck.DecksChanged += RenderDecks;
        
        Debug.Log("[DeckPanel] Requesting LoadDecks from subsystem.");
        _deck.LoadDecks();
    }

    protected override void OnDisable()
    {
        Debug.Log("[DeckPanel] OnDisable called.");
        _deck.DecksChanged -= RenderDecks;
        ClearDeckButtons();
        base.OnDisable();
    }

    private void RenderDecks(IReadOnlyList<DeckSummaryData> loadedDecks)
    {
        Debug.Log($"[DeckPanel] RenderDecks called with {loadedDecks?.Count ?? 0} decks.");
        ClearDeckButtons();

        if (loadedDecks == null) 
        {
            Debug.LogWarning("[DeckPanel] loadedDecks is null.");
            return;
        }

        int deckCount = Mathf.Min(_deckSlot.Length, loadedDecks.Count);
        Debug.Log($"[DeckPanel] Spawning {deckCount} deck buttons.");

        for (int index = 0; index < deckCount; index++)
        {
            if (_deckSlot[index] == null)
            {
                Debug.LogWarning($"[DeckPanel] Slot at index {index} is null.");
                continue;
            }
            
            if (_deckButtonPrefab == null)
            {
                Debug.LogError("[DeckPanel] _deckButtonPrefab is null! Cannot spawn buttons.");
                break;
            }

            DeckSummaryData deck = loadedDecks[index];
            DeckButton deckButton = Instantiate(_deckButtonPrefab, _deckSlot[index].transform);
            
            deckButton.Initialize(deck, () => 
            {
                Debug.Log($"[DeckPanel] Deck button clicked: {deck.id}");
                _deckBuild.LoadDeck(deck.id);
                _uiManager.Show<DeckBuildPanel>();
            });
            
            deckButton.gameObject.name = string.IsNullOrWhiteSpace(deck.name)
                ? $"DeckButton_{index}"
                : deck.name;
            deckButton.gameObject.SetActive(true);
            _spawnedDeckButtons.Add(deckButton);
        }

        // Only disable the template if it was actually in the scene
        if (_deckButtonPrefab != null && _deckButtonPrefab.gameObject.scene.name != null)
        {
            _deckButtonPrefab.gameObject.SetActive(false);
        }
    }

    private void ClearDeckButtons()
    {
        foreach (DeckButton deckButton in _spawnedDeckButtons)
        {
            if (deckButton != null)
            {
                Destroy(deckButton.gameObject);
            }
        }
        _spawnedDeckButtons.Clear();
    }
}
