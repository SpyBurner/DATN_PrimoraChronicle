using System.Collections.Generic;
using Core.GDS;
using Fusion;
using UnityEngine;
using Zenject;

public class FusionNetworkView : NetworkBehaviour, IFusionNetworkBridge
{
    [Inject(Optional = true)] private IFusionSubsystem _fusionSubsystem;
    [Inject(Optional = true)] private IUnitSubsystem _unitSubsystem;
    [Inject(Optional = true)] private IBoardSubsystem _boardSubsystem;
    [Inject(Optional = true)] private IGameStateSubsystem _gameState;
    [Inject(Optional = true)] private ICardLoadingManagerSubsystem _cardLoading;
    [Inject(Optional = true)] private ITileEffectSubsystem _tileEffectSubsystem;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public NetworkBool IsConfirmed { get; set; }
    [Networked] public NetworkString<_32> DeployedUnitId { get; set; }
    [Networked] public NetworkBool HasFusedThisTurn { get; set; }

    [Networked] public NetworkString<_32> BaseCard { get; set; }
    [Networked, Capacity(4)] public NetworkArray<NetworkString<_32>> EquipSpells { get; }
    [Networked] public int EquipSpellCount { get; set; }

    private ChangeDetector _changeDetector;

    private const int MaxEquipSlots = 4;

    private void Awake()
    {
    }

    public override void Spawned()
    {
        if (_fusionSubsystem == null)
        {
            var ctx = FindFirstObjectByType<SceneContext>();
            _fusionSubsystem = ctx?.Container.Resolve<IFusionSubsystem>();
            _unitSubsystem = ctx?.Container.Resolve<IUnitSubsystem>();
            _boardSubsystem = ctx?.Container.Resolve<IBoardSubsystem>();
            _gameState = ctx?.Container.Resolve<IGameStateSubsystem>();
            _cardLoading = ctx?.Container.Resolve<ICardLoadingManagerSubsystem>();
            _tileEffectSubsystem = ctx?.Container.TryResolve<ITileEffectSubsystem>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
            _fusionSubsystem?.RegisterNetworkBridge(this);

        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
            _fusionSubsystem?.RegisterNetworkBridge(null);
    }

    public void SendConfirmFusionRpc(string baseCardId, string equipSpellsJoined)
        => Rpc_RequestConfirmFusion(baseCardId, equipSpellsJoined);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestConfirmFusion(string baseCardId, string equipSpellsJoined)
    {
        if (!Object.HasStateAuthority) return;

        if (HasFusedThisTurn)
        {
            _logger?.LogWarning($"[FusionNetworkView] Player {Object.InputAuthority} already fused this turn.");
            return;
        }

        if (_gameState != null && _gameState.Phase != GameplayPhase.MainPhase)
        {
            _logger?.LogWarning($"[FusionNetworkView] Fusion rejected: not in MainPhase (current={_gameState.Phase}).");
            return;
        }

        if (string.IsNullOrEmpty(baseCardId))
        {
            _logger?.LogWarning("[FusionNetworkView] Fusion rejected: no base card.");
            return;
        }

        if (!ValidateBaseCard(baseCardId))
        {
            _logger?.LogWarning($"[FusionNetworkView] Fusion rejected: invalid base card '{baseCardId}'.");
            return;
        }

        string[] equipIds = ParseEquipSpells(equipSpellsJoined);

        int slotsUsed = equipIds.Length;
        bool hasInnateSkill = HasInnateSkill(baseCardId);
        if (hasInnateSkill) slotsUsed++;

        if (slotsUsed > MaxEquipSlots)
        {
            _logger?.LogWarning($"[FusionNetworkView] Fusion rejected: too many slots ({slotsUsed}/{MaxEquipSlots}).");
            return;
        }

        BaseCard = baseCardId;
        EquipSpellCount = equipIds.Length;
        for (int i = 0; i < equipIds.Length; i++)
            EquipSpells.Set(i, equipIds[i]);

        SpawnUnit(Object.InputAuthority, baseCardId, equipIds);

        // Discard base card if it is a troop — champion is never discarded
        if (_cardLoading != null && _cardLoading.TryGetCardData(baseCardId, out CardData baseData)
            && baseData.type == "troop")
            DiscardFusionCardForPlayer(Object.InputAuthority, baseCardId);

        // Discard all equip spells immediately
        foreach (var equipId in equipIds)
            DiscardFusionCardForPlayer(Object.InputAuthority, equipId);

        HasFusedThisTurn = true;
        IsConfirmed = true;

        _logger?.Log($"[FusionNetworkView] Fusion confirmed for {Object.InputAuthority}: base={baseCardId}, equips={equipIds.Length}");
    }

    public void ServerAutoConfirmFusion(string championId)
    {
        if (!Object.HasStateAuthority) return;
        if (HasFusedThisTurn) return;

        BaseCard = championId;
        EquipSpellCount = 0;
        SpawnUnit(Object.InputAuthority, championId, System.Array.Empty<string>());
        HasFusedThisTurn = true;
        IsConfirmed = true;
        _logger?.Log($"[FusionNetworkView] Auto-confirmed fusion (Champion only) for {Object.InputAuthority}.");
    }

    public void ServerResetForNewTurn()
    {
        if (!Object.HasStateAuthority) return;
        HasFusedThisTurn = false;
        IsConfirmed = false;
        DeployedUnitId = string.Empty;
        BaseCard = string.Empty;
        EquipSpellCount = 0;
        for (int i = 0; i < MaxEquipSlots; i++)
            EquipSpells.Set(i, string.Empty);
    }

    public string[] GetUsedCardIds()
    {
        var cards = new List<string>();

        string baseId = BaseCard.ToString();
        if (!string.IsNullOrEmpty(baseId))
            cards.Add(baseId);

        for (int i = 0; i < EquipSpellCount; i++)
        {
            string s = EquipSpells.Get(i).ToString();
            if (!string.IsNullOrEmpty(s)) cards.Add(s);
        }

        return cards.ToArray();
    }

    private bool ValidateBaseCard(string cardId)
    {
        if (_cardLoading == null) return true;
        if (!_cardLoading.TryGetCardData(cardId, out CardData data)) return false;

        return data.type == "troop" || data.type == "champion";
    }

    private bool HasInnateSkill(string cardId)
    {
        if (_cardLoading == null) return false;
        if (!_cardLoading.TryGetCardData(cardId, out CardData data)) return false;
        return !string.IsNullOrEmpty(data.grants_skill);
    }

    private string[] ParseEquipSpells(string joined)
    {
        if (string.IsNullOrEmpty(joined)) return System.Array.Empty<string>();

        var parts = joined.Split(',');
        var valid = new List<string>();
        foreach (var part in parts)
        {
            string trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                valid.Add(trimmed);
        }
        return valid.ToArray();
    }

    private void SpawnUnit(PlayerRef owner, string baseCardId, string[] equipSpellIds)
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null)
        {
            _logger?.LogWarning("[FusionNetworkView] GameplayNetworkCoordinator not available.");
            return;
        }

        var unitPrefab = coordinator.GetUnitPrefab();
        if (!unitPrefab.IsValid)
        {
            _logger?.LogWarning("[FusionNetworkView] Unit prefab not assigned in coordinator, cannot spawn unit.");
            return;
        }

        HexCoord deployCoord = _boardSubsystem != null
            ? _boardSubsystem.GetDeployArea(owner)
            : new HexCoord(0, 0);

        ServerClearDeployArea(owner, deployCoord);

        Vector3 spawnPos = _boardSubsystem != null
            ? _boardSubsystem.GetWorldPosition(deployCoord)
            : Vector3.zero;

        var unitObj = Runner.Spawn(unitPrefab, spawnPos, Quaternion.identity, owner);
        if (unitObj == null)
        {
            _logger?.LogWarning("[FusionNetworkView] Failed to spawn unit NetworkObject.");
            return;
        }

        var unitView = unitObj.GetComponent<UnitNetworkView>();
        if (unitView != null)
        {
            unitView.ServerInitializeFromCard(owner, baseCardId, equipSpellIds, deployCoord);
        }

        DeployedUnitId = unitObj.Id.ToString();
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

    private void PushState()
    {
        if (_fusionSubsystem == null) return;

        _fusionSubsystem.OnAuthoritativeStateReceived(new FusionStateData
        {
            IsConfirmed = IsConfirmed,
            DeployedUnitId = DeployedUnitId.ToString()
        });
    }

    // ── Deploy area helpers ──────────────────────────────────────────────────

    private void ServerClearDeployArea(PlayerRef owner, HexCoord deployCoord)
    {
        if (_unitSubsystem == null) return;

        var allUnits = _unitSubsystem.AllUnits;
        if (allUnits == null) return;

        var coordinator = GameplayNetworkCoordinator.Instance;

        foreach (var netId in allUnits)
        {
            if (!_unitSubsystem.TryGetPublic(netId, out UnitPublicData data)) continue;
            if (data.Position.P != deployCoord.P || data.Position.Q != deployCoord.Q) continue;
            if (data.CurrentHP <= 0) continue;

            if (data.DeathAnchor > 0 && coordinator != null)
            {
                var pczView = coordinator.GetPlayerCardZoneView(data.Owner);
                pczView?.ServerApplyDamage(data.DeathAnchor);
            }

            _boardSubsystem?.SetOccupant(deployCoord, null);

            if (Runner.TryFindObject(netId, out var netObj))
                Runner.Despawn(netObj);

            _logger?.Log($"[FusionNetworkView] Evicted unit {netId} from deploy area of {owner}.");
            break;
        }

        _tileEffectSubsystem?.OnEffectRemovedAt(deployCoord);
    }

    private void DiscardFusionCardForPlayer(PlayerRef owner, string cardId)
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return;
        var pczView = coordinator.GetPlayerCardZoneView(owner);
        pczView?.ServerDiscardFusionCard(cardId);
    }
}
