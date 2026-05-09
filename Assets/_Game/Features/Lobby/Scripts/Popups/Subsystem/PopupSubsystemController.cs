using System.Threading.Tasks;

public interface IPopupSubsystemController
{
    void SetResult(object result);
    Task<T> WaitForResult<T>();
    void ClearResult();
    void Cancel();
}

public class PopupSubsystemController : IPopupSubsystemController
{
    private readonly IPopupSubsystemModel _model;

    public PopupSubsystemController(IPopupSubsystemModel model)
    {
        _model = model;
    }

    public void SetResult(object result)
    {
        _model.ResultTask.TrySetResult(result);
    }

    public async Task<T> WaitForResult<T>()
    {
        var result = await _model.ResultTask.Task;
        if (result is T typedResult)
        {
            return typedResult;
        }
        
        return default;
    }

    public void ClearResult()
    {
        _model.ResultTask = new TaskCompletionSource<object>();
    }

    public void Cancel()
    {
        _model.ResultTask.TrySetCanceled();
    }
}
