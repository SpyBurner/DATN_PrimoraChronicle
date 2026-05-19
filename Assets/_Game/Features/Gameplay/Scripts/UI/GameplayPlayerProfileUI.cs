using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class GameplayPlayerProfileUI : MonoBehaviour
{
    [Inject] private readonly IProfileSubsystem _profile;
    [Inject] private readonly IPlayerCardZoneSubsystem _cardZone;

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private Toggle _readyToggle;

    private PlayerRef _playerRef;
    private bool _isLocal;
    private bool _bound;

    public void Bind(PlayerRef playerRef, bool isLocal)
    {
        _playerRef = playerRef;
        _isLocal = isLocal;
        _bound = true;

        if (_readyToggle != null) _readyToggle.interactable = false;
        Refresh();
    }

    private void OnEnable()
    {
        _cardZone.HPChanged += OnHPChanged;
        _profile.UsernameChanged += OnUsernameChanged;
    }

    private void OnDisable()
    {
        _cardZone.HPChanged -= OnHPChanged;
        _profile.UsernameChanged -= OnUsernameChanged;
    }

    private void OnHPChanged(PlayerRef p, int hp)
    {
        if (!_bound || p != _playerRef) return;
        try { if (_hpText != null) _hpText.text = hp.ToString(); }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnUsernameChanged(string name)
    {
        if (!_bound || !_isLocal) return;
        try { if (_nameText != null) _nameText.text = name; }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void Refresh()
    {
        if (_nameText != null)
            _nameText.text = _isLocal ? _profile.Username : "Opponent";

        if (_hpText != null)
            _hpText.text = _cardZone.GetHP(_playerRef).ToString();
    }
}
