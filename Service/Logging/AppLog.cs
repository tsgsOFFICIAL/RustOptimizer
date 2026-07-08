using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.IO;
using System;

namespace RustOptimizer.Service.Logging;

/// <summary>
/// Provides file-based application logging with daily rotation, retention pruning, and level
/// filtering. Static (rather than DI-registered like the other services) so it is available
/// immediately at process start and from crash handlers, before the service container exists.
/// </summary>
public static class AppLog
{
    private const int RetentionDays = 30;

    private static readonly object Sync = new();
    private static string? _logFilePath;
    private static bool _handlersRegistered;

    /// <summary>
    /// Gets or sets the minimum level that gets written. Entries below this level are dropped
    /// before any I/O happens. Defaults to <see cref="LogLevel.Debug"/> in debug builds and
    /// <see cref="LogLevel.Info"/> otherwise.
    /// </summary>
    public static LogLevel MinimumLevel { get; set; } =
#if DEBUG
        LogLevel.Debug;
#else
        LogLevel.Info;
#endif

    /// <summary>
    /// Gets the directory path where daily log files are stored.
    /// </summary>
    public static string LogDirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RustOptimizer", "logs");

    /// <summary>
    /// Gets the path to the current daily log file, or <see langword="null"/> if the logger has not been initialized.
    /// </summary>
    public static string? LogFilePath => _logFilePath;

    /// <summary>
    /// Initializes the logger, creates today's log file if it does not already exist, and prunes
    /// log files older than <see cref="RetentionDays"/> days. Subsequent calls have no effect.
    /// </summary>
    public static void Initialize()
    {
        lock (Sync)
        {
            if (!string.IsNullOrWhiteSpace(_logFilePath))
                return;

            Directory.CreateDirectory(LogDirectoryPath);

            string date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            _logFilePath = Path.Combine(LogDirectoryPath, $"app-{date}.log");

            PruneOldLogs();

            Write(LogLevel.Info, "AppLog",
                $"Logger initialized. Version={SafeGetVersion()}, OS={Environment.OSVersion.VersionString}, " +
                $"64-bit process={Environment.Is64BitProcess}");
        }
    }

    /// <summary>
    /// Hooks <see cref="AppDomain.UnhandledException"/> and <see cref="TaskScheduler.UnobservedTaskException"/>
    /// so crashes and swallowed task faults are always captured. Safe to call multiple times.
    /// </summary>
    public static void RegisterGlobalExceptionHandlers()
    {
        lock (Sync)
        {
            if (_handlersRegistered)
                return;

            _handlersRegistered = true;

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Write(LogLevel.Fatal, "UnhandledException", $"Terminating={e.IsTerminating}", e.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Write(LogLevel.Error, "UnobservedTaskException", "An unobserved task exception occurred.", e.Exception);
                e.SetObserved();
            };
        }
    }

    /// <summary>Writes a debug message. Filtered out unless <see cref="MinimumLevel"/> allows it.</summary>
    public static void Debug(string scope, string message) => Write(LogLevel.Debug, scope, message);

    /// <summary>Writes an informational message.</summary>
    public static void Info(string scope, string message) => Write(LogLevel.Info, scope, message);

    /// <summary>Writes a warning message, optionally including exception details.</summary>
    public static void Warn(string scope, string message, Exception? ex = null) => Write(LogLevel.Warn, scope, message, ex);

    /// <summary>Writes an error message, optionally including exception details.</summary>
    public static void Error(string scope, string message, Exception? ex = null) => Write(LogLevel.Error, scope, message, ex);

    /// <summary>Writes a fatal message, optionally including exception details.</summary>
    public static void Fatal(string scope, string message, Exception? ex = null) => Write(LogLevel.Fatal, scope, message, ex);

    /// <summary>
    /// Opens the log directory in the OS file explorer, creating it first if necessary.
    /// </summary>
    public static void OpenLogDirectory()
    {
        Directory.CreateDirectory(LogDirectoryPath);
        Utility.OpenUrl(LogDirectoryPath);
    }

    private static void Write(LogLevel level, string scope, string message, Exception? ex = null)
    {
        if (level < MinimumLevel)
            return;

        try
        {
            if (string.IsNullOrWhiteSpace(_logFilePath))
                Initialize();

            string ts = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
            string line = $"[{ts}] [{level.ToString().ToUpperInvariant(),-5}] [{scope}] {message}";

            if (ex != null)
                line += FormatException(ex);

            lock (Sync)
                File.AppendAllText(_logFilePath!, line + Environment.NewLine, Encoding.UTF8);

            Trace.WriteLine(line);
        }
        catch (Exception writeEx)
        {
            // The logger must never be the reason the app crashes.
            Trace.WriteLine($"[AppLog-Fallback] {writeEx.GetType().Name}: {writeEx.Message}");
        }
    }

    /// <summary>
    /// Formats an exception (and any inner exceptions) as indented, appendable log lines including stack traces.
    /// </summary>
    private static string FormatException(Exception ex)
    {
        StringBuilder sb = new();
        Exception? current = ex;

        while (current != null)
        {
            sb.Append(Environment.NewLine).Append("    ").Append(current.GetType().FullName).Append(": ").Append(current.Message);

            if (current.StackTrace != null)
            {
                foreach (string frame in current.StackTrace.Split('\n'))
                    sb.Append(Environment.NewLine).Append("        ").Append(frame.TrimEnd('\r'));
            }

            current = current.InnerException;
            if (current != null)
                sb.Append(Environment.NewLine).Append("    ---> Inner Exception:");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Deletes log files older than <see cref="RetentionDays"/> days. Best-effort: a single
    /// unreadable/locked file is skipped rather than aborting the whole prune pass.
    /// </summary>
    private static void PruneOldLogs()
    {
        try
        {
            DateTime cutoff = DateTime.Now.Date.AddDays(-RetentionDays);

            foreach (string file in Directory.EnumerateFiles(LogDirectoryPath, "app-*.log"))
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string datePart = name.Length > 4 ? name[4..] : "";

                    if (DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate)
                        && fileDate < cutoff)
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private static string SafeGetVersion()
    {
        try
        {
            return Utility.GetDisplayVersion();
        }
        catch
        {
            return "N/A";
        }
    }
}