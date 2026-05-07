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

    protected override void OnEnable()
    {
        base.OnEnable();
        _deck.DecksChanged += RenderDecks;
        _deck.LoadDecks();
    }

    protected override void OnDisable()
    {
        _deck.DecksChanged -= RenderDecks;
        ClearDeckButtons();
        base.OnDisable();
    }

    private void RenderDecks(IReadOnlyList<DeckSummaryData> loadedDecks)
    {
        ClearDeckButtons();

        if (loadedDecks == null) return;

        int deckCount = Mathf.Min(_deckSlot.Length, loadedDecks.Count);

        for (int index = 0; index < deckCount; index++)
        {
            if (_deckSlot[index] == null || _deckButtonPrefab == null)
            {
                continue;
            }

            DeckSummaryData deck = loadedDecks[index];
            DeckButton deckButton = Instantiate(_deckButtonPrefab, _deckSlot[index].transform);
            
            deckButton.Initialize(deck, () => 
            {
                _deckBuild.LoadDeck(deck.id);
                _uiManager.ShowScreen<DeckBuildPanel>();
            });
            
            deckButton.gameObject.name = string.IsNullOrWhiteSpace(deck.name)
                ? $"DeckButton_{index}"
                : deck.name;
            deckButton.gameObject.SetActive(true);
            _spawnedDeckButtons.Add(deckButton);
        }

        _deckButtonPrefab.gameObject.SetActive(false);
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
