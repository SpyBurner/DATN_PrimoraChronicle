using UnityEngine;

/// <summary>
/// A singleton logger class for debugging purposes.
/// </summary>
public class DebugLogger : IDebugLogger
{
    public bool _enableLogging = true;
    public void Log(string message)
    {
        if (_enableLogging)
        {
            Debug.Log(message);
        }
    }

    public void LogWarning(string message)
    {
        if (_enableLogging)
        {
            Debug.LogWarning(message);
        }
    }

    public void LogError(string message)
    {
        if (_enableLogging)
        {
            Debug.LogError(message);
        }
    }

    public void SetLoggingEnabled(bool isEnabled)
    {
        _enableLogging = isEnabled;
    }
}
