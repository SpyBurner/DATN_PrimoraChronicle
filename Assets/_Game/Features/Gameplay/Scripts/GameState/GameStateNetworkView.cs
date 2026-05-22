using Fusion;
using UnityEngine;
using Zenject;

public class GameStateNetworkView : NetworkBehaviour, IGameStateNetworkBridge
{
    [Inject(Optional = true)] private IGameStateSubsystem _gameState;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public GameplayPhase CurrentPhase { get; set; }
    [Networked] public TickTimer PhaseTimer { get; set; }
    [Networked] public int RoundNumber { get; set; }
    [Networked] public float MatchElapsed { get; set; }
    [Networked] public PlayerRef CurrentCombatActor { get; set; }
    [Networked] public NetworkBool IsMatchOver { get; set; }
    // Capacity 4: index 0 unused; PlayerId is 1-based (slots 1-4)
    [Networked, Capacity(4)] public NetworkArray<NetworkBool> PlayerReady => default;

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
            var ctx = FindObjectOfType<SceneContext>();
            if (ctx != null)
            {
                _gameState = ctx.Container.Resolve<IGameStateSubsystem>();
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
        }

        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _gameState?.RegisterNetworkBridge(null);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (IsMatchOver) return;

        MatchElapsed += Runner.DeltaTime;

        if (MatchElapsed >= _matchTimeLimit)
        {
            TransitionTo(GameplayPhase.GameOver);
            return;
        }

        if (CurrentPhase == GameplayPhase.StartPhase && AreAllPlayersReady())
        {
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
                TransitionTo(GameplayPhase.CombatPhase);
                break;
            case GameplayPhase.DrawPhase:
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
                break;
            case GameplayPhase.DrawPhase:
                PhaseTimer = TickTimer.CreateFromSeconds(Runner, _drawPhaseDuration);
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
            if (slot < 1 || slot >= PlayerReady.Length) return false;
            if (!PlayerReady.Get(slot)) return false;
        }
        return true;
    }

    // ── IGameStateNetworkBridge ──────────────────────────────────────────

    public void SendPhaseTransitionRpc(GameplayPhase phase)
        => Rpc_RequestPhaseTransition(phase);

    public void SendSetReadyRpc(bool ready)
        => Rpc_SetReady(ready);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_SetReady(bool ready, RpcInfo info = default)
    {
        if (_gameState == null) return;

        // Only accept ready=true when AcceptsReadyInput; always accept ready=false (unless locked)
        if (ready && !_gameState.AcceptsReadyInput)
        {
            _logger?.Log($"[GameStateNetworkView] Rpc_SetReady(true) rejected — AcceptsReadyInput=false for phase {CurrentPhase}.");
            return;
        }

        var sender = info.Source;
        int slotIndex = sender.PlayerId; // PlayerId is 1-based; NetworkArray indices match
        if (slotIndex < 1 || slotIndex >= PlayerReady.Length) return;

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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestPhaseTransition(GameplayPhase phase)
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
}
