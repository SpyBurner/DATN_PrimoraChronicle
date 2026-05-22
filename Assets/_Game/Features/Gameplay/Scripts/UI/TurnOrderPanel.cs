using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class TurnOrderPanel : MonoBehaviour
{
    [Inject] private readonly ICombatSubsystem _combat;
    [Inject] private readonly IUnitSubsystem _unit;
    [Inject] private readonly IGameStateSubsystem _gameState;
    [Inject] private readonly INetworkManagerSubsystem _network;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoading;

    [Header("References")]
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _turnOrderItemPrefab;
    [SerializeField] private Color _currentActorHighlight = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _localPlayerColor = new Color(0.3f, 0.7f, 1f, 1f);
    [SerializeField] private Color _opponentColor = new Color(1f, 0.4f, 0.4f, 1f);

    private readonly List<GameObject> _spawnedItems = new();
    private PlayerRef _localPlayer;

    private void OnEnable()
    {
        _localPlayer = _network.Runner != null ? _network.Runner.LocalPlayer : default;
        _combat.QueueChanged += OnQueueChanged;
        _combat.CurrentTurnChanged += OnCurrentTurnChanged;
        _gameState.PhaseChanged += OnPhaseChanged;

        if (_combat.IsCombatActive)
            RenderQueue(_combat.ActionQueue);
    }

    private void OnDisable()
    {
        _combat.QueueChanged -= OnQueueChanged;
        _combat.CurrentTurnChanged -= OnCurrentTurnChanged;
        _gameState.PhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(GameplayPhase phase)
    {
        try
        {
            gameObject.SetActive(phase == GameplayPhase.CombatPhase);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnQueueChanged(IReadOnlyList<string> queue)
    {
        try
        {
            RenderQueue(queue);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnCurrentTurnChanged(string currentActorId)
    {
        try
        {
            HighlightCurrentActor(currentActorId);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void RenderQueue(IReadOnlyList<string> queue)
    {
        ClearItems();
        if (queue == null) return;

        for (int i = 0; i < queue.Count; i++)
        {
            string unitId = queue[i];
            var item = Instantiate(_turnOrderItemPrefab, _content);
            item.SetActive(true);

            var nameText = item.GetComponentInChildren<TMP_Text>();
            var bg = item.GetComponent<Image>();

            if (_unit.TryGetUnit(unitId, out var unitData))
            {
                if (nameText != null)
                {
                    string displayName = unitId;
                    if (_cardLoading.TryGetCardData(unitId, out var cardData))
                        displayName = cardData.name;
                    nameText.text = displayName;
                }

                if (bg != null)
                    bg.color = unitData.Owner == _localPlayer ? _localPlayerColor : _opponentColor;
            }
            else
            {
                if (nameText != null) nameText.text = unitId;
            }

            bool isCurrent = unitId == _combat.CurrentActorId;
            if (isCurrent && bg != null)
                bg.color = _currentActorHighlight;

            _spawnedItems.Add(item);
        }
    }

    private void HighlightCurrentActor(string currentActorId)
    {
        var queue = _combat.ActionQueue;
        if (queue == null) return;

        for (int i = 0; i < _spawnedItems.Count && i < queue.Count; i++)
        {
            var item = _spawnedItems[i];
            if (item == null) continue;

            var bg = item.GetComponent<Image>();
            if (bg == null) continue;

            bool isCurrent = queue[i] == currentActorId;
            if (isCurrent)
            {
                bg.color = _currentActorHighlight;
            }
            else if (_unit.TryGetUnit(queue[i], out var unitData))
            {
                bg.color = unitData.Owner == _localPlayer ? _localPlayerColor : _opponentColor;
            }
        }
    }

    private void ClearItems()
    {
        foreach (var item in _spawnedItems)
            if (item != null) Destroy(item);
        _spawnedItems.Clear();
    }
}
