using UnityObservables;

public interface IMatchMakingModel : IModel
{
    Observable<string> Status { get; }
    Observable<float> Timer { get; }
    Observable<int> PlayerJoinedCount {  get; }

    void SetStatus(string status);
    void SetTimer(float timer);
    void SetPlayerJoinedCount(int count);
}
