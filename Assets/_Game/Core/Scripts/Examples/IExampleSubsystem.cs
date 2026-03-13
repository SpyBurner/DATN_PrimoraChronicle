using UnityEngine.Events;

public interface IExampleSubsystem : ISubsystem
{
    event UnityAction<bool> IsActiveChanged;
    event UnityAction<int> CounterChanged;

    // Controller methods forwarded
    System.Threading.Tasks.Task ToggleActive();
    void Increment();
    int GetCounter();
}