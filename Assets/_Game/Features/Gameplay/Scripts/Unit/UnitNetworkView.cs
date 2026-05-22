using System.Collections.Generic;
using Core.GDS;
using Fusion;
using UnityEngine;
using Zenject;

public class UnitNetworkView : NetworkBehaviour
{
    [Inject(Optional = true)] private IUnitSubsystem _unitSubsystem;
    [Inject(Optional = true)] private ICardLoadingManagerSubsystem _cardLoading;
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

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        if (_unitSubsystem == null)
        {
            var ctx = FindObjectOfType<SceneContext>();
            _unitSubsystem = ctx?.Container.Resolve<IUnitSubsystem>();
            _cardLoading = ctx?.Container.Resolve<ICardLoadingManagerSubsystem>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
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
            MoveRange = 2;
            NormalAttackDamage = cardData.n_atk_dmg > 0 ? cardData.n_atk_dmg : 10;
            Faction = cardData.faction ?? "";
        }
        else
        {
            CurrentHP = 50;
            MaxHP = 50;
            Speed = 3f;
            DeathAnchor = 5;
            MoveRange = 2;
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

        _logger?.Log($"[UnitNetworkView] Initialized unit {UnitId} for {owner}: base={baseCardId}, HP={CurrentHP}, Speed={Speed}, Skills={SkillCount}");
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
        if (_changeDetector == null) return;
        foreach (var _ in _changeDetector.DetectChanges(this))
        {
            PushState();
            break;
        }
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
            StatusEffects = statusEffects
        });
    }
}
