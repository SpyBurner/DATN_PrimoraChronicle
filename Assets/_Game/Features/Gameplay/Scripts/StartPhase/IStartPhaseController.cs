using System.Threading.Tasks;

public interface IStartPhaseController : IController
{
    void SelectChampion(int championId);
    void ConfirmSelection();
}
