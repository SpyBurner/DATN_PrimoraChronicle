using Zenject;

public class MatchHistoryPanel : UIPanel
{
    [Inject] private readonly IMatchHistorySubsystem _matchHistory;
}
