using System.Collections.Generic;
using UnityObservables;

internal class MatchHistoryModel : IMatchHistoryModel
{
    private Observable<List<MatchHistoryData>> _matchHistory = new(new List<MatchHistoryData>());

    public Observable<List<MatchHistoryData>> MatchHistory { get => _matchHistory; }

    public void Initialize() { }
    
    public void Dispose() 
    { 
        _matchHistory.Value.Clear();
    }

    internal void SetMatchHistory(List<MatchHistoryData> history) => _matchHistory.Value = new List<MatchHistoryData>(history);
}
