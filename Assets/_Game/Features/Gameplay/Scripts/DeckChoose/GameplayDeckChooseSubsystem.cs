using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using Zenject;

public class GameplayDeckChooseSubsystem : IGameplayDeckChooseSubsystem
{
    [Inject] private readonly IGameplayDeckChooseController _controller;
    [Inject] private readonly IGameplayDeckChooseModel _model;

    public event UnityAction<bool> IsReadyChanged;
    public event UnityAction<string> SelectedDeckIdChanged;

    public void Initialize()
    {
        _model.IsReady.OnChanged += HandleIsReadyChanged;
        _model.SelectedDeckId.OnChanged += HandleSelectedDeckIdChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.IsReady.OnChanged -= HandleIsReadyChanged;
        _model.SelectedDeckId.OnChanged -= HandleSelectedDeckIdChanged;
        _controller.Dispose();
        _model.Dispose();
    }

    public void StageSelection(DeckSummaryData summary) => _controller.StageSelection(summary);
    public Task ConfirmSelection() => _controller.ConfirmSelection();
    public Task AutoConfirmLastDeck() => _controller.AutoConfirmLastDeck();

    public void RegisterNetworkBridge(IGameplayDeckChooseNetworkBridge bridge)
        => _controller.RegisterBridge(bridge);

    public void OnAuthoritativeStateReceived(GameplayDeckChooseStateData data)
        => _controller.OnAuthoritativeStateReceived(data);

    private void HandleIsReadyChanged()
    {
        try { IsReadyChanged?.Invoke(_model.IsReady.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleSelectedDeckIdChanged()
    {
        try { SelectedDeckIdChanged?.Invoke(_model.SelectedDeckId.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
