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
        _gameState.PhaseChanged += OnPhaseChanged;
        _targeting.TargetingCancelled += OnTargetingCancelled;
        _endTurnButton?.onClick.AddListener(OnEndTurnClicked);

        if (_combat.CurrentActor != default)
            OnCurrentTurnChanged(_combat.CurrentActor);
    }

    private void OnDisable()
    {
        _combat.CurrentTurnChanged -= OnCurrentTurnChanged;
        _gameState.PhaseChanged -= OnPhaseChanged;
        _targeting.TargetingCancelled -= OnTargetingCancelled;
        _endTurnButton?.onClick.RemoveListener(OnEndTurnClicked);
        ClearSlots();
        _currentActor = default;
    }

    private void OnPhaseChanged(GameplayPhase phase)
    {
        try
        {
            gameObject.SetActive(phase == GameplayPhase.CombatPhase);
        }
        catch (Exception ex) { Debug.LogException(ex); }
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
                if (_cardLoading.TryGetSkillData(skill.SkillId, out var skillData))
                    slotUI.NameText.text = skillData.name;
                else
                    slotUI.NameText.text = skill.SkillId;
            }

            bool isDisabled = skill.IsOneTimeDisabled;
            bool onCooldown = skill.CurrentCooldown > 0;
            bool isReady = !isDisabled && !onCooldown;

            if (slotUI.CooldownText != null)
            {
                if (isDisabled)
                    slotUI.CooldownText.text = "Used";
                else if (onCooldown)
                    slotUI.CooldownText.text = $"CD: {skill.CurrentCooldown}";
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
        _combat.RequestSkill(_currentActor, skillId, target);
    }

    private void OnEndTurnClicked()
    {
        if (!IsLocalPlayerActor(_currentActor)) return;
        _combat.EndTurn();
    }

    private void SetInteractable(bool interactable)
    {
        foreach (var slot in _spawnedSlots)
            if (slot.Button != null) slot.Button.interactable = interactable && IsSkillReady(slot.SkillId);

        if (_endTurnButton != null)
            _endTurnButton.interactable = interactable;
    }

    private bool IsSkillReady(string skillId)
    {
        if (_currentActor == default) return false;
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
