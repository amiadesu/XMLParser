using System;
using System.IO;
using System.Threading;
using XMLParser.Constants;

namespace XMLParser.Components.Logging;

public sealed class Logger
{
    private static readonly Lazy<Logger> _instance = new(() => new Logger(), LazyThreadSafetyMode.ExecutionAndPublication);
    public static Logger Instance => _instance.Value;

    private readonly string _logFilePath;
    private readonly ReaderWriterLockSlim _lock = new();

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    private Logger()
    {
        string folderPath = Microsoft.Maui.Storage.FileSystem.Current.AppDataDirectory;
        _logFilePath = Path.Combine(folderPath, Literals.defaultLogFileName);

        if (!File.Exists(_logFilePath))
            File.Create(_logFilePath).Dispose();
    }

    public void Log(LogLevel level, string message, Exception? ex = null)
    {
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logLine = $"[{time}] [{level}] {message}";

        if (ex != null)
            logLine += $"{Environment.NewLine}Exception: {ex}";

        WriteLine(logLine);
    }

    private void WriteLine(string line)
    {
        _lock.EnterWriteLock();
        try
        {
            File.AppendAllText(_logFilePath, line + Environment.NewLine);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warn(string message, Exception? ex = null) => Log(LogLevel.Warning, message, ex);
    public void Error(string message, Exception? ex = null) => Log(LogLevel.Error, message, ex);
}
