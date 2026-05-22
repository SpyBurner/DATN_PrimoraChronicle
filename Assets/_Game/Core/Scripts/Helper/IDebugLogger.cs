public interface IDebugLogger
{
    void Log(string logCode, string className, string message, float networkLatencyMs = 0);
    void LogWarning(string logCode, string className, string message, float networkLatencyMs = 0);
    void LogError(string logCode, string className, string message, float networkLatencyMs = 0);
    void SetLoggingEnabled(bool isEnabled);
}
