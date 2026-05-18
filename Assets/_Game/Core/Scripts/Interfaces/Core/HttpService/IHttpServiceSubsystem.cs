using System;
using System.Threading.Tasks;
using UnityEngine.Events;

public interface IHttpServiceSubsystem : ISubsystem
{
    event UnityAction<int> RequestQueueCountChanged;
    event UnityAction<bool> IsRequestingChanged;

    Task<T> Get<T>(string url);
    Task<T> Post<T, TRequest>(string url, TRequest payload) where TRequest : class;
    Task<string> Get(string url);
    Task<string> Post<TRequest>(string url, TRequest payload) where TRequest : class;
    Task<string> Delete(string url);
    void SetAuthToken(string token);
}
