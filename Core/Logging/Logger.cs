using System;
using System.IO;

namespace CLASSIC.Core.Logging;

public static class Logger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "CLASSIC Journal.log");
    private static readonly object LockObj = new();

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

    private static void Log(string level, string message)
    {
        lock (LockObj)
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {level} | {message}";
            File.AppendAllText(LogPath, logMessage + Environment.NewLine);
        }
    }
}