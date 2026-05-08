using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
        ConfigureEmptySlotButtons(loadedDecks?.Count ?? 0);

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
            ClearSlotButton(_deckSlot[index]);

            deckButton.Initialize(deck, async () =>
            {
                Debug.Log($"[DeckPanel] Deck button clicked: {deck.id}");
                await _deckBuild.LoadDeck(deck.id);
                await OpenDeckBuildPanel();
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

    private void ConfigureEmptySlotButtons(int filledSlotCount)
    {
        for (int index = 0; index < _deckSlot.Length; index++)
        {
            GameObject slot = _deckSlot[index];
            if (slot == null)
            {
                continue;
            }

            if (index < filledSlotCount)
            {
                ClearSlotButton(slot);
                continue;
            }

            Button slotButton = slot.GetComponent<Button>();
            if (slotButton == null)
            {
                continue;
            }

            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OpenEmptyDeckBuilder);
        }
    }

    private void ClearSlotButton(GameObject slot)
    {
        if (slot == null)
        {
            return;
        }

        Button slotButton = slot.GetComponent<Button>();
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
        }
    }

    private async void OpenEmptyDeckBuilder()
    {
        Debug.Log("[DeckPanel] Empty deck slot clicked.");
        await _deckBuild.CreateEmptyDeck();
        await OpenDeckBuildPanel();
    }

    private async System.Threading.Tasks.Task OpenDeckBuildPanel()
    {
        await _uiManager.Close(this);
        await _uiManager.Show<DeckBuildPanel>();
    }
}
