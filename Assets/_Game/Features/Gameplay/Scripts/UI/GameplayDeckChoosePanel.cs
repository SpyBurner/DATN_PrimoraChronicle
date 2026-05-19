using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class GameplayDeckChoosePanel : MonoBehaviour
{
    [Inject] private readonly IGameplayDeckSubsystem _deck;
    [Inject] private readonly IGameplayDeckChooseSubsystem _deckChoose;
    [Inject] private readonly IGameStateSubsystem _gameState;

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
        _gameState.PhaseTimeRemainingChanged += OnTimeRemainingChanged;

        _confirmButton?.onClick.AddListener(OnConfirmClicked);
        if (_confirmButton != null) _confirmButton.interactable = false;

        _deckSelectOverlay?.gameObject.SetActive(false);
        OnTimeRemainingChanged(_gameState.PhaseTimeRemaining);
        _deck.LoadDecks();
    }

    private void OnDisable()
    {
        _deck.DecksChanged -= OnDecksLoaded;
        _deckChoose.IsReadyChanged -= OnIsReadyChanged;
        _deckSelectOverlay.DeckSelected -= OnDeckSelected;
        _gameState.PhaseTimeRemainingChanged -= OnTimeRemainingChanged;

        _confirmButton?.onClick.RemoveListener(OnConfirmClicked);
        _hasSelection = false;
    }

    private void OnTimeRemainingChanged(float remaining)
    {
        try
        {
            if (_timerText != null)
                _timerText.text = Mathf.CeilToInt(remaining).ToString();
        }
        catch (Exception ex) { Debug.LogException(ex); }
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
        try { if (isReady) gameObject.SetActive(false); }
        catch (Exception ex) { Debug.LogException(ex); }
    }
}
