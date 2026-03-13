using System.Threading.Tasks;

public interface IExampleController
{
    Task ToggleActive();
    void Increment();
    int GetCounter();
}