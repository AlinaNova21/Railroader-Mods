using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace AlinasRailTools.Editor.Logging;

/// <summary>
/// Custom Serilog sink that stores log events in a circular buffer for the in-game console
/// Keeps the last 10000 log lines and tracks the last log per system/source context
/// </summary>
public class InGameConsoleSink : ILogEventSink
{
    private readonly ITextFormatter _textFormatter;
    private readonly ConcurrentQueue<LogEntry> _logBuffer = new();
    private readonly ConcurrentDictionary<string, LogEntry> _lastLogPerSystem = new();
    private const int MaxBufferSize = 10000;

    public record LogEntry(
        DateTimeOffset Timestamp,
        LogEventLevel Level,
        string Message,
        string? SourceContext,
        Exception? Exception);

    public InGameConsoleSink(string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    {
        _textFormatter = new MessageTemplateTextFormatter(outputTemplate);
    }

    public void Emit(LogEvent logEvent)
    {
        // Extract source context (system name)
        string? sourceContext = null;
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue))
        {
            sourceContext = sourceContextValue.ToString().Trim('"');
        }

        // Format message
        using var writer = new StringWriter();
        _textFormatter.Format(logEvent, writer);
        var message = writer.ToString().TrimEnd('\r', '\n');

        var entry = new LogEntry(
            logEvent.Timestamp,
            logEvent.Level,
            message,
            sourceContext,
            logEvent.Exception);

        // Add to circular buffer
        _logBuffer.Enqueue(entry);

        // Trim buffer if it exceeds max size
        while (_logBuffer.Count > MaxBufferSize)
        {
            _logBuffer.TryDequeue(out _);
        }

        // Track last log per system
        if (sourceContext != null)
        {
            _lastLogPerSystem[sourceContext] = entry;
        }
    }

    /// <summary>
    /// Get all log entries in the buffer
    /// </summary>
    public IEnumerable<LogEntry> GetAllLogs() => _logBuffer.ToArray();

    /// <summary>
    /// Get the last log entry for each system
    /// </summary>
    public IReadOnlyDictionary<string, LogEntry> GetLastLogPerSystem() =>
        new Dictionary<string, LogEntry>(_lastLogPerSystem);

    /// <summary>
    /// Clear all logs from the buffer
    /// </summary>
    public void Clear()
    {
        _logBuffer.Clear();
        _lastLogPerSystem.Clear();
    }
}
