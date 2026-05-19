using System.Collections.Generic;
using Zenject;
using UnityEngine;
public class MatchHistoryPanel : UIPanel
{
    [Inject] private readonly IMatchHistorySubsystem _matchHistory;
    [SerializeField] private GameObject _matchHistoryPrefab;
    [SerializeField] private GameObject _matchHistoryContainer;

    protected override void OnEnable()
    {
        base.OnEnable();
        _matchHistory.MatchHistoryChanged += OnMatchHistoryChanged;
        _ = _matchHistory.LoadMatchHistory();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _matchHistory.MatchHistoryChanged -= OnMatchHistoryChanged;
    }

    private void OnMatchHistoryChanged(List<MatchHistoryData> history)
    {
        if (_matchHistoryContainer == null || _matchHistoryPrefab == null) return;

        foreach (Transform child in _matchHistoryContainer.transform)
            Destroy(child.gameObject);

        if (history == null) return;

        foreach (var data in history)
        {
            var go = Instantiate(_matchHistoryPrefab, _matchHistoryContainer.transform);
            var item = go.GetComponentInChildren<MatchHistoryItem>();
            item?.Setup(data);
        }

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
            _matchHistoryContainer.transform as UnityEngine.RectTransform);
    }
}
