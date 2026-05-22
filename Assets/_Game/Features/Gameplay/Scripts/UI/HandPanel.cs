using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class HandPanel : MonoBehaviour
{
    [Inject] private readonly IPlayerCardZoneSubsystem _cardZone;
    [Inject] private readonly IGameStateSubsystem _gameState;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoading;

    [Header("References")]
    [SerializeField] private Transform _cardSlotContainer;
    [SerializeField] private GameObject _cardSlotPrefab;
    [SerializeField] private TMP_Text _handCountText;

    private readonly List<GameObject> _spawnedSlots = new();

    private PlayerRef _localPlayer;

    private void OnEnable()
    {
        _cardZone.HandChanged += OnHandChanged;
        _gameState.PhaseChanged += OnPhaseChanged;

        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null) _localPlayer = runner.LocalPlayer;

        var hand = _cardZone.GetHand(_localPlayer);
        if (hand != null) RenderHand(hand);
    }

    private void OnDisable()
    {
        _cardZone.HandChanged -= OnHandChanged;
        _gameState.PhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(GameplayPhase phase)
    {
        try
        {
            bool show = phase == GameplayPhase.MainPhase || phase == GameplayPhase.CombatPhase;
            gameObject.SetActive(show);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnHandChanged(PlayerRef player, IReadOnlyList<string> hand)
    {
        if (player != _localPlayer) return;
        try
        {
            RenderHand(hand);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void RenderHand(IReadOnlyList<string> hand)
    {
        ClearSlots();
        if (hand == null) return;

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

            var dragHandle = slot.GetComponent<CardDragHandle>();
            if (dragHandle != null)
                dragHandle.Initialize(cardId, i);

            _spawnedSlots.Add(slot);
        }

        if (_handCountText != null)
            _handCountText.text = $"{hand.Count}/6";
    }

    private void ClearSlots()
    {
        foreach (var slot in _spawnedSlots)
            if (slot != null) Destroy(slot);
        _spawnedSlots.Clear();
    }

    public string GetCardIdAtIndex(int index)
    {
        var hand = _cardZone.GetHand(_localPlayer);
        if (hand == null || index < 0 || index >= hand.Count) return null;
        return hand[index];
    }
}
