using System.Collections.Generic;
using System.Linq;
using Core.GDS;
using Fusion;
using UnityEngine;
using Zenject;

public class CombatNetworkView : NetworkBehaviour, ICombatNetworkBridge
{
    [Inject(Optional = true)] private ICombatSubsystem _combatSubsystem;
    [Inject(Optional = true)] private IUnitSubsystem _unitSubsystem;
    [Inject(Optional = true)] private IBoardSubsystem _boardSubsystem;
    [Inject(Optional = true)] private IDamagePipelineSubsystem _damagePipeline;
    [Inject(Optional = true)] private ITileEffectSubsystem _tileEffectSubsystem;
    [Inject(Optional = true)] private ICardLoadingManagerSubsystem _cardLoading;
    [Inject(Optional = true)] private IBehaviorRegistrySubsystem _behaviorRegistry;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked, Capacity(20)] public NetworkArray<NetworkString<_32>> ActionQueue { get; }
    [Networked] public int QueueCount { get; set; }
    [Networked] public int CurrentIndex { get; set; }
    [Networked] public NetworkBool IsCombatActive { get; set; }
    [Networked] public NetworkBool CurrentActorHasMoved { get; set; }
    [Networked] public NetworkBool CurrentActorHasActed { get; set; }
    [Networked] public TickTimer TurnTimer { get; set; }

    [SerializeField] private float _turnDuration = 30f;

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        if (_combatSubsystem == null)
        {
            var ctx = FindFirstObjectByType<SceneContext>();
            _combatSubsystem = ctx?.Container.Resolve<ICombatSubsystem>();
            _unitSubsystem = ctx?.Container.Resolve<IUnitSubsystem>();
            _boardSubsystem = ctx?.Container.Resolve<IBoardSubsystem>();
            _damagePipeline = ctx?.Container.Resolve<IDamagePipelineSubsystem>();
            _tileEffectSubsystem = ctx?.Container.Resolve<ITileEffectSubsystem>();
            _cardLoading = ctx?.Container.Resolve<ICardLoadingManagerSubsystem>();
            _behaviorRegistry = ctx?.Container.Resolve<IBehaviorRegistrySubsystem>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _combatSubsystem?.RegisterNetworkBridge(this);

        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _combatSubsystem?.RegisterNetworkBridge(null);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!IsCombatActive) return;

        if (TurnTimer.Expired(Runner))
        {
            _logger?.Log("[Combat] Turn timer expired, auto-ending turn.");
            ServerEndTurn();
        }
    }

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var _ in _changeDetector.DetectChanges(this))
        {
            PushState();
            break;
        }
    }

    // ── Server-side API ──────────────────────────────────────────────────

    public void ServerStartCombatPhase()
    {
        if (!Object.HasStateAuthority) return;

        BuildActionQueue();

        if (QueueCount == 0)
        {
            _logger?.Log("[Combat] No units to act. Ending combat immediately.");
            ServerEndCombatPhase();
            return;
        }

        IsCombatActive = true;
        CurrentIndex = 0;
        StartCurrentActorTurn();

        _logger?.Log($"[Combat] Combat phase started. Queue size={QueueCount}.");
    }

    public void ServerEndCombatPhase()
    {
        if (!Object.HasStateAuthority) return;

        IsCombatActive = false;
        TurnTimer = TickTimer.None;

        ResetOneTimeFlagsOnPersistentUnits();
        CheckBoardClear();

        _logger?.Log("[Combat] Combat phase ended.");

        var coordinator = GameplayNetworkCoordinator.Instance;
        coordinator?.GameStateView?.ServerTransitionToDrawPhase();
    }

    // ── F4.1: Action queue build ─────────────────────────────────────────

    private void BuildActionQueue()
    {
        var allUnitIds = _unitSubsystem?.AllUnits;
        if (allUnitIds == null || allUnitIds.Count == 0)
        {
            QueueCount = 0;
            return;
        }

        var unitEntries = new List<(string id, float speed, int hp)>();

        foreach (var netId in allUnitIds)
        {
            string id = netId.ToString();
            if (TryGetUnitData(id, out UnitPublicData data) && data.CurrentHP > 0)
            {
                unitEntries.Add((id, data.Speed, data.CurrentHP));
            }
        }

        // Sort: Speed desc → HP asc → random coin toss
        var rng = new System.Random(Runner.Tick);
        unitEntries.Sort((a, b) =>
        {
            int cmp = b.speed.CompareTo(a.speed);
            if (cmp != 0) return cmp;
            cmp = a.hp.CompareTo(b.hp);
            if (cmp != 0) return cmp;
            return rng.Next(-1, 2);
        });

        QueueCount = Mathf.Min(unitEntries.Count, 20);
        for (int i = 0; i < QueueCount; i++)
            ActionQueue.Set(i, unitEntries[i].id);
    }

    public void ServerAppendToQueue(string unitId)
    {
        if (!Object.HasStateAuthority) return;
        if (QueueCount >= 20) return;

        ActionQueue.Set(QueueCount, unitId);
        QueueCount++;
    }

    // ── F4.3: Unit turn cycle ────────────────────────────────────────────

    private void StartCurrentActorTurn()
    {
        if (CurrentIndex >= QueueCount)
        {
            ServerEndCombatPhase();
            return;
        }

        string actorId = ActionQueue.Get(CurrentIndex).ToString();

        if (!TryGetUnitData(actorId, out UnitPublicData data) || data.CurrentHP <= 0)
        {
            AdvanceTurn();
            return;
        }

        // Check if rooted — can still act but not move
        CurrentActorHasMoved = HasStatus(data, "rooted");
        CurrentActorHasActed = false;
        TurnTimer = TickTimer.CreateFromSeconds(Runner, _turnDuration);

        // Tick cooldowns for this unit
        var unitView = FindUnitNetworkView(actorId);
        unitView?.ServerTickCooldowns();
        unitView?.ServerResetTurnFlags();

        // Apply start-of-turn status effects (e.g., burning damage)
        ApplyStartOfTurnEffects(actorId, data);

        // Re-check HP after start-of-turn effects
        if (TryGetUnitData(actorId, out data) && data.CurrentHP <= 0)
        {
            ProcessDeath(actorId);
            AdvanceTurn();
            return;
        }

        _logger?.Log($"[Combat] Turn started for unit {actorId} (Speed={data.Speed}).");
    }

    private void AdvanceTurn()
    {
        CurrentIndex++;

        if (CurrentIndex >= QueueCount)
        {
            ServerEndCombatPhase();
            return;
        }

        StartCurrentActorTurn();
    }

    private void ServerEndTurn()
    {
        if (!Object.HasStateAuthority) return;
        if (!IsCombatActive) return;

        TurnTimer = TickTimer.None;
        AdvanceTurn();
    }

    // ── F4.4: Movement & pathfinding ─────────────────────────────────────

    private void ServerMove(string unitId, HexCoord destination)
    {
        if (!ValidateIsCurrentActor(unitId)) return;
        if (CurrentActorHasMoved)
        {
            _logger?.LogWarning($"[Combat] Unit {unitId} already moved this turn.");
            return;
        }

        if (!TryGetUnitData(unitId, out UnitPublicData data)) return;

        if (!_boardSubsystem.IsEmpty(destination))
        {
            _logger?.LogWarning($"[Combat] Destination {destination} is occupied.");
            return;
        }

        var path = _boardSubsystem.FindPath(data.Position, destination, 1);
        if (path == null || path.Count == 0)
        {
            _logger?.LogWarning($"[Combat] No valid path from {data.Position} to {destination} within range 1.");
            return;
        }

        var unitView = FindUnitNetworkView(unitId);
        if (unitView == null) return;

        _boardSubsystem.SetOccupant(data.Position, null);
        unitView.ServerMoveTo(destination);
        _boardSubsystem.SetOccupant(destination, unitId);
        CurrentActorHasMoved = true;

        _logger?.Log($"[Combat] Unit {unitId} moved from {data.Position} to {destination}.");

        CheckAutoEndTurn();
    }

    // ── F4.5/F4.7: Normal attack ────────────────────────────────────────

    private void ServerNormalAttack(string unitId, HexCoord target)
    {
        if (!ValidateIsCurrentActor(unitId)) return;
        if (CurrentActorHasActed)
        {
            _logger?.LogWarning($"[Combat] Unit {unitId} already acted this turn.");
            return;
        }

        if (!TryGetUnitData(unitId, out UnitPublicData attackerData)) return;

        // Validate range (normal attack range = 1)
        if (_boardSubsystem.Distance(attackerData.Position, target) > 1)
        {
            _logger?.LogWarning($"[Combat] Target {target} out of normal attack range.");
            return;
        }

        string targetUnitId = FindUnitAtPosition(target);
        if (string.IsNullOrEmpty(targetUnitId))
        {
            _logger?.LogWarning($"[Combat] No unit at {target} to attack.");
            return;
        }

        if (!TryGetUnitData(targetUnitId, out UnitPublicData targetData)) return;

        // F4.11: Friendly-fire check
        if (targetData.Owner == attackerData.Owner)
        {
            _logger?.LogWarning($"[Combat] Cannot normal attack allied unit.");
            return;
        }

        var unitView = FindUnitNetworkView(unitId);
        int rawDamage = unitView != null ? unitView.NormalAttackDamage : 10;

        var context = new DamageContext
        {
            SourceUnitId = unitId,
            TargetUnitId = targetUnitId,
            TargetPosition = target,
            RawAmount = rawDamage,
            SourceSkillId = null,
            IsAOE = false
        };

        int finalDamage = _damagePipeline.Resolve(context);

        if (finalDamage > 0)
        {
            var targetView = FindUnitNetworkView(targetUnitId);
            targetView?.ServerApplyDamage(finalDamage);
            _logger?.Log($"[Combat] Unit {unitId} attacked {targetUnitId} for {finalDamage} damage (raw={rawDamage}).");

            if (TryGetUnitData(targetUnitId, out var postData) && postData.CurrentHP <= 0)
                ProcessDeath(targetUnitId);
        }

        CurrentActorHasActed = true;
        var attackerView = FindUnitNetworkView(unitId);
        if (attackerView != null) attackerView.HasActedThisTurn = true;

        CheckAutoEndTurn();
    }

    // ── F4.5/F4.9: Skill execution ──────────────────────────────────────

    private void ServerSkill(string unitId, string skillId, HexCoord target)
    {
        if (!ValidateIsCurrentActor(unitId)) return;
        if (CurrentActorHasActed)
        {
            _logger?.LogWarning($"[Combat] Unit {unitId} already acted this turn.");
            return;
        }

        if (!TryGetUnitData(unitId, out UnitPublicData casterData)) return;

        var unitView = FindUnitNetworkView(unitId);
        if (unitView == null) return;

        int skillIndex = FindSkillIndex(unitView, skillId);
        if (skillIndex < 0)
        {
            _logger?.LogWarning($"[Combat] Skill '{skillId}' not found on unit {unitId}.");
            return;
        }

        // F4.9: Check cooldown
        int currentCooldown = unitView.SkillCooldowns.Get(skillIndex);
        if (currentCooldown > 0)
        {
            _logger?.LogWarning($"[Combat] Skill '{skillId}' on cooldown ({currentCooldown} turns remaining).");
            return;
        }

        // F4.9: Check one-time disabled
        if (unitView.SkillOneTimeDisabled.Get(skillIndex))
        {
            _logger?.LogWarning($"[Combat] Skill '{skillId}' is permanently disabled (one-time used).");
            return;
        }

        // Validate range
        if (!_cardLoading.TryGetSkillData(skillId, out SkillData skillData))
        {
            _logger?.LogWarning($"[Combat] No skill data for '{skillId}'.");
            return;
        }

        // target_condition 0 means self-only: target must be caster's tile
        if (skillData.target_condition == 0)
            target = casterData.Position;

        // F4.11: Validate targeting
        if (!ValidateSkillTarget(casterData, target, skillData))
        {
            _logger?.LogWarning($"[Combat] Invalid target {target} for skill '{skillId}'.");
            return;
        }

        // Apply cooldown
        int cdValue = skillData.cooldown > 0 ? skillData.cooldown : 3;
        unitView.SkillCooldowns.Set(skillIndex, cdValue);

        // F4.9: one-time disabling
        if (skillData.one_time)
            unitView.SkillOneTimeDisabled.Set(skillIndex, true);

        // Execute skill behavior
        ExecuteSkillBehavior(unitId, skillId, target, casterData, skillData);

        CurrentActorHasActed = true;
        unitView.HasActedThisTurn = true;

        _logger?.Log($"[Combat] Unit {unitId} used skill '{skillId}' at {target}.");

        // Check deaths after skill execution
        CheckAllUnitDeaths();
        CheckAutoEndTurn();
    }

    private void ExecuteSkillBehavior(string casterId, string skillId, HexCoord target, UnitPublicData casterData, SkillData skillData)
    {
        if (string.IsNullOrEmpty(skillData.skill_behavior_id)) return;

        if (!_behaviorRegistry.TryGetSkillBehavior(skillData.skill_behavior_id, out var behaviorSO)) return;

        var behavior = behaviorSO as CombatSkillBehaviorSO;
        if (behavior == null)
        {
            _logger?.LogWarning($"[Combat] Behavior '{skillData.skill_behavior_id}' is not a CombatSkillBehaviorSO.");
            return;
        }

        var context = new CombatSkillExecutionContext
        {
            CasterId = casterId,
            CasterData = casterData,
            Target = target,
            SkillData = skillData,
            Runner = Runner,
            UnitSubsystem = _unitSubsystem,
            BoardSubsystem = _boardSubsystem,
            DamagePipeline = _damagePipeline,
            TileEffectSubsystem = _tileEffectSubsystem,
            CardLoading = _cardLoading,
            Logger = _logger,
            CombatView = this
        };

        behavior.Execute(context);
    }

    private bool ValidateSkillTarget(UnitPublicData casterData, HexCoord target, SkillData skillData)
    {
        // Check range from skill data target_pattern
        int range = HexPatternResolver.GetRange(skillData.target_pattern);

        if (_boardSubsystem.Distance(casterData.Position, target) > range)
            return false;

        int mask = skillData.target_condition;
        if (mask == 0) return true; // self-only, already validated

        string targetUnitId = FindUnitAtPosition(target);
        bool hasUnit = !string.IsNullOrEmpty(targetUnitId);

        if (hasUnit && TryGetUnitData(targetUnitId, out UnitPublicData targetData))
        {
            bool isEnemy = targetData.Owner != casterData.Owner;
            if (isEnemy && (mask & 1) != 0) return true;
            if (!isEnemy && (mask & 2) != 0) return true;
        }
        else
        {
            if ((mask & 4) != 0) return true; // EmptyTile
        }

        return false;
    }

    // ── F4.7/F4.8: Start-of-turn effects (burning, etc.) ────────────────

    private void ApplyStartOfTurnEffects(string unitId, UnitPublicData data)
    {
        if (data.StatusEffects == null) return;

        var unitView = FindUnitNetworkView(unitId);
        if (unitView == null) return;

        foreach (var status in data.StatusEffects)
        {
            switch (status.StatusId)
            {
                case "burning":
                    ApplyStatusDamage(unitId, data, 10, "burning");
                    break;
                case "melting":
                    ApplyStatusDamage(unitId, data, 20, "melting");
                    break;
            }
        }

        // Tick tile effects on this unit's tile
        if (_tileEffectSubsystem != null && _tileEffectSubsystem.TryGet(data.Position, out TileEffectInstance tileEffect))
        {
            // Only apply negative tile effects to non-owners
            if (tileEffect.Owner != data.Owner)
            {
                switch (tileEffect.EffectId)
                {
                    case "Corrupted":
                        ApplyStatusDamage(unitId, data, 10, "Corrupted_tile");
                        break;
                    case "Melting":
                        ApplyStatusDamage(unitId, data, 20, "Melting_tile");
                        break;
                }
            }
        }

        // Tick status durations
        TickStatusDurations(unitView, data);

        // F4.14: Check verdant evolution
        CheckEvolution(unitId, data, unitView);
    }

    private void ApplyStatusDamage(string unitId, UnitPublicData data, int amount, string sourceId)
    {
        var context = new DamageContext
        {
            SourceUnitId = null,
            TargetUnitId = unitId,
            TargetPosition = data.Position,
            RawAmount = amount,
            SourceSkillId = sourceId,
            IsAOE = false
        };

        int finalDamage = _damagePipeline.Resolve(context);
        if (finalDamage > 0)
        {
            var targetView = FindUnitNetworkView(unitId);
            targetView?.ServerApplyDamage(finalDamage);
            _logger?.Log($"[Combat] Status '{sourceId}' dealt {finalDamage} to unit {unitId}.");
        }
    }

    private void TickStatusDurations(UnitNetworkView unitView, UnitPublicData data)
    {
        if (data.StatusEffects == null) return;

        for (int i = unitView.StatusEffectCount - 1; i >= 0; i--)
        {
            int dur = unitView.StatusEffectDurations.Get(i);
            if (dur > 0)
            {
                dur--;
                unitView.StatusEffectDurations.Set(i, dur);
                if (dur <= 0)
                {
                    string removedId = unitView.StatusEffectIds.Get(i).ToString();
                    unitView.ServerRemoveStatus(removedId);
                    _logger?.Log($"[Combat] Status '{removedId}' expired on unit {data.UnitId}.");
                }
            }
        }
    }

    // ── F4.12: Death & DeathAnchor ───────────────────────────────────────

    private void ProcessDeath(string unitId)
    {
        if (!TryGetUnitData(unitId, out UnitPublicData data)) return;

        int deathAnchor = data.DeathAnchor;
        PlayerRef owner = data.Owner;

        // Remove occupant from board
        _boardSubsystem.SetOccupant(data.Position, null);

        // Despawn the unit
        var unitView = FindUnitNetworkView(unitId);
        if (unitView != null && unitView.Object != null)
            Runner.Despawn(unitView.Object);

        _logger?.Log($"[Combat] Unit {unitId} died. DeathAnchor={deathAnchor} applied to player {owner}.");

        // Apply DeathAnchor damage to owner's HP
        if (deathAnchor > 0)
        {
            var coordinator = GameplayNetworkCoordinator.Instance;
            var pczView = coordinator?.GetPlayerCardZoneView(owner);
            pczView?.ServerApplyDamage(deathAnchor);

            // Check elimination
            if (pczView != null && pczView.HP <= 0)
            {
                _logger?.Log($"[Combat] Player {owner} eliminated!");
                CheckWinCondition();
            }
        }
    }

    private void CheckAllUnitDeaths()
    {
        var allUnits = _unitSubsystem?.AllUnits;
        if (allUnits == null) return;

        var deadUnits = new List<string>();
        foreach (var netId in allUnits)
        {
            string id = netId.ToString();
            if (TryGetUnitData(id, out UnitPublicData data) && data.CurrentHP <= 0)
                deadUnits.Add(id);
        }

        foreach (var id in deadUnits)
            ProcessDeath(id);
    }

    // ── F4.13: Persistent units ──────────────────────────────────────────
    // Persistent units (`IsPersistent=true`) survive board clear.
    // They are excluded from discard logic and their cooldowns persist.

    // ── F4.14: Verdant evolution ─────────────────────────────────────────

    private void CheckEvolution(string unitId, UnitPublicData data, UnitNetworkView unitView)
    {
        if (data.GrowthStacks < 4) return;

        string baseCardId = unitView.BaseCardId.ToString();
        string nextForm = GetEvolutionTarget(baseCardId);

        if (string.IsNullOrEmpty(nextForm)) return;

        if (_cardLoading != null && _cardLoading.TryGetCardData(nextForm, out CardData evolvedData))
        {
            unitView.BaseCardId = nextForm;
            unitView.MaxHP = evolvedData.hp > 0 ? evolvedData.hp : unitView.MaxHP;
            unitView.CurrentHP = unitView.MaxHP;
            unitView.Speed = evolvedData.speed > 0 ? evolvedData.speed : unitView.Speed;
            unitView.NormalAttackDamage = evolvedData.n_atk_dmg > 0 ? evolvedData.n_atk_dmg : unitView.NormalAttackDamage;
            unitView.GrowthStacks = 0;

            _logger?.Log($"[Combat] Unit {unitId} evolved from '{baseCardId}' to '{nextForm}'.");
        }
    }

    private string GetEvolutionTarget(string currentForm)
    {
        return currentForm switch
        {
            "Seedling" => "Sapling",
            "Sapling" => "YoungTreant",
            "YoungTreant" => "ThornColossus",
            _ => null
        };
    }

    // ── F4.15: Board clear ───────────────────────────────────────────────

    private void CheckBoardClear()
    {
        var allUnits = _unitSubsystem?.AllUnits;
        if (allUnits == null || allUnits.Count == 0) return;

        var playerUnits = new Dictionary<int, List<string>>();
        int playersWithUnits = 0;

        foreach (var netId in allUnits)
        {
            string id = netId.ToString();
            if (!TryGetUnitData(id, out UnitPublicData data)) continue;
            if (data.CurrentHP <= 0) continue;
            if (data.IsPersistent) continue; // Persistent units don't count

            int ownerKey = data.Owner.RawEncoded;
            if (!playerUnits.ContainsKey(ownerKey))
            {
                playerUnits[ownerKey] = new List<string>();
                playersWithUnits++;
            }
            playerUnits[ownerKey].Add(id);
        }

        // Board clear triggers when only one player has non-persistent units
        if (playersWithUnits > 1) return;

        _logger?.Log("[Combat] Board clear triggered — only one player has non-persistent units remaining.");

        // Destroy all non-persistent units
        foreach (var netId in allUnits.ToList())
        {
            string id = netId.ToString();
            if (!TryGetUnitData(id, out UnitPublicData data)) continue;
            if (data.CurrentHP <= 0) continue;
            if (data.IsPersistent) continue;

            _boardSubsystem.SetOccupant(data.Position, null);
            var view = FindUnitNetworkView(id);
            if (view != null && view.Object != null)
                Runner.Despawn(view.Object);
        }

        // Force-wipe deploy areas (tile effects on deploy tiles)
        ClearDeployAreaEffects();
    }

    private void ClearDeployAreaEffects()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return;

        foreach (var player in coordinator.GetAllPlayers())
        {
            var deployCoord = _boardSubsystem.GetDeployArea(player);
            if (deployCoord.IsValid && _tileEffectSubsystem.TryGet(deployCoord, out _))
            {
                _tileEffectSubsystem.OnEffectRemovedAt(deployCoord);
            }
        }
    }

    // ── Win condition check ──────────────────────────────────────────────

    private void CheckWinCondition()
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return;

        PlayerRef? winner = null;
        int alivePlayers = 0;

        foreach (var player in coordinator.GetAllPlayers())
        {
            var pczView = coordinator.GetPlayerCardZoneView(player);
            if (pczView != null && pczView.HP > 0)
            {
                alivePlayers++;
                winner = player;
            }
        }

        if (alivePlayers <= 1)
        {
            coordinator.GameStateView?.ServerTransitionToPhase(GameplayPhase.GameOver);
        }
    }

    // ── ICombatNetworkBridge ─────────────────────────────────────────────

    public void SendMoveRpc(NetworkId unit, HexCoord destination)
        => Rpc_RequestMove(unit.ToString(), destination.P, destination.Q);

    public void SendNormalAttackRpc(NetworkId unit, HexCoord target)
        => Rpc_RequestNormalAttack(unit.ToString(), target.P, target.Q);

    public void SendSkillRpc(NetworkId unit, string skillId, HexCoord target)
        => Rpc_RequestSkill(unit.ToString(), skillId, target.P, target.Q);

    public void SendEndTurnRpc()
        => Rpc_RequestEndTurn();

    // ── RPCs (client → server) ───────────────────────────────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_RequestMove(string unitId, int p, int q)
    {
        ServerMove(unitId, new HexCoord(p, q));
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_RequestNormalAttack(string unitId, int p, int q)
    {
        ServerNormalAttack(unitId, new HexCoord(p, q));
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_RequestSkill(string unitId, string skillId, int p, int q)
    {
        ServerSkill(unitId, skillId, new HexCoord(p, q));
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_RequestEndTurn()
    {
        ServerEndTurn();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private bool TryGetUnitData(string unitId, out UnitPublicData data)
    {
        data = default;
        if (string.IsNullOrEmpty(unitId)) return false;
        if (!uint.TryParse(unitId, out uint raw)) return false;
        var netId = new NetworkId { Raw = raw };
        return _unitSubsystem != null && _unitSubsystem.TryGetPublic(netId, out data);
    }

    private bool ValidateIsCurrentActor(string unitId)
    {
        if (!IsCombatActive) return false;
        if (CurrentIndex >= QueueCount) return false;

        string currentActorId = ActionQueue.Get(CurrentIndex).ToString();
        if (currentActorId != unitId)
        {
            _logger?.LogWarning($"[Combat] Unit {unitId} is not the current actor ({currentActorId}).");
            return false;
        }
        return true;
    }

    private void CheckAutoEndTurn()
    {
        if (CurrentActorHasMoved && CurrentActorHasActed)
            ServerEndTurn();
    }

    private string FindUnitAtPosition(HexCoord position)
    {
        var allUnits = _unitSubsystem?.AllUnits;
        if (allUnits == null) return null;

        foreach (var netId in allUnits)
        {
            string id = netId.ToString();
            if (TryGetUnitData(id, out UnitPublicData data) && data.Position == position && data.CurrentHP > 0)
                return id;
        }
        return null;
    }

    private UnitNetworkView FindUnitNetworkView(string unitId)
    {
        if (string.IsNullOrEmpty(unitId) || !Runner.IsRunning) return null;

        if (uint.TryParse(unitId, out uint raw))
        {
            var netId = new NetworkId { Raw = raw };
            if (Runner.TryFindObject(netId, out var netObj))
                return netObj.GetComponent<UnitNetworkView>();
        }
        return null;
    }

    private int FindSkillIndex(UnitNetworkView unitView, string skillId)
    {
        for (int i = 0; i < unitView.SkillCount; i++)
        {
            if (unitView.SkillIds.Get(i).ToString() == skillId)
                return i;
        }
        return -1;
    }

    private bool HasStatus(UnitPublicData data, string statusId)
    {
        if (data.StatusEffects == null) return false;
        foreach (var s in data.StatusEffects)
            if (s.StatusId == statusId) return true;
        return false;
    }

    private void PushState()
    {
        if (_combatSubsystem == null) return;

        var queue = new List<CombatQueueEntry>();
        for (int i = 0; i < QueueCount; i++)
        {
            string idStr = ActionQueue.Get(i).ToString();
            if (string.IsNullOrEmpty(idStr)) continue;

            NetworkId entryNetId = default;
            if (uint.TryParse(idStr, out uint entryRaw)) entryNetId = new NetworkId { Raw = entryRaw };
            string cardId = FindUnitNetworkView(idStr)?.BaseCardId.ToString() ?? string.Empty;
            queue.Add(new CombatQueueEntry { UnitId = entryNetId, CardId = cardId });
        }

        string currentActorStr = CurrentIndex < QueueCount ? ActionQueue.Get(CurrentIndex).ToString() : string.Empty;
        NetworkId currentActorId = default;
        if (uint.TryParse(currentActorStr, out uint actorRaw)) currentActorId = new NetworkId { Raw = actorRaw };

        _combatSubsystem.OnAuthoritativeStateReceived(new CombatStateData
        {
            ActionQueue = queue,
            CurrentActor = currentActorId,
            HasMoved = CurrentActorHasMoved,
            HasActed = CurrentActorHasActed
        });
    }

    // ── F4.9: one_time flag reset for Persistent Units ───────────────────

    private void ResetOneTimeFlagsOnPersistentUnits()
    {
        var allUnits = _unitSubsystem?.AllUnits;
        if (allUnits == null) return;

        foreach (var netId in allUnits)
        {
            if (!_unitSubsystem.TryGetPublic(netId, out UnitPublicData data)) continue;
            if (!data.IsPersistent) continue;

            var unitView = FindUnitNetworkView(netId.ToString());
            unitView?.ServerResetOneTimeFlags();
        }
    }
}
