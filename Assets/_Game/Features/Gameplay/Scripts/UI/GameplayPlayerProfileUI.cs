using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// Drives one player slot on Profile_Gameplay.prefab.
/// Call Bind(playerRef, isLocal) from GameplayHUDController once the Runner knows both players.
/// - HP + Name + UserId: from IPlayerRosterSubsystem (all players)
/// - PlayerReady visual: from IGameStateSubsystem.PlayerReadyChanged (always non-interactable)
/// - Own profile picture (local slot only): from IProfileSubsystem.AvatarUrlChanged (cached URL)
/// - Opponent PFP: fetched by UserId via IHttpServiceSubsystem when UserIdChanged fires
/// </summary>
public class GameplayPlayerProfileUI : MonoBehaviour
{
    [Inject] private readonly IPlayerRosterSubsystem _roster;
    [Inject] private readonly IGameStateSubsystem _gameState;
    [Inject] private readonly IProfileSubsystem _profile;

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private Toggle _readyToggle;
    [SerializeField] private RawImage _avatarImage;     // optional — may be null if no avatar slot

    private PlayerRef _playerRef;
    private bool _isLocal;
    private bool _bound;

    private void Awake()
    {
        if (_nameText == null) throw new System.Exception("[GameplayPlayerProfileUI._nameText] Not assigned in Inspector — see wiring.md F1.5");
        if (_hpText == null) throw new System.Exception("[GameplayPlayerProfileUI._hpText] Not assigned in Inspector — see wiring.md F1.5");
        if (_readyToggle == null) throw new System.Exception("[GameplayPlayerProfileUI._readyToggle] Not assigned in Inspector — see wiring.md F1.5");
    }

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
        _roster.UserIdChanged += OnUserIdChanged;
        _gameState.PlayerReadyChanged += OnPlayerReadyChanged;

        if (_isLocal && _profile != null)
            _profile.AvatarUrlChanged += OnOwnAvatarUrlChanged;
    }

    private void OnDisable()
    {
        _roster.HPChanged -= OnHPChanged;
        _roster.NameChanged -= OnNameChanged;
        _roster.UserIdChanged -= OnUserIdChanged;
        _gameState.PlayerReadyChanged -= OnPlayerReadyChanged;

        if (_isLocal && _profile != null)
            _profile.AvatarUrlChanged -= OnOwnAvatarUrlChanged;
    }

    // ── Event handlers ────────────────────────────────────────────────────

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

    private void OnUserIdChanged(PlayerRef p, string userId)
    {
        if (!_bound || p != _playerRef) return;
        if (_isLocal) return; // local PFP comes from IProfileSubsystem, not UserId fetch
        // Opponent PFP: trigger avatar fetch by userId (fire-and-forget; result sets _avatarImage)
        FetchAvatarByUserId(userId);
    }

    private void OnPlayerReadyChanged(PlayerRef p, bool ready)
    {
        if (!_bound || p != _playerRef) return;
        try { if (_readyToggle != null) _readyToggle.isOn = ready; }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    /// <summary>
    /// Own avatar URL comes directly from IProfileSubsystem (local cache, no HTTP needed).
    /// </summary>
    private void OnOwnAvatarUrlChanged(string url)
    {
        if (!_isLocal || !_bound) return;
        // Avatar download is handled by a shared helper or left for a later pass.
        // For now, log receipt; wiring the actual Texture2D fetch is a polish task.
        Debug.Log($"[GameplayPlayerProfileUI] Own avatar URL: {url}");
    }

    // ── Initial refresh ───────────────────────────────────────────────────

    private void Refresh()
    {
        if (!_bound) return;

        var hp = _roster.GetHP(_playerRef);
        if (_hpText != null) _hpText.text = hp.ToString();

        var name = _roster.GetName(_playerRef);
        if (_nameText != null && !string.IsNullOrEmpty(name)) _nameText.text = name;

        var ready = _gameState.IsReady(_playerRef);
        if (_readyToggle != null) _readyToggle.isOn = ready;

        if (_isLocal && _profile != null && !string.IsNullOrEmpty(_profile.AvatarUrl))
            OnOwnAvatarUrlChanged(_profile.AvatarUrl);
        else if (!_isLocal)
        {
            var userId = _roster.GetUserId(_playerRef);
            if (!string.IsNullOrEmpty(userId)) FetchAvatarByUserId(userId);
        }
    }

    // ── Avatar fetch ──────────────────────────────────────────────────────

    private void FetchAvatarByUserId(string userId)
    {
        // TODO (polish): wire IHttpServiceSubsystem to download avatar Texture2D by userId
        // and assign to _avatarImage.texture. Deferred to avatar polish pass.
        Debug.Log($"[GameplayPlayerProfileUI] Opponent avatar fetch triggered for userId={userId}");
    }
}
