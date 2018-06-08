using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace SpeedDate.Logging
{
    public static class LogManager
    {
        private static LogLevel _globalLogLevel;
        private static LogHandler _appenders;

        private static Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();

        private static readonly Queue<PooledLog> _pooledLogs;

        public static bool EnableCurrentClassLogger = true;

        /// <summary>
        /// Overrides logging sett
        /// </summary>
        public static LogLevel GlobalLogLevel { get; set; }

        /// <summary>
        /// This overrides all logging settings
        /// </summary>
        public static LogLevel ForceLogLevel { get; set; }

        public static bool IsInitialized { get; private set; }

        public static int InitializationPoolSize = 100;

        static LogManager()
        {
            ForceLogLevel = LogLevel.Off;
            _pooledLogs = new Queue<PooledLog>();

            // Add default appender
            var appenders = new List<LogHandler>()
            {
                LogAppenders.ConsoleAppenderWithNames
            };

            // Initialize the log manager
            Initialize(appenders, LogLevel.All);

        }

        public static void Initialize(IEnumerable<LogHandler> appenders, LogLevel globalLogLevel)
        {
            GlobalLogLevel = globalLogLevel;

            foreach (var appender in appenders)
            {
                AddAppender(appender);
            }

            IsInitialized = true;

            // Disable pre-initialization pooling
            foreach (var logger in _loggers.Values)
            {
                logger.OnLog -= OnPooledLoggerLog;
            }

            // Push logger messages from pool to loggers
            while (_pooledLogs.Count > 0)
            {
                var log = _pooledLogs.Dequeue();
                log.Logger.Log(log.LogLevel, log.Message);
            }

            _pooledLogs.Clear();
        }

        public static void AddAppender(LogHandler appender)
        {
            _appenders += appender;
            foreach (var logger in _loggers.Values)
            {
                logger.OnLog += appender;
            }
        }
        

        public static Logger GetLogger(string name)
        {
            return GetLogger(name, true);
        }
        public static Logger GetLogger(string name, LogLevel defaultLogLevel)
        {
            var logger = GetLogger(name, true);
            logger.LogLevel = defaultLogLevel;
            return logger;
        }

        public static Logger GetCurrentClassLogger(LogLevel defaultLogLevel = LogLevel.Info, [CallerFilePath] string caller = "")
        {
            
            if (EnableCurrentClassLogger)
            {
                return GetLogger(Path.GetFileNameWithoutExtension(caller), defaultLogLevel);
            }

            return Logs.Logger;
        }


        public static Logger GetLogger(string name, bool poolUntilInitialized)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            if (!_loggers.TryGetValue(name, out var logger))
            {
                logger = CreateLogger(name);
                _loggers.Add(name, logger);
            }

            if (!IsInitialized && poolUntilInitialized)
            {
                // Register to pre-initialization pooling
                logger.OnLog += OnPooledLoggerLog;
            }

            return logger;
        }

        private static void OnPooledLoggerLog(Logger logger, LogLevel level, object message)
        {
            var log = _pooledLogs.Count >= InitializationPoolSize ? _pooledLogs.Dequeue() : new PooledLog();

            log.LogLevel = level;
            log.Logger = logger;
            log.Message = message;
            log.Date = DateTime.Now;

            _pooledLogs.Enqueue(log);
        }

        public static void Reset()
        {
            _loggers.Clear();
            _appenders = null;
        }

        private static Logger CreateLogger(string name)
        {
            var logger = new Logger(name)
            {
                LogLevel = GlobalLogLevel
            };
            logger.OnLog += _appenders;
            return logger;
        }

        private class PooledLog
        {
            public DateTime Date;
            public LogLevel LogLevel;
            public Logger Logger;
            public object Message;
        }
    }
}