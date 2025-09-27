using System;

namespace RobotApp
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        public static void Info(string message) => Write("INFO", message, ConsoleColor.Cyan);
        public static void Warning(string message) => Write("WARN", message, ConsoleColor.Yellow);
        public static void Error(string message) => Write("ERROR", message, ConsoleColor.Red);

        private static void Write(string level, string message, ConsoleColor color)
        {
            lock (_lock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
                Console.ResetColor();
            }
        }
    }
}
