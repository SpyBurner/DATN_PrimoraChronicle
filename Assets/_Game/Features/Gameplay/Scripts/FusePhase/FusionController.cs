using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zenject;

internal class FusionController : IFusionController
{
    [Inject] private readonly IFusionModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private IFusionNetworkBridge _bridge;
    private string _baseCardId;
    private readonly string[] _equipSlots = new string[4];

    public void Initialize() { }

    public void Dispose()
    {
        _bridge = null;
        _baseCardId = null;
        Array.Clear(_equipSlots, 0, _equipSlots.Length);
    }

    public void RegisterBridge(IFusionNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[Fusion] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(FusionStateData data) => _model.ApplyState(data);

    public void StageBase(string cardId)
    {
        _baseCardId = cardId;
        RefreshStaging();
    }

    public void StageEquipSpell(int slotIndex, string equipSpellId)
    {
        if (slotIndex < 0 || slotIndex >= _equipSlots.Length) return;

        // Evict the same cardId from any other slot so the same card can't occupy two slots.
        if (!string.IsNullOrEmpty(equipSpellId))
        {
            for (int i = 0; i < _equipSlots.Length; i++)
            {
                if (i != slotIndex && _equipSlots[i] == equipSpellId)
                    _equipSlots[i] = null;
            }
        }

        _equipSlots[slotIndex] = equipSpellId;
        RefreshStaging();
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _equipSlots.Length) return;
        _equipSlots[slotIndex] = null;
        RefreshStaging();
    }

    public void ClearStaging()
    {
        _baseCardId = null;
        Array.Clear(_equipSlots, 0, _equipSlots.Length);
        RefreshStaging();
    }

    public Task ConfirmFusion()
    {
        if (string.IsNullOrEmpty(_baseCardId))
        {
            _logger.LogWarning("[Fusion] ConfirmFusion called with no base card staged.");
            return Task.CompletedTask;
        }

        var filled = new List<string>();
        foreach (var s in _equipSlots)
            if (!string.IsNullOrEmpty(s)) filled.Add(s);

        if (_bridge != null)
            _bridge.SendConfirmFusionRpc(_baseCardId, string.Join(",", filled));

        return Task.CompletedTask;
    }

    private void RefreshStaging()
    {
        _model.UpdateStaging(new FusionStagingData
        {
            BaseCardId = _baseCardId,
            EquipSpellIds = (string[])_equipSlots.Clone()
        });
    }
}
