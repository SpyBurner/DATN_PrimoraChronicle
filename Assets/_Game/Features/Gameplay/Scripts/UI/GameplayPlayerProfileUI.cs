using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class GameplayPlayerProfileUI : MonoBehaviour
{
    [Inject] private readonly IPlayerRosterSubsystem _roster;
    [Inject] private readonly IGameplayDeckChooseSubsystem _deckChoose;

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

        if (_readyToggle != null)
        {
            _readyToggle.interactable = false;
            _readyToggle.isOn = false;
        }
        Refresh();
    }

    private void OnEnable()
    {
        _roster.HPChanged += OnHPChanged;
        _roster.NameChanged += OnNameChanged;
        _deckChoose.IsReadyChanged += OnIsReadyChanged;
    }

    private void OnDisable()
    {
        _roster.HPChanged -= OnHPChanged;
        _roster.NameChanged -= OnNameChanged;
        _deckChoose.IsReadyChanged -= OnIsReadyChanged;
    }

    private void OnHPChanged(PlayerRef p, int hp)
    {
        if (!_bound || p != _playerRef) return;
        try { if (_hpText != null) _hpText.text = hp.ToString(); }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnNameChanged(PlayerRef p, string name)
    {
        if (!_bound || p != _playerRef || string.IsNullOrEmpty(name)) return;
        try { if (_nameText != null) _nameText.text = name; }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnIsReadyChanged(bool isReady)
    {
        if (!_bound || !_isLocal) return;
        try { if (_readyToggle != null) _readyToggle.isOn = isReady; }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void Refresh()
    {
        if (_hpText != null)
            _hpText.text = _roster.GetHP(_playerRef).ToString();
        RefreshName();
    }

    private void RefreshName()
    {
        if (_nameText == null || !_bound) return;
        var name = _roster.GetName(_playerRef);
        if (!string.IsNullOrEmpty(name))
            _nameText.text = name;
    }
}
