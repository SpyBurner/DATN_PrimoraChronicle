using System.Threading.Tasks;

public interface IHttpServiceController : IController
{
    Task<T> Get<T>(string url);
    Task<T> Post<T>(string url, object payload);
    Task<string> Get(string url);
    Task<string> Post(string url, object payload);
    void SetAuthToken(string token);
}
