using System.Collections.Generic;
using Core.GDS;
using Fusion;
using UnityEngine;
using Zenject;

public class UnitNetworkView : NetworkBehaviour
{
    [Inject(Optional = true)] private IUnitSubsystem _unitSubsystem;
    [Inject(Optional = true)] private ICardLoadingManagerSubsystem _cardLoading;
    [Inject(Optional = true)] private IBoardSubsystem _boardSubsystem;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public NetworkString<_32> UnitId { get; set; }
    [Networked] public NetworkString<_32> BaseCardId { get; set; }
    [Networked] public int CurrentHP { get; set; }
    [Networked] public int MaxHP { get; set; }
    [Networked] public float Speed { get; set; }
    [Networked] public int DeathAnchor { get; set; }
    [Networked] public int MoveRange { get; set; }
    [Networked] public int NormalAttackDamage { get; set; }
    [Networked] public NetworkString<_16> Faction { get; set; }
    [Networked] public NetworkBool IsPersistent { get; set; }
    [Networked] public int GrowthStacks { get; set; }
    [Networked] public int PositionP { get; set; }
    [Networked] public int PositionQ { get; set; }

    [Networked, Capacity(8)] public NetworkArray<NetworkString<_32>> StatusEffectIds { get; }
    [Networked, Capacity(8)] public NetworkArray<int> StatusEffectDurations { get; }
    [Networked, Capacity(8)] public NetworkArray<int> StatusEffectOwners { get; }
    [Networked] public int StatusEffectCount { get; set; }

    [Networked, Capacity(4)] public NetworkArray<NetworkString<_32>> SkillIds { get; }
    [Networked, Capacity(4)] public NetworkArray<int> SkillCooldowns { get; }
    [Networked, Capacity(4)] public NetworkArray<NetworkBool> SkillOneTimeDisabled { get; }
    [Networked] public int SkillCount { get; set; }

    [Networked] public NetworkBool HasActedThisTurn { get; set; }
    [Networked] public NetworkBool HasMovedThisTurn { get; set; }

    [SerializeField] private Transform _meshRoot;
    private bool _meshApplied;
    private bool _presentationPlaced; // true once transform position has been set at least once
    private HexCoord _lastPresentationCoord = HexCoord.Invalid;

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        if (_unitSubsystem == null)
        {
            var ctx = FindFirstObjectByType<SceneContext>();
            _unitSubsystem = ctx?.Container.Resolve<IUnitSubsystem>();
            _cardLoading = ctx?.Container.Resolve<ICardLoadingManagerSubsystem>();
            _boardSubsystem = ctx?.Container.Resolve<IBoardSubsystem>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        TryPlacePresentation(facingCenter: true);
        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _unitSubsystem?.OnUnitDestroyed(Object.Id);
    }

    public void ServerInitializeFromCard(PlayerRef owner, string baseCardId, string[] equipSpellIds, HexCoord deployPosition)
    {
        if (!Object.HasStateAuthority) return;

        Owner = owner;
        BaseCardId = baseCardId;
        UnitId = Object.Id.ToString();
        PositionP = deployPosition.P;
        PositionQ = deployPosition.Q;
        HasActedThisTurn = false;
        HasMovedThisTurn = false;
        GrowthStacks = 0;
        IsPersistent = false;

        if (_cardLoading != null && _cardLoading.TryGetCardData(baseCardId, out CardData cardData))
        {
            CurrentHP = cardData.hp > 0 ? cardData.hp : 50;
            MaxHP = CurrentHP;
            Speed = cardData.speed > 0 ? cardData.speed : 3f;
            DeathAnchor = cardData.death_anchor;
            MoveRange = 1;
            NormalAttackDamage = cardData.n_atk_dmg > 0 ? cardData.n_atk_dmg : 10;
            Faction = cardData.faction ?? "";
        }
        else
        {
            CurrentHP = 50;
            MaxHP = 50;
            Speed = 3f;
            DeathAnchor = 5;
            MoveRange = 1;
            NormalAttackDamage = 10;
            Faction = "";
        }

        int skillIndex = 0;

        if (_cardLoading != null && _cardLoading.TryGetCardData(baseCardId, out CardData baseData)
            && !string.IsNullOrEmpty(baseData.grants_skill))
        {
            SkillIds.Set(skillIndex, baseData.grants_skill);
            SkillCooldowns.Set(skillIndex, 0);
            SkillOneTimeDisabled.Set(skillIndex, false);
            skillIndex++;
        }

        if (equipSpellIds != null)
        {
            foreach (var spellId in equipSpellIds)
            {
                if (string.IsNullOrEmpty(spellId) || skillIndex >= 4) break;

                if (_cardLoading != null && _cardLoading.TryGetCardData(spellId, out CardData spellData)
                    && spellData.grants_skills != null)
                {
                    foreach (var grantedSkill in spellData.grants_skills)
                    {
                        if (skillIndex >= 4) break;
                        SkillIds.Set(skillIndex, grantedSkill);
                        SkillCooldowns.Set(skillIndex, 0);
                        SkillOneTimeDisabled.Set(skillIndex, false);
                        skillIndex++;
                    }
                }
            }
        }

        SkillCount = skillIndex;
        StatusEffectCount = 0;

        _logger?.Log("LOG_UNITNETWORKVIEW", nameof(UnitNetworkView), $"Initialized unit {UnitId} for {owner}: base={baseCardId}, HP={CurrentHP}, Speed={Speed}, Skills={SkillCount}");
        PushState();
    }

    public void ServerMoveTo(HexCoord destination)
    {
        if (!Object.HasStateAuthority) return;
        PositionP = destination.P;
        PositionQ = destination.Q;
        HasMovedThisTurn = true;
    }

    public void ServerApplyDamage(int amount)
    {
        if (!Object.HasStateAuthority) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
    }

    public void ServerHeal(int amount)
    {
        if (!Object.HasStateAuthority) return;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    public void ServerAddGrowthStack(int count = 1)
    {
        if (!Object.HasStateAuthority) return;
        GrowthStacks += count;
    }

    public void ServerResetTurnFlags()
    {
        if (!Object.HasStateAuthority) return;
        HasActedThisTurn = false;
        HasMovedThisTurn = false;
    }

    public void ServerResetOneTimeFlags()
    {
        if (!Object.HasStateAuthority) return;
        for (int i = 0; i < SkillCount; i++)
            SkillOneTimeDisabled.Set(i, false);
    }

    public void ServerTickCooldowns()
    {
        if (!Object.HasStateAuthority) return;
        for (int i = 0; i < SkillCount; i++)
        {
            int cd = SkillCooldowns.Get(i);
            if (cd > 0) SkillCooldowns.Set(i, cd - 1);
        }
    }

    public void ServerAddStatus(string statusId, int duration, PlayerRef statusOwner)
    {
        if (!Object.HasStateAuthority) return;
        if (StatusEffectCount >= 8) return;

        StatusEffectIds.Set(StatusEffectCount, statusId);
        StatusEffectDurations.Set(StatusEffectCount, duration);
        StatusEffectOwners.Set(StatusEffectCount, statusOwner.PlayerId);
        StatusEffectCount++;
    }

    public void ServerEvolve(string nextForm, CardData evolvedData)
    {
        if (!Object.HasStateAuthority) return;
        BaseCardId = nextForm;
        MaxHP = evolvedData.hp > 0 ? evolvedData.hp : MaxHP;
        CurrentHP = MaxHP;
        Speed = evolvedData.speed > 0 ? evolvedData.speed : Speed;
        NormalAttackDamage = evolvedData.n_atk_dmg > 0 ? evolvedData.n_atk_dmg : NormalAttackDamage;
        GrowthStacks = 0;
    }

    public void ServerRemoveStatus(string statusId)
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < StatusEffectCount; i++)
        {
            if (StatusEffectIds.Get(i).ToString() == statusId)
            {
                for (int j = i; j < StatusEffectCount - 1; j++)
                {
                    StatusEffectIds.Set(j, StatusEffectIds.Get(j + 1));
                    StatusEffectDurations.Set(j, StatusEffectDurations.Get(j + 1));
                    StatusEffectOwners.Set(j, StatusEffectOwners.Get(j + 1));
                }
                StatusEffectCount--;
                StatusEffectIds.Set(StatusEffectCount, string.Empty);
                StatusEffectDurations.Set(StatusEffectCount, 0);
                StatusEffectOwners.Set(StatusEffectCount, 0);
                return;
            }
        }
    }

    public override void Render()
    {
        if (!_meshApplied && Owner != PlayerRef.None && _meshRoot != null)
        {
            var coordinator = GameplayNetworkCoordinator.Instance;
            if (coordinator != null)
            {
                int playerIndex = coordinator.GetPlayerIndex(Owner);
                var config = coordinator.GetPlayerPieceConfig(playerIndex);
                if (config?.Mesh != null)
                {
                    var meshFilter = _meshRoot.GetComponent<MeshFilter>();
                    if (meshFilter != null) meshFilter.mesh = config.Mesh;
                    var meshRenderer = _meshRoot.GetComponent<MeshRenderer>();
                    if (meshRenderer != null && config.Materials != null && config.Materials.Length > 0)
                        meshRenderer.materials = config.Materials;
                    _meshApplied = true;
                }
            }
        }

        // Retry initial placement every Render() until the board is ready.
        if (!_presentationPlaced)
            TryPlacePresentation(facingCenter: true);

        if (_changeDetector == null) return;
        bool positionChanged = false;
        bool anyChanged = false;
        foreach (var prop in _changeDetector.DetectChanges(this))
        {
            anyChanged = true;
            if (prop == nameof(PositionP) || prop == nameof(PositionQ))
                positionChanged = true;
        }
        if (!anyChanged) return;

        if (positionChanged)
            TryPlacePresentation(facingCenter: false);

        PushState();
    }

    private void TryPlacePresentation(bool facingCenter)
    {
        if (_boardSubsystem == null) return;

        var coord = new HexCoord(PositionP, PositionQ);
        Vector3 worldPos = _boardSubsystem.GetWorldPosition(coord);
        if (worldPos == Vector3.zero) return; // board not ready yet

        // Keep the unit's Y from its prefab (avoids sinking into or floating above the tile).
        transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);

        if (facingCenter || !_lastPresentationCoord.IsValid)
        {
            // Derive forward from the axis between the two deploy areas so each player faces the opponent.
            Vector3 point1 = _boardSubsystem.GetWorldPosition(new HexCoord(PositionP, PositionQ));
            Vector3 point2 = _boardSubsystem.GetWorldPosition(new HexCoord(0, 0)); 

            var dir = point2 - point1;

            _logger.Log("LOG_UNITNETWORKVIEW", nameof(UnitNetworkView), $"Placing unit {UnitId} at {coord} facing {(facingCenter ? "center" : "default direction")}, dir={dir}, point1={point1}, point2={point2}");
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-dir.normalized);
        }
        else
        {
            // Face the direction of travel.
            Vector3 prevPos = _boardSubsystem.GetWorldPosition(_lastPresentationCoord);
            Vector3 dir = new Vector3(worldPos.x - prevPos.x, 0f, worldPos.z - prevPos.z);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        _lastPresentationCoord = coord;
        _presentationPlaced = true;
    }

    private void PushState()
    {
        if (_unitSubsystem == null) return;

        var statusEffects = new List<StatusSlot>();
        for (int i = 0; i < StatusEffectCount; i++)
        {
            statusEffects.Add(new StatusSlot
            {
                StatusId = StatusEffectIds.Get(i).ToString(),
                DurationRemaining = StatusEffectDurations.Get(i),
                Owner = PlayerRef.FromEncoded(StatusEffectOwners.Get(i))
            });
        }

        _unitSubsystem.OnUnitPublicStateReceived(new UnitPublicData
        {
            UnitId = Object.Id,
            Owner = Owner,
            Position = new HexCoord(PositionP, PositionQ),
            CurrentHP = CurrentHP,
            MaxHP = MaxHP,
            Speed = Speed,
            DeathAnchor = DeathAnchor,
            IsPersistent = IsPersistent,
            GrowthStacks = GrowthStacks,
            StatusEffects = statusEffects,
            BaseCardId = BaseCardId.ToString(),
            MoveRange = MoveRange,
            NormalAttackDamage = NormalAttackDamage,
        });

        // Private skill state: only push to the owning client (AoI enforcement).
        if (Runner != null && Owner == Runner.LocalPlayer)
        {
            var skills = new List<SkillSlot>();
            for (int i = 0; i < SkillCount; i++)
            {
                skills.Add(new SkillSlot
                {
                    SkillId = SkillIds.Get(i).ToString(),
                    CurrentCooldown = SkillCooldowns.Get(i),
                    IsOneTimeDisabled = SkillOneTimeDisabled.Get(i)
                });
            }

            skills.Add(new SkillSlot { SkillId = "move", CurrentCooldown = 0, IsOneTimeDisabled = false });
            skills.Add(new SkillSlot { SkillId = "n_atk", CurrentCooldown = 0, IsOneTimeDisabled = false });

            _unitSubsystem.OnUnitPrivateStateReceived(new UnitPrivateData
            {
                UnitId = Object.Id,
                Owner = Owner,
                Skills = skills
            });
        }
    }
}
