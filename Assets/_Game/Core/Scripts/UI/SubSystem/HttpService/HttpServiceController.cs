using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;
using System.Text;
using Core.Config;

internal class HttpServiceController : IHttpServiceController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IHttpServiceModel _model;
    
    // Config can be optionally injected if bound in CoreInstaller, otherwise fallback
    [InjectOptional] private readonly ServerConfig _serverConfig;

    private string _authToken;
    
    private string GetBaseUrl()
    {
        return _serverConfig != null ? _serverConfig.ApiBaseUrl : "http://localhost:8000";
    }
    
    private string FormatUrl(string url)
    {
        if (url.StartsWith("http://") || url.StartsWith("https://"))
            return url;
            
        return _serverConfig != null ? _serverConfig.GetFullUrl(url) : $"{GetBaseUrl()}/{url.TrimStart('/')}";
    }

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

    public async Task<T> Post<T, TRequest>(string url, TRequest payload) where TRequest : class
    {
        string response = await Post<TRequest>(url, payload);
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
        string formattedUrl = FormatUrl(url);
        _model.IsRequesting.Value = true;
        _model.RequestQueueCount.Value++;

        try
        {
            using (UnityWebRequest request = UnityWebRequest.Get(formattedUrl))
            {
                AddAuthHeader(request);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorDetail = request.downloadHandler?.text;
                    _debugLogger.LogError($"HttpService GET failed: {formattedUrl} - {request.error}\nDetail: {errorDetail}");
                    throw new Exception($"HTTP GET failed: {request.error}");
                }

                _debugLogger.Log($"HttpService GET success: {formattedUrl}");
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

    public async Task<string> Post<TRequest>(string url, TRequest payload) where TRequest : class
    {
        string formattedUrl = FormatUrl(url);
        _model.IsRequesting.Value = true;
        _model.RequestQueueCount.Value++;

        try
        {
            string jsonPayload = JsonUtility.ToJson(payload);
            _debugLogger.Log($"HttpService: POST payload: {jsonPayload}");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(formattedUrl, "POST"))
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
                    string errorDetail = request.downloadHandler?.text;
                    _debugLogger.LogError($"HttpService POST failed: {formattedUrl} - {request.error}\nDetail: {errorDetail}");
                    throw new Exception($"HTTP POST failed: {request.error}");
                }

                _debugLogger.Log($"HttpService POST success: {formattedUrl}");
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
