using System.Threading.Tasks;
using Zenject;

public class ExampleController : IExampleController
{
    readonly IExampleModel _model;

    [Inject]
    public ExampleController(IExampleModel model)
    {
        _model = model;
    }

    public async Task ToggleActive()
    {
        // Example of doing async work then updating model
        var next = !_model.IsActive.Value;
        await Task.Yield(); // placeholder for real async work
        _model.IsActive.Value = next;
    }

    public void Increment()
    {
        _model.Counter.Value = _model.Counter.Value + 1;
    }

    public int GetCounter() => _model.Counter.Value;
}