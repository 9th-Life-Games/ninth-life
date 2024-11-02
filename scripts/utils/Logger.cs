using System;
using System.Globalization;
using System.IO;
using System.Text;
using Godot;
using Environment = System.Environment;
using FileAccess = System.IO.FileAccess;

namespace NinthLife.scripts.utils
{
    public partial class Logger : Node
    {
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error,
        }

        private const bool UseFileLogging = false;

        private static Logger _instance;
        private string _logFilePath;

        public static Logger Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = new Logger();

                // Retrieve the home directory path from the environment variable
                // CultureInfo.InvariantCulture
                Maybe<string> homePath = Environment.GetEnvironmentVariable("HOME").ToMaybe();

                // Get current date and time for unique file naming
                string dateTime = DateTime.Now.ToString(
                    "yyyy-MM-dd_HH-mm-ss",
                    CultureInfo.InvariantCulture
                );

                // Build the new log file path
                string directoryPath = Path.Combine(
                    homePath.UnwrapOr("/home/fireninja"),
                    ".gcache"
                );
                string logFileName = $"{dateTime}.log";
                string logFilePath = Path.Combine(directoryPath, logFileName);

                // Ensure the directory exists
                if (!Directory.Exists(directoryPath))
                {
                    _ = Directory.CreateDirectory(directoryPath);
                }

                // Set the log file path
                _instance._logFilePath = logFilePath;

                return _instance;
            }
        }

        // Method to log message with different log levels
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            // Print to Godot's output console
            GD.Print(formattedMessage);

            // Write to log file
            if (UseFileLogging)
            {
                WriteToFile(formattedMessage);
            }
        }

        // Method to write logs into a file
        private void WriteToFile(string message)
        {
            // Use FileMode.Append to automatically seek to the end of the file,
            // or create a new file if it doesn't exist.
            using FileStream file =
                new(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            // Encode the message as a byte array
            byte[] buffer = Encoding.UTF8.GetBytes(message + Environment.NewLine);
            // Write the encoded message to the file
            file.Write(buffer, 0, buffer.Length);
        }

        // Utility methods for easy access
        public static void Debug(string message)
        {
            Instance.Log(message, LogLevel.Debug);
        }

        public static void Info(string message)
        {
            Instance.Log(message);
        }

        public static void Warning(string message)
        {
            Instance.Log(message, LogLevel.Warning);
        }

        public static void Error(string message)
        {
            Instance.Log(message, LogLevel.Error);
        }
    }
}
