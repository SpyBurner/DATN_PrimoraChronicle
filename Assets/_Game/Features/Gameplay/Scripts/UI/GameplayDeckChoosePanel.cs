using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// StartPhase deck-choose panel.
/// Shows the currently selected deck via a single DeckButton; clicking it opens
/// GameplayDeckSelectOverlay so the player can pick a different deck.
/// Auto-selects the first deck returned by the API so the confirm button is always ready.
/// Hides itself when IsReady becomes true (both players confirmed).
/// </summary>
public class GameplayDeckChoosePanel : MonoBehaviour
{
    [Inject] private readonly IGameplayDeckSubsystem _deck;
    [Inject] private readonly IGameplayDeckChooseSubsystem _deckChoose;

    [Header("References")]
    [SerializeField] private DeckButton _currentDeckButton;
    [SerializeField] private GameplayDeckSelectOverlay _deckSelectOverlay;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private Button _confirmButton;

    private bool _hasSelection;

    private void OnEnable()
    {
        _deck.DecksChanged += OnDecksLoaded;
        _deckChoose.IsReadyChanged += OnIsReadyChanged;
        _deckSelectOverlay.DeckSelected += OnDeckSelected;

        _confirmButton?.onClick.AddListener(OnConfirmClicked);
        if (_confirmButton != null) _confirmButton.interactable = false;

        _deckSelectOverlay?.gameObject.SetActive(false);
        _deck.LoadDecks();
    }

    private void OnDisable()
    {
        _deck.DecksChanged -= OnDecksLoaded;
        _deckChoose.IsReadyChanged -= OnIsReadyChanged;
        _deckSelectOverlay.DeckSelected -= OnDeckSelected;
        _confirmButton?.onClick.RemoveListener(OnConfirmClicked);
        _hasSelection = false;
    }

    private void Update()
    {
        if (_timerText == null || NetworkGameplayManager.Instance == null) return;
        var runner = NetworkGameplayManager.Instance.Runner;
        if (runner == null) return;
        float? remaining = NetworkGameplayManager.Instance.PhaseTimer.RemainingTime(runner);
        _timerText.text = remaining.HasValue ? Mathf.CeilToInt(remaining.Value).ToString() : "--";
    }

    private void OnDecksLoaded(IReadOnlyList<DeckSummaryData> decks)
    {
        if (!_hasSelection && decks != null && decks.Count > 0)
            SelectDeck(decks[0]);
    }

    private void OnDeckSelected(DeckSummaryData summary) => SelectDeck(summary);

    private void SelectDeck(DeckSummaryData summary)
    {
        _hasSelection = true;
        _deckChoose.StageSelection(summary);
        _currentDeckButton?.Initialize(summary, OpenDeckOverlay);
        if (_confirmButton != null) _confirmButton.interactable = true;
    }

    private void OpenDeckOverlay() => _deckSelectOverlay?.gameObject.SetActive(true);

    private async void OnConfirmClicked()
    {
        if (!_hasSelection) return;
        if (_confirmButton != null) _confirmButton.interactable = false;
        await _deckChoose.ConfirmSelection();
    }

    private void OnIsReadyChanged(bool isReady)
    {
        if (isReady) gameObject.SetActive(false);
    }
}
