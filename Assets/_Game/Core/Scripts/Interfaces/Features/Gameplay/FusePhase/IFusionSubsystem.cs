using System.Threading.Tasks;
using UnityEngine.Events;

public interface IFusionSubsystem : ISubsystem
{
    event UnityAction<FusionStagingData> StagingChanged;
    event UnityAction FusionConfirmed;

    FusionStagingData CurrentStaging { get; }
    bool IsConfirmed { get; }

    void StageBase(string cardId);
    void StageEquipSpell(int slotIndex, string equipSpellId);
    void ClearSlot(int slotIndex);
    void ClearStaging();
    Task ConfirmFusion();

    void RegisterNetworkBridge(IFusionNetworkBridge bridge);
    void OnAuthoritativeStateReceived(FusionStateData data);
}
