using System;
using System.IO;

namespace DVVdownloader.Models
{
    public class AppSettings
    {
        public string DownloadDirectory { get; set; } = GetDefaultDownloadDirectory();
        public string DefaultQuality { get; set; } = "En İyi";
        public string DefaultAudioFormat { get; set; } = "mp3";
        public int MaxConcurrentDownloads { get; set; } = 3;
        public string CustomYtDlpPath { get; set; } = string.Empty;
        public string CustomFfmpegPath { get; set; } = string.Empty;
        public bool AutoOpenFolderAfterDownload { get; set; } = false;
        public bool PlayNotificationSound { get; set; } = true;

        public static string GetDefaultDownloadDirectory()
        {
            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "DVVdownloader"
            );
            return defaultPath;
        }
    }
}
