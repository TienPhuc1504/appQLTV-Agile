using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.Logging;

public sealed class DailyFileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly object _writeLock = new();
    private bool _disposed;

    public DailyFileLoggerProvider(string logDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
        string expandedPath =
            Environment.ExpandEnvironmentVariables(logDirectory);
        _logDirectory = Path.IsPathRooted(expandedPath)
            ? Path.GetFullPath(expandedPath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedPath));
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new DailyFileLogger(categoryName, WriteLogEntry);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void WriteLogEntry(
        LogLevel logLevel,
        string categoryName,
        EventId eventId,
        string message,
        Exception? exception)
    {
        if (_disposed)
        {
            return;
        }

        string timestamp =
            DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        var builder = new StringBuilder()
            .Append(timestamp)
            .Append(" [")
            .Append(logLevel)
            .Append("] ")
            .Append(categoryName);
        if (eventId.Id != 0)
        {
            builder.Append(" (").Append(eventId.Id).Append(')');
        }

        builder.Append(": ").AppendLine(message);
        if (exception is not null)
        {
            builder.AppendLine(exception.ToString());
        }

        string logFilePath = Path.Combine(
            _logDirectory,
            $"library-{DateTime.Now:yyyyMMdd}.log");
        try
        {
            lock (_writeLock)
            {
                File.AppendAllText(
                    logFilePath,
                    builder.ToString(),
                    Encoding.UTF8);
            }
        }
        catch
        {
            // Logging không được làm gián đoạn luồng nghiệp vụ của ứng dụng.
        }
    }

    private sealed class DailyFileLogger(
        string categoryName,
        Action<LogLevel, string, EventId, string, Exception?> writeLogEntry)
        : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (!IsEnabled(logLevel))
            {
                return;
            }

            writeLogEntry(
                logLevel,
                categoryName,
                eventId,
                formatter(state, exception),
                exception);
        }
    }
}
