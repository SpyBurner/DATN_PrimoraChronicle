using System.Threading.Tasks;
using Zenject;

public class PopupSubsystem : IPopupSubsystem
{
    [Inject] private readonly IPopupSubsystemController _controller;

    public void SetResult(object result) => _controller.SetResult(result);
    public Task<T> WaitForResult<T>() => _controller.WaitForResult<T>();
    public void ClearResult() => _controller.ClearResult();
    public void Cancel() => _controller.Cancel();
}
