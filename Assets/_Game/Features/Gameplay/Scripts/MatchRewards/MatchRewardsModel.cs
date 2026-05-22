internal class MatchRewardsModel : IMatchRewardsModel
{
    public event System.Action<int, int> OwnRewardsReceived;

    public int OwnGold { get; private set; }
    public int OwnXP { get; private set; }

    public void Initialize() { }

    public void Dispose()
    {
        OwnGold = 0;
        OwnXP = 0;
    }

    public void ApplyState(MatchRewardsPrivateData data)
    {
        OwnGold = data.GoldEarned;
        OwnXP = data.XPEarned;
        OwnRewardsReceived?.Invoke(OwnGold, OwnXP);
    }
}
