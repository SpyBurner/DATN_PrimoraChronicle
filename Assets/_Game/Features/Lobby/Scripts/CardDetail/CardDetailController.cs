using System;
using System.Threading.Tasks;
using Zenject;

internal class CardDetailController : ICardDetailController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly ICardDetailModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;

    public void Initialize() { }
    public void Dispose() { }

    public async Task LoadCard(string cardId)
    {
        try
        {
            _debugLogger.Log($"CardDetail: Loading card {cardId}");
            var response = await _httpService.Get<CardDetailResponse>($"/api/collection/cards/{cardId}");

            if (response != null)
            {
                _model.SetCardName(response.name);
                _model.SetCardDescription(response.description);
                _model.SetCardCost(response.cost);
                _model.SetCardPower(response.power);
                _model.SetCardImageUrl(response.imageUrl);
                _debugLogger.Log($"CardDetail: Loaded card {response.name}");
            }
            else
            {
                _debugLogger.LogError("CardDetail: Failed to load card");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"CardDetail: LoadCard failed: {ex.Message}");
        }
    }
}

[System.Serializable]
internal class CardDetailResponse
{
    public string name;
    public string description;
    public int cost;
    public int power;
    public string imageUrl;
}
