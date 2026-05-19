using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// Overlay panel that lists the player's decks so they can choose one during StartPhase.
/// Clicking a deck fires DeckSelected and closes the overlay.
/// Mirrors the Lobby DeckPanel layout (8 slots) but without DeckBuild navigation.
/// </summary>
public class GameplayDeckSelectOverlay : MonoBehaviour
{
    [Inject] private readonly IGameplayDeckSubsystem _deck;

    [SerializeField] private GameObject[] _deckSlot = new GameObject[8];
    [SerializeField] private DeckButton _deckButtonPrefab;

    public event Action<DeckSummaryData> DeckSelected;

    private readonly List<DeckButton> _spawnedButtons = new();

    private void OnEnable()
    {
        _deck.DecksChanged += RenderDecks;
        _deck.LoadDecks();
    }

    private void OnDisable()
    {
        _deck.DecksChanged -= RenderDecks;
        ClearButtons();
    }

    private void RenderDecks(IReadOnlyList<DeckSummaryData> decks)
    {
        ClearButtons();
        if (decks == null) return;

        int count = Mathf.Min(_deckSlot.Length, decks.Count);
        for (int i = 0; i < count; i++)
        {
            if (_deckSlot[i] == null) continue;
            ClearSlotButton(_deckSlot[i]);

            var summary = decks[i];
            var btn = Instantiate(_deckButtonPrefab, _deckSlot[i].transform);
            btn.Initialize(summary, () => OnDeckClicked(summary));
            btn.gameObject.SetActive(true);
            _spawnedButtons.Add(btn);
        }

        for (int i = decks.Count; i < _deckSlot.Length; i++)
        {
            if (_deckSlot[i] == null) continue;
            var slotBtn = _deckSlot[i].GetComponent<Button>();
            if (slotBtn != null) slotBtn.interactable = false;
        }
    }

    private void OnDeckClicked(DeckSummaryData summary)
    {
        try { DeckSelected?.Invoke(summary); }
        catch (Exception ex) { Debug.LogException(ex); }
        gameObject.SetActive(false);
    }

    private void ClearSlotButton(GameObject slot)
    {
        var btn = slot.GetComponent<Button>();
        btn?.onClick.RemoveAllListeners();
    }

    private void ClearButtons()
    {
        foreach (var btn in _spawnedButtons)
            if (btn != null) Destroy(btn.gameObject);
        _spawnedButtons.Clear();
    }
}
