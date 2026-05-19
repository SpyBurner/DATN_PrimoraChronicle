using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using Zenject;

public class FusionSubsystem : IFusionSubsystem
{
    [Inject] private readonly IFusionController _controller;
    [Inject] private readonly IFusionModel _model;

    public event UnityAction<FusionStagingData> StagingChanged;
    public event UnityAction FusionConfirmed;

    public FusionStagingData CurrentStaging => _model.Staging.Value;
    public bool IsConfirmed => _model.IsConfirmed.Value;

    public void Initialize()
    {
        _model.Staging.OnChanged += HandleStagingChanged;
        _model.IsConfirmed.OnChanged += HandleIsConfirmedChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.Staging.OnChanged -= HandleStagingChanged;
        _model.IsConfirmed.OnChanged -= HandleIsConfirmedChanged;
        _controller.Dispose();
        _model.Dispose();
    }

    public void StageBase(string cardId) => _controller.StageBase(cardId);
    public void StageEquipSpell(int slotIndex, string equipSpellId) => _controller.StageEquipSpell(slotIndex, equipSpellId);
    public void ClearSlot(int slotIndex) => _controller.ClearSlot(slotIndex);
    public void ClearStaging() => _controller.ClearStaging();
    public Task ConfirmFusion() => _controller.ConfirmFusion();

    public void RegisterNetworkBridge(IFusionNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnAuthoritativeStateReceived(FusionStateData data) => _controller.OnAuthoritativeStateReceived(data);

    private void HandleStagingChanged()
    {
        try { StagingChanged?.Invoke(_model.Staging.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleIsConfirmedChanged()
    {
        if (!_model.IsConfirmed.Value) return;
        try { FusionConfirmed?.Invoke(); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
