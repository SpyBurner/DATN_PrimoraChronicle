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

    void Refresh();
}
