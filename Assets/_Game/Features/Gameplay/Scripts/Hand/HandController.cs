using System.Threading.Tasks;
using Zenject;

public class HandController : IHandController
{
    [Inject] private readonly IHandModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;

    public void Initialize() { }
    public void Dispose() { }

    public async Task PlayCard(string cardId)
    {
        _debugLogger.Log($"HandController: Requesting to play card {cardId}");
        _model.RequestPlayCard(cardId);
        await Task.Yield();
    }
}
