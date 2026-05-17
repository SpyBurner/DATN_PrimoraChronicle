using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

internal class BackendBridgeController : IBackendBridgeController
{
    [Inject] private readonly IBackendBridgeModel _model;
    [Inject] private readonly IHttpServiceSubsystem _http;
    [Inject] private readonly IDebugLogger _logger;

    private HttpListener _listener;
    private Thread _listenThread;
    private bool _isDisposed;

    public void Initialize()
    {
        if (!Application.isBatchMode)
        {
            _logger.Log("[BackendBridge] Non-headless build detected. Subsystem initialized but inactive.");
            return;
        }

        _logger.Log("[BackendBridge] Headless build detected. Starting HttpListener...");
        try
        {
            _listener = new HttpListener();
            try
            {
                _listener.Prefixes.Add("http://*:7070/");
                _listener.Start();
                _logger.Log("[BackendBridge] HttpListener started on http://*:7070/");
            }
            catch (HttpListenerException ex)
            {
                _logger.LogWarning($"[BackendBridge] Failed to bind to *, falling back to localhost: {ex.Message}");
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:7070/");
                _listener.Start();
                _logger.Log("[BackendBridge] HttpListener started on http://localhost:7070/");
            }

            _listenThread = new Thread(ListenLoop)
            {
                IsBackground = true
            };
            _listenThread.Start();

            _model.ApplyState(new BackendBridgeStateData
            {
                PendingStartSession = null,
                IsListening = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BackendBridge] Failed to start HttpListener: {ex.Message}");
        }
    }

    public void ClearPendingStartSession()
    {
        _logger.Log("[BackendBridge] Clearing pending StartSessionCommand...");
        _model.ApplyState(new BackendBridgeStateData
        {
            PendingStartSession = null,
            IsListening = _model.IsListening.Value
        });
    }

    public void Dispose()

    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BackendBridge] Exception during HttpListener shutdown: {ex.Message}");
        }

        _model.ApplyState(new BackendBridgeStateData
        {
            PendingStartSession = null,
            IsListening = false
        });
    }

    private void ListenLoop()
    {
        while (!_isDisposed && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(HandleRequest, context);
            }
            catch (Exception)
            {
                break;
            }
        }
    }

    private void HandleRequest(object state)
    {
        var context = (HttpListenerContext)state;
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = request.Url.AbsolutePath;
            var method = request.HttpMethod;

            _logger.Log($"[BackendBridge] Inbound request: {method} {path}");

            if (method == "POST" && path == "/start-session")
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();

                var cmd = JsonConvert.DeserializeObject<StartSessionCommand>(body);
                if (cmd == null || string.IsNullOrEmpty(cmd.SessionName))
                {
                    WriteResponse(response, 400, "{\"error\": \"Invalid StartSessionCommand\"}");
                    return;
                }

                _logger.Log($"[BackendBridge] StartSession command received: {cmd.SessionName}");

                MainThreadDispatcher.Enqueue(() =>
                {
                    _model.ApplyState(new BackendBridgeStateData
                    {
                        PendingStartSession = cmd,
                        IsListening = true
                    });
                });

                WriteResponse(response, 200, "{\"status\": \"accepted\"}");
            }
            else if (method == "POST" && path == "/force-end-match")
            {
                _logger.Log("[BackendBridge] ForceEndMatch command received.");
                // For now, write a placeholder or trigger the event
                WriteResponse(response, 200, "{\"status\": \"accepted\"}");
            }
            else
            {
                WriteResponse(response, 404, "{\"error\": \"Not Found\"}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BackendBridge] Error handling request: {ex.Message}");
            try
            {
                WriteResponse(response, 500, $"{{\"error\": \"{ex.Message}\"}}");
            }
            catch { }
        }
    }

    private void WriteResponse(HttpListenerResponse response, int statusCode, string json)
    {
        try
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BackendBridge] Error writing response: {ex.Message}");
        }
    }

    public async Task ReportMatchResultAsync(MatchResultData result)
    {
        _logger.Log($"[BackendBridge] Reporting match result to backend: {result.SessionName}");
        try
        {
            await _http.Post<string, MatchResultData>("/api/matches/result", result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BackendBridge] Failed to report match result: {ex.Message}");
        }
    }

    public async Task ReportPlayerDisconnectedAsync(string userId)
    {
        _logger.Log($"[BackendBridge] Reporting player disconnected: {userId}");
        try
        {
            var payload = new PlayerDisconnectedPayload { userId = userId };
            await _http.Post<string, PlayerDisconnectedPayload>("/api/players/disconnected", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BackendBridge] Failed to report player disconnect: {ex.Message}");
        }
    }
}
