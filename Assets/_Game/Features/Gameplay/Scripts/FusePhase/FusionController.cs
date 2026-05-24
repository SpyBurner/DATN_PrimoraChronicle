using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zenject;

internal class FusionController : IFusionController
{
    [Inject] private readonly IFusionModel _model;
    [Inject] private readonly IDebugLogger _logger;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoading;

    private IFusionNetworkBridge _bridge;
    private string _baseCardId;
    private readonly string[] _equipSlots = new string[4];
    private readonly int[] _equipHandIndices = new int[4] { -1, -1, -1, -1 };

    public void Initialize() { }

    public void Dispose()
    {
        _bridge = null;
        _baseCardId = null;
        Array.Clear(_equipSlots, 0, _equipSlots.Length);
        for (int i = 0; i < _equipHandIndices.Length; i++) _equipHandIndices[i] = -1;
    }

    public void RegisterBridge(IFusionNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log("LOG_FUSION", nameof(FusionController), $"Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(FusionStateData data) => _model.ApplyState(data);

    public void StageBase(string cardId)
    {
        _baseCardId = cardId;
        RefreshStaging();
    }

    public void StageEquipSpell(int slotIndex, string equipSpellId, int handIndex)
    {
        if (slotIndex < 0 || slotIndex >= _equipSlots.Length) return;

        // Evict the same hand-slot from any other equip slot so the same card instance
        // can't occupy two slots. Uses handIndex so two copies of the same cardId are allowed.
        if (handIndex >= 0)
        {
            for (int i = 0; i < _equipSlots.Length; i++)
            {
                if (i != slotIndex && _equipHandIndices[i] == handIndex)
                {
                    _equipSlots[i] = null;
                    _equipHandIndices[i] = -1;
                }
            }
        }

        _equipSlots[slotIndex] = equipSpellId;
        _equipHandIndices[slotIndex] = handIndex;
        RefreshStaging();
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _equipSlots.Length) return;
        _equipSlots[slotIndex] = null;
        _equipHandIndices[slotIndex] = -1;
        RefreshStaging();
    }

    public void ClearStaging()
    {
        _baseCardId = null;
        Array.Clear(_equipSlots, 0, _equipSlots.Length);
        for (int i = 0; i < _equipHandIndices.Length; i++) _equipHandIndices[i] = -1;
        RefreshStaging();
    }

    public Task ConfirmFusion()
    {
        if (string.IsNullOrEmpty(_baseCardId))
        {
            _logger.LogWarning("LOG_FUSION", nameof(FusionController), "ConfirmFusion called with no base card staged.");
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
        bool hasInnateSkill = false;
        if (!string.IsNullOrEmpty(_baseCardId) && _cardLoading != null)
        {
            if (_cardLoading.TryGetCardData(_baseCardId, out var data))
            {
                hasInnateSkill = !string.IsNullOrEmpty(data.grants_skill);
            }
        }

        _model.UpdateStaging(new FusionStagingData
        {
            BaseCardId = _baseCardId,
            EquipSpellIds = (string[])_equipSlots.Clone(),
            HasInnateSkill = hasInnateSkill
        });
    }
}
