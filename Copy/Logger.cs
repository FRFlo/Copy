using System.Text;

namespace Copy
{
    /// <summary>
    /// Logger class.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Indicates if the program is in debug mode.
        /// </summary>
        public static bool IsDebug { get; set; } = false;
        /// <summary>
        /// Path to the log file.
        /// <list type="bullet">
        /// <item><description>In debug mode, it is located in the program folder with the name "debug.log".</description></item>
        /// <item><description>In production mode, it is located in the program folder with the name "production.log".</description></item>
        /// </list>
        /// </summary>
        public static string LogFilePath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(),
#if DEBUG
                                                                  "debug.log");
#else
                                                                  "production.log");
#endif
        /// <summary>
        /// Indicates if the logger should write logs to the file.
        /// </summary>
        public static bool LogToFile { get; set; } = true;
        /// <summary>
        /// Shows a message in the console and writes it to the log file.
        /// </summary>
        /// <param name="prefix">Prefix of the message</param>
        /// <param name="prefixColor">Color of the prefix</param>
        /// <param name="message">Message to show</param>
        /// <param name="color">Color of the message</param>
        /// <param name="icon">Icon to show before the message</param>
        private static void Print(string prefix, ConsoleColor prefixColor, string message, ConsoleColor color = ConsoleColor.White, LoggerIcon? icon = null)
        {
            StringBuilder sb = new();
            if (message.EndsWith('\n'))
            {
                message = message[..^1];
            }

            if (prefix != "DEBUG" || IsDebug)
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
                    Console.WriteLine($" {line}");
                    Console.ResetColor();

                    sb.Append($"{DateTime.Now:dd/MM/yyyy, HH:mm:fff} {prefix} {line}\n");
                }
            }
            string final = sb.ToString();

            if (LogToFile) File.AppendAllText(LogFilePath, final, Encoding.UTF8);
        }

        /// <summary>
        /// Shows a debug message in the console and writes it to the log file.
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="icon">Icon to show before the message</param>
        public static void Debug(string message, LoggerIcon? icon = null)
        {
            Print("DEBUG", ConsoleColor.DarkGreen, message, ConsoleColor.Green, icon);
        }

        /// <summary>
        /// Shows a info message in the console and writes it to the log file.
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="icon">Icon to show before the message</param>
        public static void Info(string message, LoggerIcon? icon = null)
        {
            Print("INFO", ConsoleColor.DarkBlue, message, ConsoleColor.Blue, icon);
        }

        /// <summary>
        /// Shows a warn message in the console and writes it to the log file.
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="icon">Icon to show before the message</param>
        public static void Warn(string message, LoggerIcon? icon = null)
        {
            Print("WARN", ConsoleColor.DarkYellow, message, ConsoleColor.Yellow, icon);
        }

        /// <summary>
        /// Shows a error message in the console and writes it to the log file.
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="icon">Icon to show before the message</param>
        public static void Error(string message, LoggerIcon? icon = null)
        {
            Print("ERREUR", ConsoleColor.DarkRed, message, ConsoleColor.Red, icon);
        }
    }

    /// <summary>
    /// Class to define an icon for the logger.
    /// </summary>
    /// <param name="icon">Unicode text defining the icon</param>
    /// <param name="iconColor">Foreground color of the icon</param>
    /// <param name="iconBackground">Background color of the icon</param>
    public class LoggerIcon(string icon, ConsoleColor iconColor, ConsoleColor iconBackground)
    {
        /// <summary>
        /// Unicode text defining the icon
        /// </summary>
        public string Icon { get; set; } = icon;
        /// <summary>
        /// Foreground color of the icon
        /// </summary>
        public ConsoleColor ForegroundColor { get; set; } = iconColor;
        /// <summary>
        /// Background color of the icon
        /// </summary>
        public ConsoleColor BackgroundColor { get; set; } = iconBackground;
    }
}
