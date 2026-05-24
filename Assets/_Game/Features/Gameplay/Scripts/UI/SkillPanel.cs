using System;
using System.Collections.Generic;
using Core.GDS;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class SkillPanel : MonoBehaviour
{
    [Inject] private readonly ICombatSubsystem _combat;
    [Inject] private readonly IUnitSubsystem _unit;
    [Inject] private readonly ITargetingSubsystem _targeting;
    [Inject] private readonly IGameStateSubsystem _gameState;
    [Inject] private readonly INetworkManagerSubsystem _network;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoading;
    [Inject] private readonly IDebugLogger _debugLogger;

    [Header("References")]
    [SerializeField] private Transform _skillSlotContainer;
    [SerializeField] private SkillSlotUI _skillSlotPrefab;
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private TMP_Text _actorNameText;

    [Header("Slot Colors")]
    [SerializeField] private Color _readyColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color _cooldownColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private Color _disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private readonly List<SkillSlotUI> _spawnedSlots = new();
    private PlayerRef _localPlayer;
    private NetworkId _currentActor;
    private NetworkId _localActorId; // last actor that belongs to the local player — kept across opponent turns
    private string _activeSkillId;

    private void Awake()
    {
        if (_skillSlotContainer == null) throw new Exception("[SkillPanel._skillSlotContainer] Not assigned in Inspector — see wiring-F4.md F4.5");
        if (_skillSlotPrefab == null) throw new Exception("[SkillPanel._skillSlotPrefab] Not assigned in Inspector — see wiring-F4.md F4.5");
        if (_endTurnButton == null) throw new Exception("[SkillPanel._endTurnButton] Not assigned in Inspector — see wiring-F4.md F4.5");
        if (_actorNameText == null) throw new Exception("[SkillPanel._actorNameText] Not assigned in Inspector — see wiring-F4.md F4.5");

        foreach (Transform child in _skillSlotContainer) Destroy(child.gameObject);
    }

    private void OnEnable()
    {
        RefreshLocalPlayer();

        _combat.CurrentTurnChanged += OnCurrentTurnChanged;
        _combat.CurrentActorCanMoveChanged += OnActionFlagsChanged;
        _combat.CurrentActorCanActChanged += OnActionFlagsChanged;
        _unit.OwnUnitSkillsChanged += OnOwnUnitSkillsChanged;
        _targeting.TargetingCancelled += OnTargetingCancelled;
        _targeting.TargetConfirmed += OnTargetingConfirmed;
        _endTurnButton?.onClick.AddListener(OnEndTurnClicked);

        if (_combat.CurrentActor != default)
            OnCurrentTurnChanged(_combat.CurrentActor);
    }

    private void OnDisable()
    {
        _combat.CurrentTurnChanged -= OnCurrentTurnChanged;
        _combat.CurrentActorCanMoveChanged -= OnActionFlagsChanged;
        _combat.CurrentActorCanActChanged -= OnActionFlagsChanged;
        _unit.OwnUnitSkillsChanged -= OnOwnUnitSkillsChanged;
        _targeting.TargetingCancelled -= OnTargetingCancelled;
        _targeting.TargetConfirmed -= OnTargetingConfirmed;
        _endTurnButton?.onClick.RemoveListener(OnEndTurnClicked);
        ClearSlots();
        _currentActor = default;
        _localActorId = default;
        _activeSkillId = null;
    }

    private void RefreshLocalPlayer()
    {
        _localPlayer = _network.Runner != null ? _network.Runner.LocalPlayer : default;
        _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"RefreshLocalPlayer → Runner={(_network.Runner != null ? "ready" : "null")} localPlayer={_localPlayer}");
    }

    private void OnCurrentTurnChanged(NetworkId actorId)
    {
        try
        {
            if (_targeting.IsTargeting)
            {
                _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"OnCurrentTurnChanged — cancelling lingering targeting (activeSkill={_activeSkillId})");
                _activeSkillId = null;
                _targeting.Cancel();
            }

            RefreshLocalPlayer();
            _currentActor = actorId;
            bool isLocal = IsLocalPlayerActor(actorId);
            if (isLocal) _localActorId = actorId;
            _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"OnCurrentTurnChanged actorId={actorId} localPlayer={_localPlayer} isLocal={isLocal} localActorId={_localActorId}");
            RenderSkills(_localActorId);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnActionFlagsChanged(bool _)
    {
        try
        {
            if (_targeting.IsTargeting) return;
            RefreshSlotInteractability();
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnOwnUnitSkillsChanged(NetworkId actorId, IReadOnlyList<SkillSlot> skills)
    {
        try
        {
            _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"OwnUnitSkillsChanged actorId={actorId} localActorId={_localActorId} skillCount={skills?.Count ?? 0}");
            if (actorId != _localActorId) return;
            RenderSkills(_localActorId);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnTargetingCancelled()
    {
        try
        {
            _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"TargetingCancelled activeSkill={_activeSkillId}");
            _activeSkillId = null;
            RefreshSlotInteractability();
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnTargetingConfirmed(HexCoord coord)
    {
        try
        {
            _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"TargetingConfirmed coord={coord} activeSkill={_activeSkillId}");
            _activeSkillId = null;
            RefreshSlotInteractability();
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private bool IsLocalPlayerActor(NetworkId actorId)
    {
        if (actorId == default) return false;
        if (!_unit.TryGetPublic(actorId, out var data)) return false;
        bool isLocal = data.Owner == _localPlayer;
        _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"IsLocalPlayerActor actorId={actorId} owner={data.Owner} localPlayer={_localPlayer} result={isLocal}");
        return isLocal;
    }

    private void RenderSkills(NetworkId actorId)
    {
        ClearSlots();

        if (actorId == default)
        {
            _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), "RenderSkills skipped — actorId is default");
            return;
        }
        if (!_unit.TryGetPublic(actorId, out var unitData))
        {
            _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"RenderSkills skipped — TryGetPublic failed for {actorId}");
            return;
        }

        if (_actorNameText != null)
        {
            string displayName = actorId.ToString();
            if (!string.IsNullOrEmpty(unitData.BaseCardId) && _cardLoading.TryGetCardData(unitData.BaseCardId, out var cardData))
                displayName = cardData.name;
            _actorNameText.text = displayName;
        }

        if (!_unit.TryGetOwnSkills(actorId, out var skills) || skills == null)
        {
            _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"RenderSkills — TryGetOwnSkills null/false for {actorId} (no private data — unit not ours or not yet received)");
            return;
        }

        _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"RenderSkills actorId={actorId} skillCount={skills.Count} isLocal={IsLocalPlayerActor(actorId)}");

        for (int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (string.IsNullOrEmpty(skill.SkillId)) continue;

            var slot = Instantiate(_skillSlotPrefab, _skillSlotContainer);
            slot.gameObject.SetActive(true);

            if (slot.NameText != null)
                slot.NameText.text = ResolveSkillName(skill.SkillId);

            ApplySlotVisualState(slot, skill, actorId);

            if (slot.Button != null)
            {
                slot.Button.onClick.RemoveAllListeners();
                string skillId = skill.SkillId;
                slot.Button.onClick.AddListener(() => OnSkillClicked(skillId));
                slot.Button.interactable = IsSkillReady(skill.SkillId, skill) && _currentActor == _localActorId;
            }

            _spawnedSlots.Add(slot);
        }
    }

    private string ResolveSkillName(string skillId)
    {
        if (skillId == "move") return "Move";
        if (skillId == "n_atk") return "Attack";
        if (_cardLoading.TryGetSkillData(skillId, out var skillData))
            return skillData.name;
        return skillId;
    }

    private void ApplySlotVisualState(SkillSlotUI slot, SkillSlot skill, NetworkId actorId)
    {
        bool isDisabled = skill.IsOneTimeDisabled;
        bool onCooldown = skill.CurrentCooldown > 0;

        if (slot.CooldownText != null)
        {
            if (isDisabled)
                slot.CooldownText.text = "Used";
            else if (onCooldown)
                slot.CooldownText.text = $"CD: {skill.CurrentCooldown}";
            else if (skill.SkillId == "move" && !_combat.CurrentActorCanMove)
                slot.CooldownText.text = "Done";
            else if (skill.SkillId == "n_atk" && !_combat.CurrentActorCanAct)
                slot.CooldownText.text = "Done";
            else
                slot.CooldownText.text = "";
        }

        if (slot.Background != null)
        {
            if (isDisabled) slot.Background.color = _disabledColor;
            else if (onCooldown) slot.Background.color = _cooldownColor;
            else slot.Background.color = _readyColor;
        }
    }

    private void RefreshSlotInteractability()
    {
        if (!_unit.TryGetOwnSkills(_localActorId, out var skills) || skills == null) return;

        bool isLocal = IsLocalPlayerActor(_currentActor);
        bool targeting = _targeting.IsTargeting;

        for (int i = 0; i < _spawnedSlots.Count && i < skills.Count; i++)
        {
            var slot = _spawnedSlots[i];
            var skill = skills[i];
            if (slot == null) continue;

            ApplySlotVisualState(slot, skill, _currentActor);

            if (slot.Button != null)
            {
                bool isActiveSkill = targeting && skill.SkillId == _activeSkillId;
                // Keep the active skill button clickable so the player can reclick to cancel targeting.
                slot.Button.interactable = (!targeting || isActiveSkill) && isLocal && IsSkillReady(skill.SkillId, skill);
            }
        }

        if (_endTurnButton != null)
            _endTurnButton.interactable = !targeting && isLocal;
    }

    private void OnSkillClicked(string skillId)
    {
        if (_currentActor == default) return;

        _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"OnSkillClicked skillId={skillId} isTargeting={_targeting.IsTargeting} activeSkill={_activeSkillId}");

        if (_targeting.IsTargeting)
        {
            if (skillId == _activeSkillId)
            {
                _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"Reclick on active skill '{skillId}' — cancelling targeting");
                _activeSkillId = null;
                _targeting.Cancel();
            }
            return;
        }

        if (skillId == "move")
        {
            _activeSkillId = skillId;
            BeginMoveTargeting();
            return;
        }
        if (skillId == "n_atk")
        {
            _activeSkillId = skillId;
            BeginNormalAttackTargeting();
            return;
        }

        _cardLoading.TryGetSkillData(skillId, out var skillData);

        int range = 1;
        TargetMask mask = TargetMask.Enemy;
        string displayPattern = null;

        if (skillData != null)
        {
            range = HexPatternResolver.GetRange(skillData.target_pattern);
            mask = ResolveTargetMask(skillData.target_condition);
            displayPattern = skillData.display_pattern != null && skillData.display_pattern.Count > 0 ? skillId : null;
        }

        var request = new TargetingRequest
        {
            Mask = mask,
            Range = range,
            DisplayPattern = displayPattern,
            TargetPatternSkillId = skillId,
            Caster = _currentActor,
            IgnorePathfinding = skillData?.ignore_pathfinding ?? false,
        };

        _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"BeginTargeting skillId={skillId} mask={mask} range={range} caster={_currentActor}");

        _activeSkillId = skillId;
        RefreshSlotInteractability();
        _targeting.BeginTargeting(request, target => OnTargetConfirmed(skillId, target));
        RefreshSlotInteractability();
    }

    private void BeginMoveTargeting()
    {
        if (!_combat.CurrentActorCanMove)
        {
            _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), "BeginMoveTargeting — CurrentActorCanMove is false, aborting");
            _activeSkillId = null;
            return;
        }

        int range = 2;
        if (_unit.TryGetPublic(_currentActor, out var data) && data.MoveRange > 0)
            range = data.MoveRange;

        var request = new TargetingRequest
        {
            Mask = TargetMask.EmptyTile,
            Range = range,
            DisplayPattern = null,
            Caster = _currentActor,
            IgnorePathfinding = false
        };

        _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"BeginMoveTargeting range={range} caster={_currentActor}");
        _targeting.BeginTargeting(request, target => OnTargetConfirmed("move", target));
        RefreshSlotInteractability();
    }

    private void BeginNormalAttackTargeting()
    {
        if (!_combat.CurrentActorCanAct)
        {
            _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), "BeginNormalAttackTargeting — CurrentActorCanAct is false, aborting");
            _activeSkillId = null;
            return;
        }

        string baseCardId = null;
        if (_unit.TryGetPublic(_currentActor, out var data))
            baseCardId = data.BaseCardId;

        var request = new TargetingRequest
        {
            Mask = TargetMask.Enemy,
            Range = 1,
            TargetPatternCardId = baseCardId,
            DisplayPattern = null,
            Caster = _currentActor,
            IgnorePathfinding = false
        };

        _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"BeginNormalAttackTargeting cardId={baseCardId} caster={_currentActor}");
        _targeting.BeginTargeting(request, target => OnTargetConfirmed("n_atk", target));
        RefreshSlotInteractability();
    }

    private void OnTargetConfirmed(string skillId, HexCoord target)
    {
        _debugLogger.Log("LOG_SKILL_PANEL", nameof(SkillPanel), $"OnTargetConfirmed skillId={skillId} target={target}");
        if (skillId == "move")
            _combat.RequestMove(_currentActor, target);
        else if (skillId == "n_atk")
            _combat.RequestNormalAttack(_currentActor, target);
        else
            _combat.RequestSkill(_currentActor, skillId, target);
    }

    private void OnEndTurnClicked()
    {
        if (!IsLocalPlayerActor(_currentActor)) return;
        _combat.EndTurn();
    }

    private bool IsSkillReady(string skillId, SkillSlot skill)
    {
        if (_currentActor == default) return false;

        if (skillId == "move") return _combat.CurrentActorCanMove;
        if (skillId == "n_atk") return _combat.CurrentActorCanAct;

        return !skill.IsOneTimeDisabled && skill.CurrentCooldown <= 0 && _combat.CurrentActorCanAct;
    }

    private static TargetMask ResolveTargetMask(int targetCondition)
    {
        if (targetCondition == 0) return TargetMask.Self;

        TargetMask mask = TargetMask.None;
        if ((targetCondition & 1) != 0) mask |= TargetMask.Enemy;
        if ((targetCondition & 2) != 0) mask |= TargetMask.Ally;
        if ((targetCondition & 4) != 0) mask |= TargetMask.EmptyTile;
        return mask;
    }

    private void ClearSlots()
    {
        foreach (var slot in _spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);
        _spawnedSlots.Clear();
    }
}
