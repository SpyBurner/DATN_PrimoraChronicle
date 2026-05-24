using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class FusionPanel : MonoBehaviour
{
    [Inject] private readonly IFusionSubsystem _fusion;
    [Inject] private readonly IGameStateSubsystem _gameState;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoading;

    [Header("Unit Slot (Base)")]
    [SerializeField] private Transform _unitSlot;
    [SerializeField] private TMP_Text _unitNameText;
    [SerializeField] private TMP_Text _unitStatsText;
    [SerializeField] private Button _clearBaseButton;

    [Header("Innate Slots (always visible)")]
    // [SerializeField] private GameObject _normalAttackSlot;
    // [SerializeField] private GameObject _movementSlot;

    [Header("Fuse Slots")]
    [SerializeField] private Transform _fuseSlotContainer;
    [SerializeField] private GameObject _fuseSlotPrefab;

    private FuseSlotUI[] _fuseSlots;

    [Header("Timer")]
    [SerializeField] private TMP_Text _timerText;

    [Header("Confirm")]
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TMP_Text _confirmText;

    [Header("Hand Source")]
    [SerializeField] private HandPanel _handPanel;

    private bool _confirmed;

    private void Awake()
    {
        if (_unitSlot == null) throw new System.Exception("[FusionPanel._unitSlot] Not assigned in Inspector");
        // if (_normalAttackSlot == null) throw new System.Exception("[FusionPanel._normalAttackSlot] Not assigned in Inspector");
        // if (_movementSlot == null) throw new System.Exception("[FusionPanel._movementSlot] Not assigned in Inspector");
        if (_timerText == null) throw new System.Exception("[FusionPanel._timerText] Not assigned in Inspector");
        if (_confirmButton == null) throw new System.Exception("[FusionPanel._confirmButton] Not assigned in Inspector");
        if (_handPanel == null) throw new System.Exception("[FusionPanel._handPanel] Not assigned in Inspector");
        if (_fuseSlotContainer == null) throw new System.Exception("[FusionPanel._fuseSlotContainer] Not assigned in Inspector");
        if (_fuseSlotPrefab == null) throw new System.Exception("[FusionPanel._fuseSlotPrefab] Not assigned in Inspector");

        BuildFuseSlots();
    }

    private void BuildFuseSlots()
    {
        foreach (Transform child in _fuseSlotContainer)
            Destroy(child.gameObject);

        _fuseSlots = new FuseSlotUI[4];
        for (int i = 0; i < 4; i++)
        {
            var go = Instantiate(_fuseSlotPrefab, _fuseSlotContainer);
            _fuseSlots[i] = go.GetComponent<FuseSlotUI>();
            go.GetComponent<FuseSlotDropTarget>()?.Initialize(i, this);
        }
    }

    private void OnEnable()
    {
        _confirmed = false;
        _fusion.ClearStaging();

        _fusion.StagingChanged += OnStagingChanged;
        _fusion.FusionConfirmed += OnFusionConfirmed;
        _gameState.PhaseChanged += OnPhaseChanged;
        _gameState.PhaseTimeRemainingChanged += OnPhaseTimeRemainingChanged;
        _confirmButton?.onClick.AddListener(OnConfirmClicked);
        _clearBaseButton?.onClick.AddListener(OnClearBaseClicked);

        for (int i = 0; i < _fuseSlots.Length; i++)
        {
            int slotIndex = i;
            _fuseSlots[i]?.ClearButton?.onClick.AddListener(() => OnClearSlotClicked(slotIndex));
        }

        RefreshUI(_fusion.CurrentStaging);
    }

    private void OnDisable()
    {
        _fusion.StagingChanged -= OnStagingChanged;
        _fusion.FusionConfirmed -= OnFusionConfirmed;
        _gameState.PhaseChanged -= OnPhaseChanged;
        _gameState.PhaseTimeRemainingChanged -= OnPhaseTimeRemainingChanged;
        _confirmButton?.onClick.RemoveListener(OnConfirmClicked);
        _clearBaseButton?.onClick.RemoveListener(OnClearBaseClicked);

        for (int i = 0; i < _fuseSlots.Length; i++)
            _fuseSlots[i]?.ClearButton?.onClick.RemoveAllListeners();
    }

    private void OnPhaseChanged(GameplayPhase phase)
    {
        try
        {
            gameObject.SetActive(phase == GameplayPhase.MainPhase);
            if (phase == GameplayPhase.MainPhase)
            {
                _confirmed = false;
                _fusion.ClearStaging();
                SetConfirmInteractable(true);
            }
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnPhaseTimeRemainingChanged(float seconds)
    {
        try
        {
            int s = Mathf.CeilToInt(seconds);
            _timerText.text = s.ToString();
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnStagingChanged(FusionStagingData staging)
    {
        try { RefreshUI(staging); }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnFusionConfirmed()
    {
        try
        {
            _confirmed = true;
            SetConfirmInteractable(false);
            if (_confirmText != null) _confirmText.text = "Confirmed";
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void RefreshUI(FusionStagingData staging)
    {
        RefreshBaseSlot(staging.BaseCardId);
        RefreshFuseSlots(staging);
        UpdateConfirmState(staging);
    }

    private void RefreshBaseSlot(string baseCardId)
    {
        bool hasBase = !string.IsNullOrEmpty(baseCardId);
        // if (_normalAttackSlot != null) _normalAttackSlot.SetActive(hasBase);
        // if (_movementSlot != null) _movementSlot.SetActive(hasBase);
        if (_clearBaseButton != null) _clearBaseButton.gameObject.SetActive(hasBase);

        if (hasBase && _cardLoading.TryGetCardData(baseCardId, out var cardData))
        {
            if (_unitNameText != null) _unitNameText.text = cardData.name;
            if (_unitStatsText != null)
                _unitStatsText.text = $"HP:{cardData.hp} SPD:{cardData.speed:F1} ATK:{cardData.n_atk_dmg}";
        }
        else
        {
            if (_unitNameText != null) _unitNameText.text = "";
            if (_unitStatsText != null) _unitStatsText.text = "";
        }
    }

    private void RefreshFuseSlots(FusionStagingData staging)
    {
        if (_fuseSlots == null) return;
        int maxSlots = staging.HasInnateSkill ? 3 : 4;
        var equips = staging.EquipSpellIds;

        for (int i = 0; i < _fuseSlots.Length; i++)
        {
            var slot = _fuseSlots[i];
            if (slot == null) continue;

            bool isAvailable = i < maxSlots;
            slot.gameObject.SetActive(isAvailable);

            if (!isAvailable) continue;

            bool hasEquip = equips != null && i < equips.Length && !string.IsNullOrEmpty(equips[i]);
            if (hasEquip && _cardLoading.TryGetCardData(equips[i], out var spellData))
            {
                if (slot.NameText != null) slot.NameText.text = spellData.name;
                if (slot.ClearButton != null) slot.ClearButton.gameObject.SetActive(true);
            }
            else
            {
                if (slot.NameText != null) slot.NameText.text = "Empty";
                if (slot.ClearButton != null) slot.ClearButton.gameObject.SetActive(false);
            }
        }

        if (staging.HasInnateSkill && _fuseSlots.Length > 3 && _fuseSlots[3] != null)
        {
            var innateSlot = _fuseSlots[3];
            innateSlot.gameObject.SetActive(true);
            if (innateSlot.NameText != null) innateSlot.NameText.text = "Innate Skill";
            if (innateSlot.ClearButton != null) innateSlot.ClearButton.gameObject.SetActive(false);
        }
    }

    private void UpdateConfirmState(FusionStagingData staging)
    {
        bool canConfirm = !_confirmed && !string.IsNullOrEmpty(staging.BaseCardId);
        SetConfirmInteractable(canConfirm);
        if (_confirmText != null && !_confirmed) _confirmText.text = "Confirm Fusion";
    }

    private void SetConfirmInteractable(bool value)
    {
        if (_confirmButton != null) _confirmButton.interactable = value;
    }

    private void OnClearSlotClicked(int slotIndex)
    {
        if (_confirmed) return;
        _fusion.ClearSlot(slotIndex);
    }

    private async void OnConfirmClicked()
    {
        if (_confirmed) return;
        SetConfirmInteractable(false);
        await _fusion.ConfirmFusion();
    }

    private void OnClearBaseClicked()
    {
        if (_confirmed) return;
        _fusion.StageBase(null);
    }

    public void StageBase(string cardId)
    {
        if (_confirmed) return;
        _fusion.StageBase(cardId);
    }

    public void StageEquipSpell(int slotIndex, string cardId, int handIndex)
    {
        if (_confirmed) return;
        _fusion.StageEquipSpell(slotIndex, cardId, handIndex);
    }

    public int GetFirstEmptyFuseSlotIndex()
    {
        var staging = _fusion.CurrentStaging;
        int maxSlots = staging.HasInnateSkill ? 3 : 4;
        var equips = staging.EquipSpellIds;

        for (int i = 0; i < maxSlots; i++)
        {
            if (equips == null || i >= equips.Length || string.IsNullOrEmpty(equips[i]))
                return i;
        }
        return -1;
    }
}
