using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class DrawPhasePanel : MonoBehaviour
{
    [Inject] private readonly IPlayerCardZoneSubsystem _cardZone;
    [Inject] private readonly IGameStateSubsystem _gameState;
    [Inject] private readonly INetworkManagerSubsystem _network;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoading;

    [Header("References")]
    [SerializeField] private Transform _cardSlotContainer;
    [SerializeField] private GameObject _cardSlotPrefab;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TMP_Text _keepCountText;

    [Header("Selection Visuals")]
    [SerializeField] private Color _selectedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color _discardedColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

    private const int HandMax = 6;

    private readonly List<GameObject> _spawnedSlots = new();
    private readonly HashSet<int> _selectedIndices = new();
    private PlayerRef _localPlayer;
    private IReadOnlyList<string> _currentCards;

    private void OnEnable()
    {
        _localPlayer = _network.Runner != null ? _network.Runner.LocalPlayer : default;
        _cardZone.HandChanged += OnHandChanged;
        _confirmButton?.onClick.AddListener(OnConfirmClicked);

        var hand = _cardZone.GetHand(_localPlayer);
        if (hand != null) RefreshDisplay(hand);
    }

    private void OnDisable()
    {
        _cardZone.HandChanged -= OnHandChanged;
        _confirmButton?.onClick.RemoveListener(OnConfirmClicked);
        ClearSlots();
        _selectedIndices.Clear();
        _currentCards = null;
    }

    private void OnHandChanged(PlayerRef player, IReadOnlyList<string> hand)
    {
        try
        {
            if (player != _localPlayer) return;
            RefreshDisplay(hand);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void RefreshDisplay(IReadOnlyList<string> hand)
    {
        ClearSlots();
        _selectedIndices.Clear();
        _currentCards = hand;

        if (hand == null || hand.Count == 0)
        {
            UpdateKeepCount();
            return;
        }

        for (int i = 0; i < hand.Count; i++)
        {
            _selectedIndices.Add(i);
        }

        for (int i = 0; i < hand.Count; i++)
        {
            var cardId = hand[i];
            var slot = Instantiate(_cardSlotPrefab, _cardSlotContainer);
            slot.SetActive(true);

            var nameText = slot.GetComponentInChildren<TMP_Text>();
            if (nameText != null && _cardLoading.TryGetCardData(cardId, out var cardData))
                nameText.text = cardData.name;
            else if (nameText != null)
                nameText.text = cardId;

            int index = i;
            var button = slot.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => ToggleCard(index));

            _spawnedSlots.Add(slot);
        }

        if (hand.Count <= HandMax)
        {
            UpdateConfirmState();
            UpdateKeepCount();
            return;
        }

        UpdateSlotVisuals();
        UpdateConfirmState();
        UpdateKeepCount();
    }

    private void ToggleCard(int index)
    {
        if (_currentCards == null) return;

        if (_selectedIndices.Contains(index))
        {
            _selectedIndices.Remove(index);
        }
        else
        {
            if (_selectedIndices.Count >= HandMax) return;
            _selectedIndices.Add(index);
        }

        UpdateSlotVisuals();
        UpdateConfirmState();
        UpdateKeepCount();
    }

    private void UpdateSlotVisuals()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            var slot = _spawnedSlots[i];
            if (slot == null) continue;

            var image = slot.GetComponent<Image>();
            if (image != null)
                image.color = _selectedIndices.Contains(i) ? _selectedColor : _discardedColor;
        }
    }

    private void UpdateConfirmState()
    {
        if (_confirmButton == null) return;
        _confirmButton.interactable = _selectedIndices.Count <= HandMax;
    }

    private void UpdateKeepCount()
    {
        if (_keepCountText == null) return;
        int total = _currentCards?.Count ?? 0;
        _keepCountText.text = $"{_selectedIndices.Count}/{HandMax}";
    }

    private void OnConfirmClicked()
    {
        if (_currentCards == null) return;
        if (_selectedIndices.Count > HandMax) return;

        var keep = new List<string>();
        foreach (int i in _selectedIndices)
        {
            if (i >= 0 && i < _currentCards.Count)
                keep.Add(_currentCards[i]);
        }

        _cardZone.RequestKeepCards(_localPlayer, keep);
        if (_confirmButton != null) _confirmButton.interactable = false;
    }

    private void ClearSlots()
    {
        foreach (var slot in _spawnedSlots)
            if (slot != null) Destroy(slot);
        _spawnedSlots.Clear();
    }
}
