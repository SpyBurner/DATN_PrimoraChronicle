using System.Threading.Tasks;

public interface IPopupSubsystemModel
{
    TaskCompletionSource<object> ResultTask { get; set; }
}

public class PopupSubsystemModel : IPopupSubsystemModel
{
    public TaskCompletionSource<object> ResultTask { get; set; } = new TaskCompletionSource<object>();
}
