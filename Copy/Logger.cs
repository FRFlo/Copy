using Copy.Types;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Copy
{
    internal static class LoggerService
    {
        private static ILoggerFactory? _loggerFactory;
        private static ILogger? _logger;
        private static bool _debug;

        public static void Initialize(bool debug = false)
        {
            _debug = debug;
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(debug ? LogLevel.Debug : LogLevel.Information)
                    .AddConsole();
            });

            _logger = _loggerFactory.CreateLogger("Copy");
        }

        public static void Debug(string message)
        {
            _logger?.LogDebug(message);
        }

        public static void Info(string message)
        {
            _logger?.LogInformation(message);
        }

        public static void Warning(string message)
        {
            _logger?.LogWarning(message);
        }

        public static void Error(string message)
        {
            _logger?.LogError(message);
        }

        public static bool IsDebugEnabled()
        {
            return _debug;
        }

        public static void Dispose()
        {
            _loggerFactory?.Dispose();
            _loggerFactory = null;
            _logger = null;
        }
    }

    /// <summary>
    ///     Logger class.
    /// </summary>
    public static class Logger
    {
        private sealed class LogScopeState(IReadOnlyDictionary<string, string> values, LogScopeState? parent)
        {
            public IReadOnlyDictionary<string, string> Values { get; } = values;
            public LogScopeState? Parent { get; } = parent;
        }

        private sealed class LogScope(LogScopeState? previousState) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _scope.Value = previousState;
                _disposed = true;
            }
        }

        private static readonly AsyncLocal<LogScopeState?> _scope = new();
        private static readonly AsyncLocal<bool> _isSendingMailNotification = new();
        private static EmailNotifier? _emailNotifier;

        /// <summary>
        ///     Path to the log file.
        ///     <list type="bullet">
        ///         <item>
        ///             <description>In debug mode, it is located in the program folder with the name "debug.log".</description>
        ///         </item>
        ///         <item>
        ///             <description>In production mode, it is located in the program folder with the name "production.log".</description>
        ///         </item>
        ///     </list>
        /// </summary>
        public static string LogFilePath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(),
#if DEBUG
            "debug.log");
#else
            "production.log");
#endif
        /// <summary>
        ///     Indicates if the logger should write logs to the file.
        /// </summary>
        public static bool LogToFile { get; set; } = true;

        internal static void ConfigureNotifications(SmtpSettings? smtp, string[] recipients)
        {
            _emailNotifier = EmailNotifier.Create(recipients, smtp);
        }

        /// <summary>
        ///     Push contextual properties that will automatically be appended to every log line
        ///     within the current async flow.
        /// </summary>
        /// <param name="properties">Properties to attach to the current log scope.</param>
        /// <returns>A disposable scope that restores the previous context when disposed.</returns>
        public static IDisposable BeginScope(params (string Key, string? Value)[] properties)
        {
            Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
            foreach ((string key, string? value) in properties)
            {
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    values[key] = value;
                }
            }

            LogScopeState? previousState = _scope.Value;
            _scope.Value = values.Count == 0 ? previousState : new LogScopeState(values, previousState);
            return new LogScope(previousState);
        }

        /// <summary>
        ///     Shows a message in the console and writes it to the log file.
        /// </summary>
        /// <param name="prefix">Prefix of the message</param>
        /// <param name="prefixColor">Color of the prefix</param>
        /// <param name="message">Message to show</param>
        /// <param name="color">Color of the message</param>
        /// <param name="icon">Icon to show before the message</param>
        private static void Print(string prefix, ConsoleColor prefixColor, string message,
            ConsoleColor color = ConsoleColor.White, LoggerIcon? icon = null)
        {
            StringBuilder sb = new();
            string contextPrefix = BuildContextPrefix();
            if (message.EndsWith('\n'))
            {
                message = message[..^1];
            }

            if (prefix != "DEBUG" || LoggerService.IsDebugEnabled())
            {
                foreach (string line in message.Split('\n'))
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($"{DateTime.Now:dd/MM/yyyy, HH:mm:fff} ");
                    Console.BackgroundColor = prefixColor;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(prefix);
                    if (icon != null)
                    {
                        Console.BackgroundColor = icon.BackgroundColor;
                        Console.ForegroundColor = icon.ForegroundColor;
                        Console.Write($" {icon.Icon} ");
                    }

                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = color;
                    Console.WriteLine($" {contextPrefix}{line}");
                    Console.ResetColor();

                    sb.Append($"{DateTime.Now:dd/MM/yyyy, HH:mm:fff} {prefix} {contextPrefix}{line}\n");
                }
            }

            string final = sb.ToString();

            if (LogToFile)
            {
                File.AppendAllText(LogFilePath, final, Encoding.UTF8);
            }

            TrySendNotification(prefix, contextPrefix, message);
        }

        /// <summary>
        ///     Shows a debug message in the console and writes it to the log file.
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="icon">Icon to show before the message</param>
        public static void Debug(string message, LoggerIcon? icon = null)
        {
            Print("DEBUG", ConsoleColor.DarkGreen, message, ConsoleColor.Green, icon);
        }

        /// <summary>
        ///     Shows a info message in the console and writes it to the log file.
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="icon">Icon to show before the message</param>
        public static void Info(string message, LoggerIcon? icon = null)
        {
            Print("INFO", ConsoleColor.DarkBlue, message, ConsoleColor.Blue, icon);
        }

        /// <summary>
        ///     Shows a warn message in the console and writes it to the log file.
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="icon">Icon to show before the message</param>
        public static void Warn(string message, LoggerIcon? icon = null)
        {
            Print("WARN", ConsoleColor.DarkYellow, message, ConsoleColor.Yellow, icon);
        }

        /// <summary>
        ///     Shows a error message in the console and writes it to the log file.
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="icon">Icon to show before the message</param>
        public static void Error(string message, LoggerIcon? icon = null)
        {
            Print("ERREUR", ConsoleColor.DarkRed, message, ConsoleColor.Red, icon);
        }

        /// <summary>
        ///     Shows an error message together with the exception details.
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="exception">Exception to print with stack trace</param>
        /// <param name="icon">Icon to show before the message</param>
        public static void Error(string message, Exception exception, LoggerIcon? icon = null)
        {
            Print("ERREUR", ConsoleColor.DarkRed, $"{message}{Environment.NewLine}{exception}", ConsoleColor.Red, icon);
        }

        private static void TrySendNotification(string prefix, string contextPrefix, string message)
        {
            if (_emailNotifier == null || _isSendingMailNotification.Value)
            {
                return;
            }

            if (prefix != "WARN" && prefix != "ERREUR")
            {
                return;
            }

            try
            {
                _isSendingMailNotification.Value = true;
                _emailNotifier.Send(prefix, contextPrefix, message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to send {prefix} notification email: {ex}");
            }
            finally
            {
                _isSendingMailNotification.Value = false;
            }
        }

        private static string BuildContextPrefix()
        {
            LogScopeState? current = _scope.Value;
            if (current == null)
            {
                return string.Empty;
            }

            Stack<LogScopeState> states = new();
            while (current != null)
            {
                states.Push(current);
                current = current.Parent;
            }

            Dictionary<string, string> merged = new(StringComparer.OrdinalIgnoreCase);
            while (states.Count > 0)
            {
                foreach ((string key, string value) in states.Pop().Values)
                {
                    merged[key] = value;
                }
            }

            if (merged.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(' ', merged
                .OrderBy(entry => GetContextPriority(entry.Key))
                .Select(entry => $"[{entry.Key}={entry.Value}]")
                .ToArray()) + ' ';
        }

        private static int GetContextPriority(string key)
        {
            return key.ToLowerInvariant() switch
            {
                "runid" => 0,
                "taskid" => 1,
                "phase" => 2,
                _ => 100
            };
        }
    }

    /// <summary>
    ///     Class to define an icon for the logger.
    /// </summary>
    /// <param name="icon">Unicode text defining the icon</param>
    /// <param name="iconColor">Foreground color of the icon</param>
    /// <param name="iconBackground">Background color of the icon</param>
    public class LoggerIcon(string icon, ConsoleColor iconColor, ConsoleColor iconBackground)
    {
        /// <summary>
        ///     Unicode text defining the icon
        /// </summary>
        public string Icon { get; set; } = icon;

        /// <summary>
        ///     Foreground color of the icon
        /// </summary>
        public ConsoleColor ForegroundColor { get; set; } = iconColor;

        /// <summary>
        ///     Background color of the icon
        /// </summary>
        public ConsoleColor BackgroundColor { get; set; } = iconBackground;
    }
}