using System.Threading.Tasks;

public interface IPopupSubsystem
{
    void SetResult(object result);
    Task<T> WaitForResult<T>();
    void ClearResult();
    void Cancel();
}
