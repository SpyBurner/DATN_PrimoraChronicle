using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;

public interface IMatchHistorySubsystem : ISubsystem
{
    event UnityAction<List<MatchHistoryData>> MatchHistoryChanged;
    Task LoadMatchHistory(string userId);
}
