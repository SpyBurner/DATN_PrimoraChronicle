public interface IDebugLogger
{
    void Log(string message);
    void LogError(string message);
    void LogWarning(string message);
    void SetLoggingEnabled(bool isEnabled);
}