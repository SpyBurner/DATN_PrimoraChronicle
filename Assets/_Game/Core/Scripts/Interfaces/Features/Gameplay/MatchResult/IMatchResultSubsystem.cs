using System.Threading.Tasks;
using UnityEngine.Events;

public interface IMatchResultSubsystem : ISubsystem
{
    event UnityAction<GameMatchResult> MatchEnded;

    bool HasResult { get; }
    GameMatchResult Result { get; }

    Task ReturnToLobby();
    void RegisterNetworkBridge(IMatchResultNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameMatchResult data);
}
