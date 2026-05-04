using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;
using System.Text;

internal class HttpServiceController : IHttpServiceController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IHttpServiceModel _model;

    private string _authToken;

    public void Initialize()
    {
        _authToken = null;
    }

    public void Dispose()
    {
        _authToken = null;
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _debugLogger.Log($"HttpService: Auth token set. Token length: {token?.Length ?? 0}");
    }

    public async Task<T> Get<T>(string url)
    {
        string response = await Get(url);
        try
        {
            return JsonUtility.FromJson<T>(response);
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"HttpService: Failed to deserialize response to {typeof(T).Name}: {ex.Message}");
            throw;
        }
    }

    public async Task<T> Post<T>(string url, object payload)
    {
        string response = await Post(url, payload);
        try
        {
            return JsonUtility.FromJson<T>(response);
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"HttpService: Failed to deserialize response to {typeof(T).Name}: {ex.Message}");
            throw;
        }
    }

    public async Task<string> Get(string url)
    {
        _model.IsRequesting.Value = true;
        _model.RequestQueueCount.Value++;

        try
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                AddAuthHeader(request);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    _debugLogger.LogError($"HttpService GET failed: {url} - {request.error}");
                    throw new Exception($"HTTP GET failed: {request.error}");
                }

                _debugLogger.Log($"HttpService GET success: {url}");
                return request.downloadHandler.text;
            }
        }
        finally
        {
            _model.RequestQueueCount.Value--;
            if (_model.RequestQueueCount.Value == 0)
            {
                _model.IsRequesting.Value = false;
            }
        }
    }

    public async Task<string> Post(string url, object payload)
    {
        _model.IsRequesting.Value = true;
        _model.RequestQueueCount.Value++;

        try
        {
            string jsonPayload = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                AddAuthHeader(request);

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    _debugLogger.LogError($"HttpService POST failed: {url} - {request.error}");
                    throw new Exception($"HTTP POST failed: {request.error}");
                }

                _debugLogger.Log($"HttpService POST success: {url}");
                return request.downloadHandler.text;
            }
        }
        finally
        {
            _model.RequestQueueCount.Value--;
            if (_model.RequestQueueCount.Value == 0)
            {
                _model.IsRequesting.Value = false;
            }
        }
    }

    private void AddAuthHeader(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(_authToken))
        {
            request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
        }
    }
}
