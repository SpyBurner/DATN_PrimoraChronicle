using System;
using UnityEngine;
using Zenject;

public class PanelVisibilityRouter : MonoBehaviour
{
    [Serializable]
    public struct PhasePanel
    {
        public GameplayPhase Phase;
        public GameObject Panel;
    }

    [Inject] private readonly IGameStateSubsystem _gameState;

    [SerializeField] private PhasePanel[] _phasePanels;

    private void OnEnable()
    {
        _gameState.PhaseChanged += OnPhaseChanged;
        OnPhaseChanged(_gameState.Phase);
    }

    private void OnDisable()
    {
        _gameState.PhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(GameplayPhase phase)
    {
        try
        {
            // Collect every panel that belongs to the current phase.
            // A panel may appear in multiple entries (multiple phases),
            // so decide visibility per unique panel reference, not per entry.
            var activePanels = new System.Collections.Generic.HashSet<GameObject>();
            foreach (var entry in _phasePanels)
                if (entry.Panel != null && entry.Phase == phase)
                    activePanels.Add(entry.Panel);

            foreach (var entry in _phasePanels)
                entry.Panel?.SetActive(activePanels.Contains(entry.Panel));
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }
}
