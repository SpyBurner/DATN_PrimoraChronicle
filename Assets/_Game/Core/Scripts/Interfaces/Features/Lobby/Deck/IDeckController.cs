using System.Threading.Tasks;

public interface IDeckController : IController
{
    Task LoadDecks();
}
