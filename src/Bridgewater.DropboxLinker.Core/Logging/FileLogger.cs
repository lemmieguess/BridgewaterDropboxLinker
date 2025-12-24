using System;
using System.IO;
using Bridgewater.DropboxLinker.Core.Contracts;

namespace Bridgewater.DropboxLinker.Core.Logging
{
    /// <summary>
    /// A simple file-based logger that writes to daily rolling log files.
    /// </summary>
    /// <remarks>
    /// Log files are named: dropboxlinker-YYYYMMDD.log
    /// </remarks>
    public sealed class FileLogger : IAppLogger, IDisposable
    {
        private const string LogFilePrefix = "dropboxlinker-";
        private const string LogFileExtension = ".log";
        private const string DateFormat = "yyyyMMdd";
        private const string TimestampFormat = "O"; // ISO 8601

        private readonly string _logDir;
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class.
        /// </summary>
        /// <param name="logDir">The directory where log files will be written.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="logDir"/> is null or whitespace.</exception>
        public FileLogger(string logDir)
        {
            if (string.IsNullOrWhiteSpace(logDir))
            {
                throw new ArgumentException("Log directory is required.", nameof(logDir));
            }

            _logDir = logDir;
            Directory.CreateDirectory(_logDir);
        }

        /// <inheritdoc />
        public void Info(string message)
        {
            Write("INFO", message);
        }

        /// <inheritdoc />
        public void Warn(string message)
        {
            Write("WARN", message);
        }

        /// <inheritdoc />
        public void Error(Exception ex, string message)
        {
            if (ex == null)
            {
                Write("ERROR", message);
                return;
            }

            var exceptionInfo = $"{ex.GetType().Name}: {ex.Message}";
            Write("ERROR", $"{message} :: {exceptionInfo}");
        }

        /// <summary>
        /// Releases resources used by the logger.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
        }

        private void Write(string level, string message)
        {
            if (_disposed)
            {
                return;
            }

            var timestamp = DateTimeOffset.Now.ToString(TimestampFormat);
            var line = $"{timestamp} [{level}] {message}";
            var fileName = $"{LogFilePrefix}{DateTime.Now.ToString(DateFormat)}{LogFileExtension}";
            var filePath = Path.Combine(_logDir, fileName);

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(filePath, line + Environment.NewLine);
                }
                catch
                {
                    // Swallow logging errors to prevent cascading failures
                    // In production, you might want to write to Event Log as fallback
                }
            }
        }
    }
}
