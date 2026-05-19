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
            foreach (var entry in _phasePanels)
                if (entry.Panel != null) entry.Panel.SetActive(false);

            foreach (var entry in _phasePanels)
                if (entry.Panel != null && entry.Phase == phase)
                    entry.Panel.SetActive(true);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }
}
