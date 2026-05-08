using System.Threading.Tasks;

public interface ICardDetailController : IController
{
    Task LoadCard(string cardId);
}
