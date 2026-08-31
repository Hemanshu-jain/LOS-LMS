namespace LosLms.Server;

/// <summary>
/// A tiny append-only file logger. The Server package runs every process windowless, so there is no
/// console to watch — this file (next to the exe) is the record of what the launcher did and why a
/// startup stopped. Thread-safe because the supervise loop and the UI thread both write to it.
/// </summary>
internal static class Log
{
    private static readonly object Gate = new();

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(string message, Exception ex) => Write("ERROR", $"{message} :: {ex}");

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level,-5} {message}";
        lock (Gate)
        {
            try
            {
                File.AppendAllText(Paths.LogFile, line + Environment.NewLine);
            }
            catch
            {
                // Never let logging failures take down startup.
            }
        }
    }
}
