using System;

namespace HandlingEditor.Client
{
    public class CfxLogger : ILogger
    {
        #region Protected Members

        /// <summary>
        /// The configuration to use
        /// </summary>
        protected CfxLoggerConfiguration mConfiguration;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configuration">The configuration to use</param>
        public CfxLogger(CfxLoggerConfiguration configuration)
        {
            mConfiguration = configuration;
        }

        #endregion

        public void Log(LogLevel logLevel, string message)
        {
            // If we should not log...
            if (!IsEnabled(logLevel))
                // Return
                return;

            // Get current time
            var currentTime = DateTimeOffset.Now.ToString("yyyy-MM-dd hh:mm:ss");

            // Prepend log level
            var logLevelString = mConfiguration.OutputLogLevel ? $"{logLevel.ToString().ToUpper()}: " : "";

            // Prepend the time to the log if desired
            var timeLogString = mConfiguration.LogTime ? $"[{currentTime}] " : "";

            var colorPrefix = GetColorPrefix(logLevel);
            var white = "^7";

            // Write the message
            var output = $"{Globals.ScriptName}: {colorPrefix}{logLevelString}{white}{timeLogString}{message}{Environment.NewLine}";

            CitizenFX.Core.Debug.Write(output);
        }

        /// <summary>
        /// Enabled if the log level is the same or greater than the configuration
        /// </summary>
        /// <param name="logLevel">The log level to check against</param>
        /// <returns></returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            // Enabled if the log level is greater or equal to what we want to log
            return logLevel >= mConfiguration.LogLevel;
        }

        public string GetColorPrefix(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace: return "^7";       // White
                case LogLevel.Debug: return "^5";       // Light blue
                case LogLevel.Information: return "^2"; // Light Green
                case LogLevel.Warning: return "^3";     // Light Yellow
                case LogLevel.Error: return "^1";       // Red Orange
                case LogLevel.Critical: return "^8";    // Blood Red
                case LogLevel.None:
                default: return string.Empty;
            }
        }
    }
}
