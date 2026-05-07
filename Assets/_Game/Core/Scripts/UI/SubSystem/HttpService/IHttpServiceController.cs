using System.Threading.Tasks;

public interface IHttpServiceController : IController
{
    Task<T> Get<T>(string url);
    Task<T> Post<T, TRequest>(string url, TRequest payload) where TRequest : class;
    Task<string> Get(string url);
    Task<string> Post<TRequest>(string url, TRequest payload) where TRequest : class;
    void SetAuthToken(string token);
}
