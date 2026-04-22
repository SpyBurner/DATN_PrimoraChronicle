using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ProfilePanel : UIPanel
{
    [Inject] private readonly IProfileSubsystem _profile;

    [SerializeField] private Button _matchHistoryButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _matchHistoryButton?.onClick.AddListener(OnMatchHistory);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _matchHistoryButton?.onClick.RemoveListener(OnMatchHistory);
    }

    private void OnMatchHistory() => _profile.NavigateToMatchHistory();
}
