using Fusion;
using UnityEngine.Events;

public interface IServerSessionSubsystem : ISubsystem
{
    event UnityAction<string> SessionStarted;
    event UnityAction<PlayerRef> PlayerJoined;
    event UnityAction<PlayerRef> PlayerLeft;

    event UnityAction MatchEnded;

    void EndMatch(string winnerUserId, string loserUserId, string endReason);
}
