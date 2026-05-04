using System.Threading.Tasks;
using UnityEngine.Events;

public interface ILobbyMainSubsystem : ISubsystem
{
    event UnityAction<string> UsernameChanged;
    event UnityAction<int> LevelChanged;
    event UnityAction<int> GoldChanged;
    event UnityAction<string> AvatarUrlChanged;

    Task Logout();
}
