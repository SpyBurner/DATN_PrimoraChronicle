using Fusion;
using UnityObservables;

public interface IServerSessionModel : IModel
{
    Observable<string> ActiveSessionName { get; }
    Observable<bool> IsRunning { get; }
    Observable<PlayerRef> LastJoinedPlayer { get; }
    Observable<PlayerRef> LastLeftPlayer { get; }

    void ApplyState(ServerSessionStateData data);
}
