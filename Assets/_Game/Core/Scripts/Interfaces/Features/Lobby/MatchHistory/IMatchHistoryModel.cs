using System.Collections.Generic;
using UnityObservables;

public interface IMatchHistoryModel : IModel
{
    Observable<List<MatchHistoryData>> MatchHistory { get; }

    void SetMatchHistory(List<MatchHistoryData> history);
}

[System.Serializable]
public class MatchHistoryData
{
    public string matchID;
    public string endDateTime;
    public bool isWinner;
    public int goldReceived;
    public int xpReceived;
    public string actionLogURL;
}
