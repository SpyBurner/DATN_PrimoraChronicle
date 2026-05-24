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

    [Header("References")]
    [SerializeField] private Transform _skillSlotContainer;
    [SerializeField] private GameObject _skillSlotPrefab;
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private TMP_Text _actorNameText;

    [Header("Slot Colors")]
    [SerializeField] private Color _readyColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color _cooldownColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private Color _disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private readonly List<SkillSlotUI> _spawnedSlots = new();
    private PlayerRef _localPlayer;
    private NetworkId _currentActor;

    private void Awake()
    {
        if (_skillSlotContainer == null) throw new System.Exception("[SkillPanel._skillSlotContainer] Not assigned in Inspector — see wiring-F4.md F4.5");
        if (_skillSlotPrefab == null) throw new System.Exception("[SkillPanel._skillSlotPrefab] Not assigned in Inspector — see wiring-F4.md F4.5");
        if (_endTurnButton == null) throw new System.Exception("[SkillPanel._endTurnButton] Not assigned in Inspector — see wiring-F4.md F4.5");
        if (_actorNameText == null) throw new System.Exception("[SkillPanel._actorNameText] Not assigned in Inspector — see wiring-F4.md F4.5");

        foreach (Transform child in _skillSlotContainer) Destroy(child.gameObject);
    }

    private struct SkillSlotUI
    {
        public GameObject Root;
        public Button Button;
        public TMP_Text NameText;
        public TMP_Text CooldownText;
        public Image Background;
        public string SkillId;
    }

    private void OnEnable()
    {
        _localPlayer = _network.Runner != null ? _network.Runner.LocalPlayer : default;

        _combat.CurrentTurnChanged += OnCurrentTurnChanged;
        _targeting.TargetingCancelled += OnTargetingCancelled;
        _endTurnButton?.onClick.AddListener(OnEndTurnClicked);

        if (_combat.CurrentActor != default)
            OnCurrentTurnChanged(_combat.CurrentActor);
    }

    private void OnDisable()
    {
        _combat.CurrentTurnChanged -= OnCurrentTurnChanged;
        _targeting.TargetingCancelled -= OnTargetingCancelled;
        _endTurnButton?.onClick.RemoveListener(OnEndTurnClicked);
        ClearSlots();
        _currentActor = default;
    }



    private void OnCurrentTurnChanged(NetworkId actorId)
    {
        try
        {
            _currentActor = actorId;
            bool isLocalTurn = IsLocalPlayerActor(actorId);
            SetInteractable(isLocalTurn);
            RenderSkills(actorId);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnTargetingCancelled()
    {
        try
        {
            SetInteractable(IsLocalPlayerActor(_currentActor));
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private bool IsLocalPlayerActor(NetworkId actorId)
    {
        if (actorId == default) return false;
        if (!_unit.TryGetPublic(actorId, out var data)) return false;
        return data.Owner == _localPlayer;
    }

    private void RenderSkills(NetworkId actorId)
    {
        ClearSlots();

        if (actorId == default) return;
        if (!_unit.TryGetPublic(actorId, out var unitData)) return;

        if (_actorNameText != null)
        {
            string displayName = actorId.ToString();
            if (!string.IsNullOrEmpty(unitData.UnitId.ToString()) && _cardLoading.TryGetCardData(actorId.ToString(), out var cardData))
                displayName = cardData.name;
            _actorNameText.text = displayName;
        }

        if (!_unit.TryGetOwnSkills(actorId, out var skills) || skills == null) return;

        for (int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (string.IsNullOrEmpty(skill.SkillId)) continue;

            var slot = Instantiate(_skillSlotPrefab, _skillSlotContainer);
            slot.SetActive(true);

            var slotUI = new SkillSlotUI
            {
                Root = slot,
                Button = slot.GetComponentInChildren<Button>(),
                NameText = slot.transform.Find("NameText")?.GetComponent<TMP_Text>(),
                CooldownText = slot.transform.Find("CooldownText")?.GetComponent<TMP_Text>(),
                Background = slot.GetComponent<Image>(),
                SkillId = skill.SkillId,
            };

            if (slotUI.NameText != null)
            {
                if (skill.SkillId == "move")
                    slotUI.NameText.text = "Move";
                else if (skill.SkillId == "n_atk")
                    slotUI.NameText.text = "Attack";
                else if (_cardLoading.TryGetSkillData(skill.SkillId, out var skillData))
                    slotUI.NameText.text = skillData.name;
                else
                    slotUI.NameText.text = skill.SkillId;
            }

            bool isDisabled = skill.IsOneTimeDisabled;
            bool onCooldown = skill.CurrentCooldown > 0;
            
            // Move doesn't consume cooldown, it consumes HasMoved
            // N_Atk doesn't consume cooldown, it consumes HasActed
            bool isReady = !isDisabled && !onCooldown;
            if (skill.SkillId == "move")
                isReady = _combat.CurrentActorCanMove;
            else if (skill.SkillId == "n_atk")
                isReady = _combat.CurrentActorCanAct;
            else
                isReady = isReady && _combat.CurrentActorCanAct;

            if (slotUI.CooldownText != null)
            {
                if (isDisabled)
                    slotUI.CooldownText.text = "Used";
                else if (onCooldown)
                    slotUI.CooldownText.text = $"CD: {skill.CurrentCooldown}";
                else if (skill.SkillId == "move" && !_combat.CurrentActorCanMove)
                    slotUI.CooldownText.text = "Done";
                else if (skill.SkillId == "n_atk" && !_combat.CurrentActorCanAct)
                    slotUI.CooldownText.text = "Done";
                else
                    slotUI.CooldownText.text = "";
            }

            if (slotUI.Background != null)
            {
                if (isDisabled) slotUI.Background.color = _disabledColor;
                else if (onCooldown) slotUI.Background.color = _cooldownColor;
                else slotUI.Background.color = _readyColor;
            }

            if (slotUI.Button != null)
            {
                slotUI.Button.interactable = isReady && IsLocalPlayerActor(actorId);
                string skillId = skill.SkillId;
                slotUI.Button.onClick.AddListener(() => OnSkillClicked(skillId));
            }

            _spawnedSlots.Add(slotUI);
        }
    }

    private void OnSkillClicked(string skillId)
    {
        if (_currentActor == default) return;
        if (_targeting.IsTargeting) return;

        if (skillId == "move")
        {
            OnMoveClicked();
            return;
        }
        else if (skillId == "n_atk")
        {
            OnNormalAttackClicked();
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
            Caster = _currentActor,
            IgnorePathfinding = skillData?.ignore_pathfinding ?? false,
        };

        SetInteractable(false);
        _targeting.BeginTargeting(request, target => OnTargetConfirmed(skillId, target));
    }

    private void OnTargetConfirmed(string skillId, HexCoord target)
    {
        if (skillId == "move")
            _combat.RequestMove(_currentActor, target);
        else if (skillId == "n_atk")
            _combat.RequestNormalAttack(_currentActor, target);
        else
            _combat.RequestSkill(_currentActor, skillId, target);
    }

    private void OnMoveClicked()
    {
        if (_currentActor == default) return;
        if (_targeting.IsTargeting) return;
        if (!_combat.CurrentActorCanMove) return;

        var request = new TargetingRequest
        {
            Mask = TargetMask.EmptyTile,
            Range = 2, // Hardcoded MoveRange for now based on UnitNetworkView
            DisplayPattern = null,
            Caster = _currentActor,
            IgnorePathfinding = false
        };

        SetInteractable(false);
        _targeting.BeginTargeting(request, target => OnTargetConfirmed("move", target));
    }

    private void OnNormalAttackClicked()
    {
        if (_currentActor == default) return;
        if (_targeting.IsTargeting) return;
        if (!_combat.CurrentActorCanAct) return;

        int range = 1; // Default normal attack range
        TargetMask mask = TargetMask.Enemy;

        if (_unit.TryGetPublic(_currentActor, out var data))
        {
            // UnitNetworkView BaseCardId isn't exposed directly here but we can assume normal attack is standard.
            // Ideally we get n_atk_pattern from CardData if we had the BaseCardId, but range=1 is default.
        }

        var request = new TargetingRequest
        {
            Mask = mask,
            Range = range,
            DisplayPattern = null,
            Caster = _currentActor,
            IgnorePathfinding = false
        };

        SetInteractable(false);
        _targeting.BeginTargeting(request, target => OnTargetConfirmed("n_atk", target));
    }

    private void OnEndTurnClicked()
    {
        if (!IsLocalPlayerActor(_currentActor)) return;
        _combat.EndTurn();
    }

    private void SetInteractable(bool interactable)
    {
        foreach (var slot in _spawnedSlots)
        {
            if (slot.Button != null)
            {
                bool ready = IsSkillReady(slot.SkillId);
                slot.Button.interactable = interactable && ready;
            }
        }

        if (_endTurnButton != null)
            _endTurnButton.interactable = interactable;
    }

    private bool IsSkillReady(string skillId)
    {
        if (_currentActor == default) return false;
        
        if (skillId == "move") return _combat.CurrentActorCanMove;
        if (skillId == "n_atk") return _combat.CurrentActorCanAct;

        if (!_unit.TryGetOwnSkills(_currentActor, out var skills) || skills == null) return false;

        foreach (var skill in skills)
        {
            if (skill.SkillId == skillId)
                return !skill.IsOneTimeDisabled && skill.CurrentCooldown <= 0;
        }
        return false;
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
            if (slot.Root != null) Destroy(slot.Root);
        _spawnedSlots.Clear();
    }
}
