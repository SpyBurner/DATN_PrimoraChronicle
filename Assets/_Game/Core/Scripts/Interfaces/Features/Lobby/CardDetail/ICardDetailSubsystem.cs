using System.Threading.Tasks;
using UnityEngine.Events;

public interface ICardDetailSubsystem : ISubsystem
{
    event UnityAction<string> CardNameChanged;
    event UnityAction<string> CardDescriptionChanged;
    event UnityAction<int> CardCostChanged;
    event UnityAction<int> CardPowerChanged;
    event UnityAction<string> CardImageUrlChanged;
    event UnityAction<string> SkillNameChanged;
    event UnityAction<string> SkillDescriptionChanged;
    event UnityAction<string> SkillPatternChanged;

    Task LoadCard(string cardId);
}
