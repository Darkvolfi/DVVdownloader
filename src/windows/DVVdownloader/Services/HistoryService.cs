using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DVVdownloader.Models;

namespace DVVdownloader.Services
{
    public class HistoryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileSizeFormatted { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; } = DateTime.Now;

        public bool FileExists => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
    }

    public class HistoryService
    {
        private static readonly Lazy<HistoryService> _instance = new(() => new HistoryService());
        public static HistoryService Instance => _instance.Value;

        private readonly string _historyPath;
        private readonly object _lock = new();

        private HistoryService()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DVVdownloader"
            );
            Directory.CreateDirectory(appData);
            _historyPath = Path.Combine(appData, "history.json");
        }

        public List<HistoryItem> LoadHistory()
        {
            lock (_lock)
            {
                return LoadHistoryInternal();
            }
        }

        /// <summary>Lock dışında dosya okuma — caller zaten lock tutmalıdır.</summary>
        private List<HistoryItem> LoadHistoryInternal()
        {
            try
            {
                if (File.Exists(_historyPath))
                {
                    string json = File.ReadAllText(_historyPath);
                    var list = JsonSerializer.Deserialize<List<HistoryItem>>(json);
                    if (list != null)
                    {
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Error($"Failed to read history: {ex.Message}", "HistoryService");
            }
            return new List<HistoryItem>();
        }

        public void AddHistoryItem(HistoryItem item)
        {
            lock (_lock)
            {
                try
                {
                    var list = LoadHistoryInternal();
                    list.Insert(0, item);
                    if (list.Count > 500)
                    {
                        list.RemoveAt(list.Count - 1);
                    }
                    string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_historyPath, json);
                }
                catch (Exception ex)
                {
                    LogService.Instance.Error($"Failed to add history entry: {ex.Message}", "HistoryService");
                }
            }
        }

        public void ClearHistory()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_historyPath))
                    {
                        File.WriteAllText(_historyPath, "[]");
                    }
                }
                catch (Exception ex)
                {
                    LogService.Instance.Error($"Failed to clear history: {ex.Message}", "HistoryService");
                }
            }
        }
    }
}
