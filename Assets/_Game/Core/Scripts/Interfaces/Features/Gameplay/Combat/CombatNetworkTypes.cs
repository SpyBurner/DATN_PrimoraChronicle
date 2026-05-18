using Fusion;

public struct CombatStateData : INetworkStruct
{
    public NetworkString<_16> CurrentAttackerId;
    public NetworkString<_16> CurrentDefenderId;
    public NetworkString<_64> CombatLog;
}

public interface ICombatNetworkBridge
{
    void SendExecuteTurnRpc();
    void SendSkipCombatRpc();
}
