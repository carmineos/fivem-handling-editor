namespace HandlingEditor.Client
{
    public interface ILogger
    {
        void Log(LogLevel logLevel, string message);

        bool IsEnabled(LogLevel logLevel);
    }
}
