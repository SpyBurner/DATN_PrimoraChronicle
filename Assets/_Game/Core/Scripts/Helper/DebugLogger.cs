using System;
using System.IO;
using UnityEngine;

public class DebugLogger : IDebugLogger
{
    public bool _enableLogging = true;

    private StreamWriter _writer;
    private readonly long _sessionStartMs;

    public DebugLogger()
    {
        _sessionStartMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string logsDir = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(logsDir);

        string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".csv";
        string filePath = Path.Combine(logsDir, fileName);

        _writer = new StreamWriter(filePath, append: false);
        _writer.WriteLine("TIMESTAMP_MS, LOG_CODE, CLASS, MESSAGE, NETWORK_LATENCY_MS");
        _writer.Flush();

        Application.quitting += CloseFile;
    }

    public void Log(string logCode, string className, string message, float networkLatencyMs = 0)
    {
        if (!_enableLogging) return;
        string row = BuildRow(logCode, className, message, networkLatencyMs);
        Debug.Log(row);
        WriteToFile(row);
    }

    public void LogWarning(string logCode, string className, string message, float networkLatencyMs = 0)
    {
        if (!_enableLogging) return;
        string row = BuildRow(logCode, className, message, networkLatencyMs);
        Debug.LogWarning(row);
        WriteToFile(row);
    }

    public void LogError(string logCode, string className, string message, float networkLatencyMs = 0)
    {
        if (!_enableLogging) return;
        string row = BuildRow(logCode, className, message, networkLatencyMs);
        Debug.LogError(row);
        WriteToFile(row);
    }

    public void SetLoggingEnabled(bool isEnabled)
    {
        _enableLogging = isEnabled;
    }

    private string BuildRow(string logCode, string className, string message, float networkLatencyMs)
    {
        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _sessionStartMs;
        string escaped = message.Replace("\"", "\"\"");
        return $"{elapsed:D7}, {logCode}, {className}, \"{escaped}\", {networkLatencyMs:F0}";
    }

    private void WriteToFile(string row)
    {
        try
        {
            _writer?.WriteLine(row);
            _writer?.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void CloseFile()
    {
        Application.quitting -= CloseFile;
        _writer?.Close();
        _writer = null;
    }
}
