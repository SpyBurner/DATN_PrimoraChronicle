using UnityEngine;
using Zenject;

internal class BattleController : IBattleController
{
    [Inject] private readonly IBattleModel _model;

    public void Initialize() { }
    public void Dispose() { }

    public void StartMatchmaking()
    {
        Debug.Log("Battle: Start Matchmaking");
    }
}
