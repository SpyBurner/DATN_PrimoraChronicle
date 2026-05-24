using System;

public interface IMatchRewardsModel : IModel
{
    event Action<int, int> OwnRewardsReceived; // (gold, xp)
    int OwnGold { get; }
    int OwnXP { get; }
    void ApplyState(MatchRewardsPrivateData data);
}
