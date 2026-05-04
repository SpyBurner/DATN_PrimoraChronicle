using System;
using System.Threading.Tasks;
using UnityEngine.Events;

public interface IHttpServiceSubsystem : ISubsystem
{
    event UnityAction<int> RequestQueueCountChanged;
    event UnityAction<bool> IsRequestingChanged;

    Task<T> Get<T>(string url);
    Task<T> Post<T>(string url, object payload);
    Task<string> Get(string url);
    Task<string> Post(string url, object payload);
    void SetAuthToken(string token);
}
