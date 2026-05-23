using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class FusionPanel : MonoBehaviour
{
    [Inject] private readonly IFusionSubsystem _fusion;
    [Inject] private readonly IPlayerCardZoneSubsystem _cardZone;
    [Inject] private readonly IGameStateSubsystem _gameState;
    [Inject] private readonly INetworkManagerSubsystem _network;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoading;

    [Header("Unit Slot (Base)")]
    [SerializeField] private Transform _unitSlot;
    [SerializeField] private TMP_Text _unitNameText;
    [SerializeField] private TMP_Text _unitStatsText;

    [Header("Innate Slots (always visible)")]
    [SerializeField] private GameObject _normalAttackSlot;
    [SerializeField] private GameObject _movementSlot;

    [Header("Fuse Slots (drop targets)")]
    [SerializeField] private FuseSlot[] _fuseSlots = new FuseSlot[4];

    [Header("Confirm")]
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TMP_Text _confirmText;

    [Header("Hand Source")]
    [SerializeField] private HandPanel _handPanel;

    private PlayerRef _localPlayer;
    private bool _confirmed;

    private void Awake()
    {
        if (_unitSlot == null) throw new System.Exception("[FusionPanel._unitSlot] Not assigned in Inspector — see wiring-F3.md F3.2");
        // if (_unitNameText == null) throw new System.Exception("[FusionPanel._unitNameText] Not assigned in Inspector — see wiring-F3.md F3.2");
        // if (_unitStatsText == null) throw new System.Exception("[FusionPanel._unitStatsText] Not assigned in Inspector — see wiring-F3.md F3.2");
        if (_normalAttackSlot == null) throw new System.Exception("[FusionPanel._normalAttackSlot] Not assigned in Inspector — see wiring-F3.md F3.2");
        if (_movementSlot == null) throw new System.Exception("[FusionPanel._movementSlot] Not assigned in Inspector — see wiring-F3.md F3.2");
        if (_confirmButton == null) throw new System.Exception("[FusionPanel._confirmButton] Not assigned in Inspector — see wiring-F3.md F3.2");
        // if (_confirmText == null) throw new System.Exception("[FusionPanel._confirmText] Not assigned in Inspector — see wiring-F3.md F3.2");
        if (_handPanel == null) throw new System.Exception("[FusionPanel._handPanel] Not assigned in Inspector — see wiring-F3.md F3.2");
        for (int i = 0; i < _fuseSlots.Length; i++)
            if (_fuseSlots[i].Root == null) throw new System.Exception($"[FusionPanel._fuseSlots[{i}].Root] Not assigned in Inspector — see wiring-F3.md F3.2");
    }

    [Serializable]
    public struct FuseSlot
    {
        public GameObject Root;
        public TMP_Text NameText;
        public Image Icon;
        public Button ClearButton;
    }

    private void OnEnable()
    {
        _localPlayer = _network.Runner != null ? _network.Runner.LocalPlayer : default;
        _confirmed = false;

        _fusion.StagingChanged += OnStagingChanged;
        _fusion.FusionConfirmed += OnFusionConfirmed;
        _gameState.PhaseChanged += OnPhaseChanged;
        _confirmButton?.onClick.AddListener(OnConfirmClicked);

        for (int i = 0; i < _fuseSlots.Length; i++)
        {
            int slotIndex = i;
            _fuseSlots[i].ClearButton?.onClick.AddListener(() => OnClearSlotClicked(slotIndex));
        }

        RefreshUI(_fusion.CurrentStaging);
    }

    private void OnDisable()
    {
        _fusion.StagingChanged -= OnStagingChanged;
        _fusion.FusionConfirmed -= OnFusionConfirmed;
        _gameState.PhaseChanged -= OnPhaseChanged;
        _confirmButton?.onClick.RemoveListener(OnConfirmClicked);

        for (int i = 0; i < _fuseSlots.Length; i++)
            _fuseSlots[i].ClearButton?.onClick.RemoveAllListeners();
    }

    private void OnPhaseChanged(GameplayPhase phase)
    {
        try
        {
            gameObject.SetActive(phase == GameplayPhase.MainPhase);
            if (phase == GameplayPhase.MainPhase)
            {
                _confirmed = false;
                SetConfirmInteractable(true);
            }
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
        if (_unitSlot != null) _unitSlot.gameObject.SetActive(hasBase);
        if (_normalAttackSlot != null) _normalAttackSlot.SetActive(hasBase);
        if (_movementSlot != null) _movementSlot.SetActive(hasBase);

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
        int maxSlots = staging.HasInnateSkill ? 3 : 4;
        var equips = staging.EquipSpellIds;

        for (int i = 0; i < _fuseSlots.Length; i++)
        {
            var slot = _fuseSlots[i];
            if (slot.Root == null) continue;

            bool isAvailable = i < maxSlots;
            slot.Root.SetActive(isAvailable);

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

        if (staging.HasInnateSkill && _fuseSlots.Length > 3)
        {
            var innateSlot = _fuseSlots[3];
            if (innateSlot.Root != null)
            {
                innateSlot.Root.SetActive(true);
                if (innateSlot.NameText != null) innateSlot.NameText.text = "Innate Skill";
                if (innateSlot.ClearButton != null) innateSlot.ClearButton.gameObject.SetActive(false);
            }
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

    public void StageBase(string cardId)
    {
        if (_confirmed) return;
        _fusion.StageBase(cardId);
    }

    public void StageEquipSpell(int slotIndex, string cardId)
    {
        if (_confirmed) return;
        _fusion.StageEquipSpell(slotIndex, cardId);
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
