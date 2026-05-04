using System.Threading.Tasks;
using UnityEngine.Events;

public interface ICardDetailSubsystem : ISubsystem
{
    event UnityAction<string> CardNameChanged;
    event UnityAction<string> CardDescriptionChanged;
    event UnityAction<int> CardCostChanged;
    event UnityAction<int> CardPowerChanged;
    event UnityAction<string> CardImageUrlChanged;

    Task LoadCard(string cardId);
}
