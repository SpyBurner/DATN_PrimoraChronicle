using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class BattleSubsystem : IBattleSubsystem
{
    [Inject] private readonly IBattleController _controller;
    [Inject] private readonly IBattleModel _model;

    public event UnityAction<string> OpponentNameChanged;
    public event UnityAction<int> OpponentLevelChanged;
    public event UnityAction<int> PlayerHPChanged;
    public event UnityAction<int> OpponentHPChanged;
    public event UnityAction<int> PlayerMaxHPChanged;
    public event UnityAction<int> OpponentMaxHPChanged;
    public event UnityAction<bool> IsReadyChanged;

    public void Initialize()
    {
        if (_model?.OpponentName != null)
            _model.OpponentName.OnChanged += HandleOpponentNameChanged;

        if (_model?.OpponentLevel != null)
            _model.OpponentLevel.OnChanged += HandleOpponentLevelChanged;

        if (_model?.PlayerHP != null)
            _model.PlayerHP.OnChanged += HandlePlayerHPChanged;

        if (_model?.OpponentHP != null)
            _model.OpponentHP.OnChanged += HandleOpponentHPChanged;

        if (_model?.PlayerMaxHP != null)
            _model.PlayerMaxHP.OnChanged += HandlePlayerMaxHPChanged;

        if (_model?.OpponentMaxHP != null)
            _model.OpponentMaxHP.OnChanged += HandleOpponentMaxHPChanged;

        if (_model?.IsReady != null)
            _model.IsReady.OnChanged += HandleIsReadyChanged;
    }

    public void Dispose()
    {
        if (_model?.OpponentName != null)
            _model.OpponentName.OnChanged -= HandleOpponentNameChanged;

        if (_model?.OpponentLevel != null)
            _model.OpponentLevel.OnChanged -= HandleOpponentLevelChanged;

        if (_model?.PlayerHP != null)
            _model.PlayerHP.OnChanged -= HandlePlayerHPChanged;

        if (_model?.OpponentHP != null)
            _model.OpponentHP.OnChanged -= HandleOpponentHPChanged;

        if (_model?.PlayerMaxHP != null)
            _model.PlayerMaxHP.OnChanged -= HandlePlayerMaxHPChanged;

        if (_model?.OpponentMaxHP != null)
            _model.OpponentMaxHP.OnChanged -= HandleOpponentMaxHPChanged;

        if (_model?.IsReady != null)
            _model.IsReady.OnChanged -= HandleIsReadyChanged;
    }

    public Task InitializeBattleSetup() => _controller.InitializeBattleSetup();

    public void SetIsReady(bool isReady) => _controller.SetIsReady(isReady);

    private void HandleOpponentNameChanged()
    {
        try { OpponentNameChanged?.Invoke(_model.OpponentName.Value); } catch { }
    }

    private void HandleOpponentLevelChanged()
    {
        try { OpponentLevelChanged?.Invoke(_model.OpponentLevel.Value); } catch { }
    }

    private void HandlePlayerHPChanged()
    {
        try { PlayerHPChanged?.Invoke(_model.PlayerHP.Value); } catch { }
    }

    private void HandleOpponentHPChanged()
    {
        try { OpponentHPChanged?.Invoke(_model.OpponentHP.Value); } catch { }
    }

    private void HandlePlayerMaxHPChanged()
    {
        try { PlayerMaxHPChanged?.Invoke(_model.PlayerMaxHP.Value); } catch { }
    }

    private void HandleOpponentMaxHPChanged()
    {
        try { OpponentMaxHPChanged?.Invoke(_model.OpponentMaxHP.Value); } catch { }
    }

    private void HandleIsReadyChanged()
    {
        try { IsReadyChanged?.Invoke(_model.IsReady.Value); } catch { }
    }
}
