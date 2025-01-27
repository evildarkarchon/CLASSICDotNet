using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CLASSIC.Core.Logging;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class Logger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "CLASSIC Journal.log");
    private static readonly object LockObj = new();

    /// <summary>
    /// Configures the logger by checking and maintaining the state of the log file.
    /// If the log file exists and is older than 7 days, it deletes the file and logs a message
    /// indicating the change. This ensures that outdated logs do not accumulate.
    /// If an error occurs during the deletion process, the exception is logged to the console.
    /// </summary>
    public static void Configure()
    {
        if (!File.Exists(LogPath)) return;
        var fileInfo = new FileInfo(LogPath);
        var logAge = DateTime.Now - fileInfo.LastWriteTime;

        if (!(logAge.TotalDays > 7)) return;
        try
        {
            File.Delete(LogPath);
            Console.WriteLine("CLASSIC Journal.log has been deleted and regenerated due to being older than 7 days.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while deleting {LogPath}: {ex.Message}");
        }
    }

    public static void Debug(string message)
    {
        Log("DEBUG", message);
    }

    public static void Info(string message)
    {
        Log("INFO", message); 
    }

    public static void Error(string message)
    {
        Log("ERROR", message);
    }

    /// <summary>
    /// Logs a message with the specified log level to a file. The log entry includes the timestamp,
    /// log level, and the message, ensuring thread-safe operation by using a locking mechanism.
    /// </summary>
    /// <param name="level">The level of the log message (e.g., DEBUG, INFO, ERROR).</param>
    /// <param name="message">The message to log. Contains details about the event or error.</param>
    private static void Log(string level, string message)
    {
        lock (LockObj)
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {level} | {message}";
            File.AppendAllText(LogPath, logMessage + Environment.NewLine);
        }
    }
}