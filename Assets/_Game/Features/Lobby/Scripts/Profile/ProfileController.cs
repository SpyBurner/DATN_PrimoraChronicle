using Zenject;

internal class ProfileController : IProfileController
{
    [Inject] private readonly IProfileModel _model;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    public void Initialize() { }
    public void Dispose() { }

    public void NavigateToMatchHistory()
    {
        _uiManager.ShowScreen<MatchHistoryPanel>();
    }
}
