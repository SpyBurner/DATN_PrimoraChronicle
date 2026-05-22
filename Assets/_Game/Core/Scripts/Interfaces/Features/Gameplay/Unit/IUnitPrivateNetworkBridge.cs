using System.Collections.Generic;
using Fusion;

public interface IUnitPrivateNetworkBridge
{
    void SendSkillsUpdatedRpc(NetworkId unitId, IReadOnlyList<SkillSlot> skills);
}
