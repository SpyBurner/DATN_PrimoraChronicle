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
                    string readableError = InterceptError(request, errorDetail);
                    _debugLogger.LogError($"HttpService GET failed: {formattedUrl} - {request.error}\nDetail: {errorDetail}\nReadable: {readableError}");
                    throw new Exception(readableError);
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
                    string readableError = InterceptError(request, errorDetail);
                    _debugLogger.LogError($"HttpService POST failed: {formattedUrl} - {request.error}\nDetail: {errorDetail}\nReadable: {readableError}");
                    throw new Exception(readableError);
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

    public async Task<string> Delete(string url)
    {
        string formattedUrl = FormatUrl(url);
        _model.IsRequesting.Value = true;
        _model.RequestQueueCount.Value++;

        try
        {
            using (UnityWebRequest request = UnityWebRequest.Delete(formattedUrl))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                AddAuthHeader(request);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorDetail = request.downloadHandler?.text;
                    string readableError = InterceptError(request, errorDetail);
                    _debugLogger.LogError($"HttpService DELETE failed: {formattedUrl} - {request.error}\nDetail: {errorDetail}\nReadable: {readableError}");
                    throw new Exception(readableError);
                }

                _debugLogger.Log($"HttpService DELETE success: {formattedUrl}");
                return request.downloadHandler?.text ?? "";
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

    private string InterceptError(UnityWebRequest request, string detail)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError)
            return HttpErrors.NETWORK_ERROR;
            
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            // Try to parse backend detail
            if (!string.IsNullOrEmpty(detail))
            {
                try 
                {
                    var errorObj = JsonUtility.FromJson<BackendError>(detail);
                    if (!string.IsNullOrEmpty(errorObj.detail))
                    {
                        return MapBackendDetail(errorObj.detail);
                    }
                }
                catch { /* Ignore parse error */ }
            }

            return request.responseCode switch
            {
                401 => HttpErrors.UNAUTHORIZED,
                403 => HttpErrors.FORBIDDEN,
                404 => HttpErrors.NOT_FOUND,
                >= 500 => HttpErrors.SERVER_ERROR,
                _ => HttpErrors.DEFAULT
            };
        }

        return HttpErrors.DEFAULT;
    }

    private string MapBackendDetail(string detail)
    {
        // Exact matches for backend strings defined in TestBE main.py
        return detail switch
        {
            "Invalid credentials" => HttpErrors.INVALID_CREDENTIALS,
            "Username already registered" => HttpErrors.USERNAME_TAKEN,
            "Deck must contain exactly 20 cards" => HttpErrors.DECK_SIZE_INVALID,
            _ when detail.Contains("does not own enough copies") => HttpErrors.CARD_NOT_OWNED,
            _ when detail.Contains("not found in GDS") => HttpErrors.CARD_NOT_FOUND,
            _ when detail.Contains("is not 'Common'") => HttpErrors.CARD_INVALID_RARITY,
            _ => detail // Return original if no specific mapping, or fallback to DEFAULT if preferred
        };
    }

    [Serializable]
    private class BackendError
    {
        public string detail;
    }
}
