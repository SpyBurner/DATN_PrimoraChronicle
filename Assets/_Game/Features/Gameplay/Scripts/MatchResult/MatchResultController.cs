using System.Threading.Tasks;
using Zenject;

internal class MatchResultController : IMatchResultController
{
    [Inject] private readonly IMatchResultModel _model;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IDebugLogger _logger;

    private IMatchResultNetworkBridge _bridge;

    public void Initialize() { }

    public void Dispose() => _bridge = null;

    public void RegisterBridge(IMatchResultNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[MatchResult] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(GameMatchResult data) => _model.ApplyState(data);

    public async Task ReturnToLobby()
    {
        await _sceneLoader.LoadScene("Lobby");
    }
}
