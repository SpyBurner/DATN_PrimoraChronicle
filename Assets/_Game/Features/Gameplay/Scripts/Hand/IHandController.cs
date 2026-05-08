using System.Threading.Tasks;

public interface IHandController : IController
{
    Task PlayCard(string cardId);
}
