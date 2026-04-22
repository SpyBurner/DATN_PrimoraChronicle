using Zenject;

internal class ProfileController : IProfileController
{
    [Inject] private readonly IProfileModel _model;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    public void Initialize() { }

    public void NavigateToMatchHistory()
    {
        _uiManager.ShowScreen<MatchHistoryPanel>();
    }
}
