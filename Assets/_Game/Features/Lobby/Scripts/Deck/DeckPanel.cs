using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using Zenject;

public class DeckPanel : UIPanel
{
    [Inject] private readonly IDeckSubsystem _deck;

    [SerializeField] private GameObject[] _deckSlot = new GameObject[8];
    [SerializeField] private DeckButton _deckButtonPrefab;

    private readonly List<DeckButton> _spawnedDeckButtons = new();


    protected override void OnEnable()
    {
        base.OnEnable();
        RenderDecks(_deck.LoadDecks());
    }

    protected override void OnDisable()
    {
        ClearDeckButtons();
        base.OnDisable();
    }

    private void RenderDecks(IReadOnlyList<DeckSO> loadedDecks)
    {
        ClearDeckButtons();

        int deckCount = Mathf.Min(_deckSlot.Length, loadedDecks.Count);

        for (int index = 0; index < deckCount; index++)
        {
            if (_deckSlot[index] == null || _deckButtonPrefab == null)
            {
                continue;
            }

            DeckSO deckSO = loadedDecks[index];
            DeckButton deckButton = Instantiate(_deckButtonPrefab, _deckSlot[index].transform);
            deckButton.Initialize(deckSO, () => _deck.EditDeck(deckSO));
            deckButton.gameObject.name = string.IsNullOrWhiteSpace(loadedDecks[index].DeckName)
                ? $"DeckButton_{index}"
                : loadedDecks[index].DeckName;
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
