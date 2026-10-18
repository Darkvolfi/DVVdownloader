using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace DVVdownloader.Services
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Engine
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;

        public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");
        public string LevelBadge => Level switch
        {
            LogLevel.Info => "[BİLGİ]",
            LogLevel.Warning => "[UYARI]",
            LogLevel.Error => "[HATA]",
            LogLevel.Engine => "[MOTOR]",
            _ => "[LOG]"
        };
    }

    public class LogService
    {
        private static LogService? _instance;
        public static LogService Instance => _instance ??= new LogService();

        public ObservableCollection<LogEntry> Logs { get; } = new();
        private readonly string _logFilePath;
        private readonly object _lock = new();

        private LogService()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DVVdownloader",
                "logs"
            );
            Directory.CreateDirectory(appData);
            _logFilePath = Path.Combine(appData, $"dvv_{DateTime.Now:yyyyMMdd}.log");
        }

        public void Log(LogLevel level, string message, string source = "Sistem")
        {
            var entry = new LogEntry
            {
                Level = level,
                Message = message,
                Source = source
            };

            // UI thread safe dispatch
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.InvokeAsync(() => AddEntry(entry));
            }
            else
            {
                AddEntry(entry);
            }

            // Write to local file safely
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, $"[{entry.FormattedTime}] {entry.LevelBadge} [{entry.Source}] {entry.Message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Never crash because of logging failure
            }
        }

        private void AddEntry(LogEntry entry)
        {
            if (Logs.Count > 1000)
            {
                Logs.RemoveAt(0);
            }
            Logs.Add(entry);
        }

        public void Info(string message, string source = "Uygulama") => Log(LogLevel.Info, message, source);
        public void Warning(string message, string source = "Uygulama") => Log(LogLevel.Warning, message, source);
        public void Error(string message, string source = "Uygulama") => Log(LogLevel.Error, message, source);
        public void Engine(string message, string source = "yt-dlp") => Log(LogLevel.Engine, message, source);
    }
}
