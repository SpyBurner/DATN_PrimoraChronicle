using Fusion;
using UnityEngine;
using Zenject;

public class GameStateNetworkView : NetworkBehaviour, IGameStateNetworkBridge
{
    [Inject(Optional = true)] private IGameStateSubsystem _gameState;
    [Inject(Optional = true)] private IUnitSubsystem _unitSubsystem;
    [Inject(Optional = true)] private INetworkManagerSubsystem _networkManager;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public GameplayPhase CurrentPhase { get; set; }
    [Networked] public TickTimer PhaseTimer { get; set; }
    [Networked] public int RoundNumber { get; set; }
    [Networked] public float MatchElapsed { get; set; }
    [Networked] public PlayerRef CurrentCombatActor { get; set; }
    [Networked] public NetworkBool IsMatchOver { get; set; }
    // Capacity 8: allow 0-based PlayerId
    [Networked, Capacity(8)] public NetworkArray<NetworkBool> PlayerReady => default;

    [Header("Phase Durations")]
    [SerializeField] private float _startPhaseDuration = 30f;
    [SerializeField] private float _mainPhaseDuration = 60f;
    [SerializeField] private float _drawPhaseDuration = 30f;
    [SerializeField] private float _matchTimeLimit = 3600f;

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        // GameObjectContext handles injection when set up on the prefab.
        // If it hasn't fired (e.g. Fusion instantiation bypassed Awake ordering),
        // fall back to resolving from the SceneContext directly.
        if (_gameState == null)
        {
            var ctx = FindFirstObjectByType<SceneContext>();
            if (ctx != null)
            {
                _gameState = ctx.Container.Resolve<IGameStateSubsystem>();
                _unitSubsystem = ctx.Container.TryResolve<IUnitSubsystem>();
                _networkManager = ctx.Container.TryResolve<INetworkManagerSubsystem>();
                _logger  = ctx.Container.Resolve<IDebugLogger>();
            }
            else
            {
                Debug.LogError("[GameStateNetworkView] SceneContext not found — injection failed.");
                return;
            }
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _gameState.RegisterNetworkBridge(this);

        if (Object.HasStateAuthority)
        {
            CurrentPhase = GameplayPhase.StartPhase;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, _startPhaseDuration);
            RoundNumber = 1;
            MatchElapsed = 0f;
            IsMatchOver = false;
            // Reset all ready flags
            for (int i = 0; i < PlayerReady.Length; i++) PlayerReady.Set(i, false);
            _logger?.Log("[GameStateNetworkView] Spawned as StateAuthority. Phase=StartPhase.");

            if (_networkManager != null)
                _networkManager.PlayerLeft += HandlePlayerLeft;
        }

        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _gameState?.RegisterNetworkBridge(null);
        if (_networkManager != null)
            _networkManager.PlayerLeft -= HandlePlayerLeft;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (IsMatchOver) return;

        MatchElapsed += Runner.DeltaTime;

        if (MatchElapsed >= _matchTimeLimit)
        {
            EndMatchByTimeLimit();
            return;
        }

        if (CurrentPhase == GameplayPhase.StartPhase && AreAllPlayersReady())
        {
            TransitionTo(GameplayPhase.MainPhase);
            return;
        }

        if (CurrentPhase != GameplayPhase.Setup && CurrentPhase != GameplayPhase.StartPhase)
        {
            var eliminatedPlayer = CheckElimination();
            if (eliminatedPlayer.IsRealPlayer)
            {
                EndMatchByElimination(eliminatedPlayer);
                return;
            }
        }

        if (CurrentPhase == GameplayPhase.DrawPhase && AllPlayersDrawPhaseConfirmed())
        {
            RoundNumber++;
            TransitionTo(GameplayPhase.MainPhase);
            return;
        }

        if (PhaseTimer.Expired(Runner))
        {
            HandlePhaseTimeout();
        }
    }

    private void HandlePhaseTimeout()
    {
        switch (CurrentPhase)
        {
            case GameplayPhase.StartPhase:
                AutoConfirmUnreadyPlayers();
                TransitionTo(GameplayPhase.MainPhase);
                break;
            case GameplayPhase.MainPhase:
                AutoDeployUnfusedPlayers();
                TransitionTo(GameplayPhase.CombatPhase);
                break;
            case GameplayPhase.DrawPhase:
                AutoKeepUnconfirmedPlayers();
                RoundNumber++;
                TransitionTo(GameplayPhase.MainPhase);
                break;
        }
    }

    private void AutoConfirmUnreadyPlayers()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return;

        foreach (var player in coordinator.GetAllPlayers())
        {
            var dcView = coordinator.GetDeckChooseView(player);
            if (dcView != null && !dcView.IsReady)
            {
                dcView.ServerAutoConfirm(player.PlayerId);
                _logger?.Log($"[GameStateNetworkView] Auto-confirmed deck for unready player {player}.");
            }
        }
    }

    private void AutoDeployUnfusedPlayers()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return;

        foreach (var player in coordinator.GetAllPlayers())
        {
            var fusionView = coordinator.GetFusionView(player);
            if (fusionView == null || fusionView.HasFusedThisTurn) continue;

            var pczView = coordinator.GetPlayerCardZoneView(player);
            string championId = pczView?.ChampionId.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(championId))
            {
                fusionView.ServerAutoConfirmFusion(championId);
                _logger?.Log($"[GameStateNetworkView] Auto-deployed Champion for unready player {player}.");
            }
        }
    }

    private void TransitionTo(GameplayPhase newPhase)
    {
        CurrentPhase = newPhase;

        // Reset all player ready flags on every phase transition
        if (Object.HasStateAuthority)
            for (int i = 0; i < PlayerReady.Length; i++) PlayerReady.Set(i, false);

        switch (newPhase)
        {
            case GameplayPhase.StartPhase:
                PhaseTimer = TickTimer.CreateFromSeconds(Runner, _startPhaseDuration);
                break;
            case GameplayPhase.MainPhase:
                PhaseTimer = TickTimer.CreateFromSeconds(Runner, _mainPhaseDuration);
                break;
            case GameplayPhase.CombatPhase:
                PhaseTimer = TickTimer.None;
                StartCombatPhase();
                break;
            case GameplayPhase.DrawPhase:
                PhaseTimer = TickTimer.CreateFromSeconds(Runner, _drawPhaseDuration);
                StartDrawPhase();
                break;
            case GameplayPhase.GameOver:
                PhaseTimer = TickTimer.None;
                IsMatchOver = true;
                break;
        }

        _logger?.Log($"[GameStateNetworkView] Phase transition -> {newPhase}");
    }

    private bool AreAllPlayersReady()
    {
        // Use the authoritative PlayerReady NetworkArray.
        // At least 2 active players must exist and all must be ready.
        int activeCount = 0;
        foreach (var _ in Runner.ActivePlayers) activeCount++;
        if (activeCount < 2) return false;

        foreach (var player in Runner.ActivePlayers)
        {
            int slot = player.PlayerId;
            if (slot < 0 || slot >= PlayerReady.Length) return false;
            if (!PlayerReady.Get(slot)) return false;
        }
        return true;
    }

    private void StartCombatPhase()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        coordinator?.CombatView?.ServerStartCombatPhase();
    }

    private void StartDrawPhase()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return;

        foreach (var player in coordinator.GetAllPlayers())
        {
            var pczView = coordinator.GetPlayerCardZoneView(player);
            pczView?.ServerStartDrawPhase();
        }

        _logger?.Log("[GameStateNetworkView] DrawPhase started — drew cards for all players.");
    }

    private void AutoKeepUnconfirmedPlayers()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return;

        foreach (var player in coordinator.GetAllPlayers())
        {
            var pczView = coordinator.GetPlayerCardZoneView(player);
            if (pczView != null && !pczView.DrawPhaseConfirmed)
            {
                pczView.ServerAutoKeepOnTimeout();
                _logger?.Log($"[GameStateNetworkView] Auto-kept cards for unconfirmed player {player}.");
            }
        }
    }

    private bool AllPlayersDrawPhaseConfirmed()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return false;

        foreach (var player in coordinator.GetAllPlayers())
        {
            var pczView = coordinator.GetPlayerCardZoneView(player);
            if (pczView != null && !pczView.DrawPhaseConfirmed)
                return false;
        }
        return true;
    }

    // ── Win Condition ───────────────────────────────────────────────────

    private PlayerRef CheckElimination()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return default;

        foreach (var player in coordinator.GetAllPlayers())
        {
            var pczView = coordinator.GetPlayerCardZoneView(player);
            if (pczView != null && pczView.IsSetup && pczView.HP <= 0)
                return player;
        }
        return default;
    }

    private void EndMatchByElimination(PlayerRef eliminatedPlayer)
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return;

        PlayerRef winner = default;
        foreach (var player in coordinator.GetAllPlayers())
        {
            if (player != eliminatedPlayer)
            {
                winner = player;
                break;
            }
        }

        CommitMatchResult(new GameMatchResult
        {
            Winner = winner,
            IsTie = false,
            GoldEarned = CalculateGoldReward(),
            XPEarned = CalculateXPReward(),
            DurationSeconds = MatchElapsed
        });
    }

    private void EndMatchByTimeLimit()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null)
        {
            TransitionTo(GameplayPhase.GameOver);
            return;
        }

        PlayerRef highestHPPlayer = default;
        int highestHP = int.MinValue;
        bool isTie = false;

        foreach (var player in coordinator.GetAllPlayers())
        {
            var pczView = coordinator.GetPlayerCardZoneView(player);
            if (pczView == null) continue;

            int hp = pczView.HP;
            if (hp > highestHP)
            {
                highestHP = hp;
                highestHPPlayer = player;
                isTie = false;
            }
            else if (hp == highestHP)
            {
                isTie = true;
            }
        }

        CommitMatchResult(new GameMatchResult
        {
            Winner = isTie ? default : highestHPPlayer,
            IsTie = isTie,
            GoldEarned = isTie ? 0 : CalculateGoldReward(),
            XPEarned = CalculateXPReward(),
            DurationSeconds = MatchElapsed
        });
    }

    private void CommitMatchResult(GameMatchResult result)
    {
        TransitionTo(GameplayPhase.GameOver);

        var coordinator = GameplayNetworkCoordinator.Instance;
        var matchResultView = coordinator?.MatchResultView;
        if (matchResultView != null)
        {
            matchResultView.ServerEndMatch(result);
        }
        else
        {
            _logger?.LogWarning("[GameStateNetworkView] MatchResultView not found — cannot commit result.");
        }
    }

    private int CalculateGoldReward()
    {
        int baseGold = 50;
        int roundBonus = RoundNumber * 5;
        return baseGold + roundBonus;
    }

    private int CalculateXPReward()
    {
        int baseXP = 100;
        int roundBonus = RoundNumber * 10;
        return baseXP + roundBonus;
    }

    // ── IGameStateNetworkBridge ──────────────────────────────────────────

    public void SendPhaseTransitionRpc(GameplayPhase phase)
        => Rpc_RequestPhaseTransition(Runner.LocalPlayer, phase);

    public void SendSetReadyRpc(bool ready)
        => Rpc_SetReady(Runner.LocalPlayer, ready);

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_SetReady(PlayerRef sender, bool ready, RpcInfo info = default)
    {
        if (_gameState == null) return;

        // Only accept ready=true when AcceptsReadyInput; always accept ready=false (unless locked)
        if (ready && !_gameState.AcceptsReadyInput)
        {
            _logger?.Log($"[GameStateNetworkView] Rpc_SetReady(true) rejected — AcceptsReadyInput=false for phase {CurrentPhase}.");
            return;
        }

        int slotIndex = sender.PlayerId;
        if (slotIndex < 0 || slotIndex >= PlayerReady.Length) return;

        // Lock: once ready=true, server ignores ready=false until phase advances
        if (!ready && PlayerReady.Get(slotIndex))
        {
            _logger?.Log($"[GameStateNetworkView] Rpc_SetReady(false) rejected — {sender} already locked ready.");
            return;
        }

        PlayerReady.Set(slotIndex, ready);
        _gameState.OnPlayerReadyChanged(sender, ready);
        _logger?.Log($"[GameStateNetworkView] Rpc_SetReady({ready}) from {sender} — slot {slotIndex} written.");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_RequestPhaseTransition(PlayerRef sender, GameplayPhase phase, RpcInfo info = default)
    {
        TransitionTo(phase);
    }

    // ── Server-side API for other subsystems (Combat, DeckChoose, etc.) ──

    public void ServerTransitionToPhase(GameplayPhase phase)
    {
        if (!Object.HasStateAuthority) return;
        TransitionTo(phase);
    }

    public void ServerTransitionToDrawPhase()
    {
        if (!Object.HasStateAuthority) return;
        RoundNumber++;
        TransitionTo(GameplayPhase.DrawPhase);
    }

    public void ServerSetCombatActor(PlayerRef actor)
    {
        if (!Object.HasStateAuthority) return;
        CurrentCombatActor = actor;
    }

    public void ServerCheckElimination()
    {
        if (!Object.HasStateAuthority) return;
        if (IsMatchOver) return;

        var eliminatedPlayer = CheckElimination();
        if (eliminatedPlayer.IsRealPlayer)
            EndMatchByElimination(eliminatedPlayer);
    }

    // ── State push (server → all clients) ────────────────────────────────

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var _ in _changeDetector.DetectChanges(this))
        {
            PushState();
            break;
        }
    }

    private void PushState()
    {
        if (_gameState == null) return;
        float timeRemaining = 0f;
        if (PhaseTimer.IsRunning)
        {
            var remaining = PhaseTimer.RemainingTime(Runner);
            timeRemaining = remaining.HasValue ? remaining.Value : 0f;
        }

        // Copy NetworkArray<NetworkBool> into plain bool[] for the data struct
        var readyArr = new bool[PlayerReady.Length];
        for (int i = 0; i < PlayerReady.Length; i++)
            readyArr[i] = PlayerReady.Get(i);

        // Detect and fire events for changed ready states
        foreach (var player in Runner.ActivePlayers)
        {
            int playerId = player.PlayerId;
            if (playerId >= 0 && playerId < readyArr.Length)
            {
                bool newReady = readyArr[playerId];
                bool oldReady = _gameState.IsReady(player);
                if (newReady != oldReady)
                    _gameState.OnPlayerReadyChanged(player, newReady);
            }
        }

        _gameState.OnAuthoritativeStateReceived(new GameStateData
        {
            Phase = CurrentPhase,
            PhaseTimeRemaining = timeRemaining,
            MatchElapsed = MatchElapsed,
            RoundNumber = RoundNumber,
            CurrentCombatActor = CurrentCombatActor,
            PlayerReady = readyArr,
        });
    }

    // ── Forfeit / disconnect ─────────────────────────────────────────────

    private void HandlePlayerLeft(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (IsMatchOver) return;

        _logger?.Log($"[GameStateNetworkView] Player {player} disconnected — treating as forfeit.");

        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator != null && _unitSubsystem != null)
        {
            var allUnits = _unitSubsystem.AllUnits;
            if (allUnits != null)
            {
                var toDestroy = new System.Collections.Generic.List<NetworkId>();
                foreach (var netId in allUnits)
                {
                    if (_unitSubsystem.TryGetPublic(netId, out UnitPublicData data) && data.Owner == player)
                        toDestroy.Add(netId);
                }

                foreach (var netId in toDestroy)
                {
                    if (!_unitSubsystem.TryGetPublic(netId, out UnitPublicData data)) continue;

                    if (data.DeathAnchor > 0)
                    {
                        var pczView = coordinator.GetPlayerCardZoneView(player);
                        pczView?.ServerApplyDamage(data.DeathAnchor);
                    }

                    if (Runner.TryFindObject(netId, out var netObj))
                        Runner.Despawn(netObj);
                }
            }
        }

        ServerCheckElimination();
    }
}
