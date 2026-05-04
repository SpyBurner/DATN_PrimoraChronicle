using System.Threading.Tasks;
using UnityEngine.Events;

public interface IBattleSubsystem : ISubsystem
{
    event UnityAction<string> OpponentNameChanged;
    event UnityAction<int> OpponentLevelChanged;
    event UnityAction<int> PlayerHPChanged;
    event UnityAction<int> OpponentHPChanged;
    event UnityAction<int> PlayerMaxHPChanged;
    event UnityAction<int> OpponentMaxHPChanged;
    event UnityAction<bool> IsReadyChanged;

    Task InitializeBattleSetup();
    void SetIsReady(bool isReady);
}
