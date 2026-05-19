using System;
using UnityEngine;

/// <summary>
/// Scene-level MonoBehaviour. Polls NetworkGameplayManager.CurrentPhase each frame and
/// activates exactly the panel(s) mapped to that phase. Configure the mapping in the
/// Inspector — add one entry per phase that has a visible panel.
/// </summary>
public class PhaseVisibilityController : MonoBehaviour
{
    [Serializable]
    public struct PhasePanel
    {
        public GameplayPhase Phase;
        public GameObject Panel;
    }

    [SerializeField] private PhasePanel[] _phasePanels;

    private GameplayPhase _lastPhase = (GameplayPhase)(-1);

    private void Update()
    {
        if (NetworkGameplayManager.Instance == null) return;
        GameplayPhase current = NetworkGameplayManager.Instance.CurrentPhase;
        if (current == _lastPhase) return;
        _lastPhase = current;
        ApplyVisibility(current);
    }

    private void ApplyVisibility(GameplayPhase current)
    {
        if (_phasePanels == null) return;
        foreach (var entry in _phasePanels)
            entry.Panel?.SetActive(entry.Phase == current);
    }
}
