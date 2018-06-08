namespace SpeedDate.Logging
{
    public interface ILogger
    {
        LogLevel LogLevel { get; set; }
        string Name { get; }

        /// <summary>
        /// Returns true, if message of this level will be logged
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        bool IsLogging(LogLevel level);

        void Trace(object message);
        void Debug(object message);
        void Info(object message);
        void Warn(object message);
        void Error(object message);
        void Fatal(object message);
        void Log(LogLevel logLvl, object message);
    }
}