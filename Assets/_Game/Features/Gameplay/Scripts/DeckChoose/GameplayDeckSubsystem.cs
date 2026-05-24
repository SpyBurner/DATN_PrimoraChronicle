using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

public class GameplayDeckSubsystem : IGameplayDeckSubsystem
{
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly IAuthSessionModel _authSession;
    [Inject] private readonly IDebugLogger _logger;

    public event UnityAction<IReadOnlyList<DeckSummaryData>> DecksChanged;

    private List<DeckSummaryData> _cache;
    private bool _loading;

    public void Initialize() { }

    public void Dispose()
    {
        _cache = null;
        _loading = false;
    }

    public async Task LoadDecks()
    {
        if (_cache != null)
        {
            FireDecksChanged(_cache);
            return;
        }

        if (_loading) return;
        _loading = true;

        try
        {
            string userId = _authSession.CurrentUserId.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("LOG_GAMEPLAYDECKSUBSYSTEM", nameof(GameplayDeckSubsystem), "No user ID — cannot load decks.");
                return;
            }

            string encoded = Uri.EscapeDataString(userId);
            var response = await _httpService.Get<GameplayDecksListResponse>($"/api/decks?user_id={encoded}");
            _cache = response?.decks != null
                ? new List<DeckSummaryData>(response.decks)
                : new List<DeckSummaryData>();
            FireDecksChanged(_cache);
        }
        catch (Exception ex)
        {
            _logger.LogError("LOG_GAMEPLAYDECKSUBSYSTEM", nameof(GameplayDeckSubsystem), $"LoadDecks failed: {ex.Message}");
        }
        finally
        {
            _loading = false;
        }
    }

    private void FireDecksChanged(IReadOnlyList<DeckSummaryData> decks)
    {
        try { DecksChanged?.Invoke(decks); }
        catch (Exception ex) { Debug.LogException(ex); }
    }
}

[Serializable]
internal class GameplayDecksListResponse
{
    public DeckSummaryData[] decks;
}
