using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// StartPhase deck-choose panel.
/// Reuses IDeckSubsystem (from Lobby) to load and display the player's decks.
/// Reuses DeckButton prefab for the list items.
/// Confirms selection through IGameplayDeckChooseSubsystem → network bridge.
/// </summary>
public class GameplayDeckChoosePanel : MonoBehaviour
{
    [Inject] private readonly IDeckSubsystem _deck;
    [Inject] private readonly IGameplayDeckChooseSubsystem _deckChoose;

    [Header("References")]
    [SerializeField] private Transform _deckListContainer;
    [SerializeField] private DeckButton _deckButtonPrefab;
    [SerializeField] private Button _confirmButton;

    private readonly List<DeckButton> _spawnedButtons = new();
    private DeckSummaryData _pendingSelection;
    private bool _hasSelection;

    private void OnEnable()
    {
        _deck.DecksChanged += RenderDecks;
        _deckChoose.IsReadyChanged += OnIsReadyChanged;

        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(OnConfirmClicked);
            _confirmButton.interactable = false;
        }

        _deck.LoadDecks();
    }

    private void OnDisable()
    {
        _deck.DecksChanged -= RenderDecks;
        _deckChoose.IsReadyChanged -= OnIsReadyChanged;

        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(OnConfirmClicked);

        ClearButtons();
        _hasSelection = false;
    }

    private void RenderDecks(IReadOnlyList<DeckSummaryData> decks)
    {
        ClearButtons();

        if (decks == null) return;

        foreach (var summary in decks)
        {
            var captured = summary;
            var btn = Instantiate(_deckButtonPrefab, _deckListContainer);
            btn.Initialize(captured, () => OnDeckSelected(captured));
            btn.gameObject.SetActive(true);
            _spawnedButtons.Add(btn);
        }
    }

    private void OnDeckSelected(DeckSummaryData summary)
    {
        _pendingSelection = summary;
        _hasSelection = true;
        _deckChoose.StageSelection(summary);

        if (_confirmButton != null)
            _confirmButton.interactable = true;
    }

    private async void OnConfirmClicked()
    {
        if (!_hasSelection) return;

        if (_confirmButton != null)
            _confirmButton.interactable = false;

        await _deckChoose.ConfirmSelection();
    }

    private void OnIsReadyChanged(bool isReady)
    {
        if (isReady)
            gameObject.SetActive(false);
    }

    private void ClearButtons()
    {
        foreach (var btn in _spawnedButtons)
            if (btn != null) Destroy(btn.gameObject);
        _spawnedButtons.Clear();
    }
}
