using System.Threading.Tasks;

public interface IMatchHistoryController : IController
{
    Task LoadMatchHistory();
}
