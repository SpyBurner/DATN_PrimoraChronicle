using System.Threading.Tasks;
using UnityEngine.Events;

public interface IProfileSubsystem : ISubsystem
{
    event UnityAction<string> UsernameChanged;
    event UnityAction<int> LevelChanged;
    event UnityAction<int> XpChanged;
    event UnityAction<int> XpToNextLevelChanged;
    event UnityAction<int> GoldChanged;
    event UnityAction<string> AvatarUrlChanged;

    public string Username { get; }
    public int Level { get; }
    public int Xp { get; }
    public int XpToNextLevel { get; }
    public int Gold { get; }
    public string AvatarUrl { get; }

    void Refresh();
}
