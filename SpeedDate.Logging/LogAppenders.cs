using System;

namespace SpeedDate.Logging
{
    public class LogAppenders
    {
        public delegate string LogFormatter(Logger logger, LogLevel level, object message);

        public static void ConsoleAppender(Logger logger, LogLevel logLevel, object message)
        {
            if (logLevel <= LogLevel.Info)
            {
                Console.WriteLine($"[{logLevel}] {message}");
            } else if (logLevel <= LogLevel.Warn)
            {
                Console.WriteLine($"[{logLevel}] {message}");
            }
            else if (logLevel <= LogLevel.Fatal)
            {
                Console.WriteLine($"[{logLevel}] {message}");
            }
        }

        public static void ConsoleAppenderWithNames(Logger logger, LogLevel logLevel, object message)
        {
            if (logLevel <= LogLevel.Info)
            {
                Console.WriteLine($"[{logLevel} | {logger.Name}] {message}");
            }
            else if (logLevel <= LogLevel.Warn)
            {
                Console.WriteLine($"[{logLevel} | {logger.Name}] {message}");
            }
            else if (logLevel <= LogLevel.Fatal)
            {
                Console.WriteLine($"[{logLevel} | {logger.Name}] {message}");
            }
        }
    }
}