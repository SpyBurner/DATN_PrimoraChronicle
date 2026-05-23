using System.Threading.Tasks;

public interface IFusionController : IController
{
    void StageBase(string cardId);
    void StageEquipSpell(int slotIndex, string equipSpellId, int handIndex);
    void ClearSlot(int slotIndex);
    void ClearStaging();
    Task ConfirmFusion();
    void RegisterBridge(IFusionNetworkBridge bridge);
    void OnAuthoritativeStateReceived(FusionStateData data);
}
