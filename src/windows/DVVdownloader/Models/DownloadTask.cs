using System;
using System.Text.Json.Serialization;
using DVVdownloader.Common;

using DVVdownloader.Localization;

namespace DVVdownloader.Models
{
    public enum DownloadStatus
    {
        Kuyrukta,
        BilgilerAliniyor,
        Indiriliyor,
        Birlestiriliyor,
        Tamamlandi,
        Hata,
        IptalEdildi,
        Duraklatildi
    }

    public class DownloadTask : ObservableObject
    {
        private double _progressPercentage;
        private string _downloadSpeed = "--";
        private string _eta = "--";
        private DownloadStatus _status = DownloadStatus.Kuyrukta;
        private string _statusText = Loc.Get("StatusQueued");
        private string _errorMessage = string.Empty;

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = "Media";
        public string Platform { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string SelectedQuality { get; set; } = string.Empty;
        public string SelectedFormatId { get; set; } = "best";
        public int SelectedHeight { get; set; } = 1080;
        public bool IsAudioOnly { get; set; }
        public string TargetFilePath { get; set; } = string.Empty;
        public string DirectUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;

        public bool CanCancel => Status == DownloadStatus.Kuyrukta || Status == DownloadStatus.Indiriliyor || Status == DownloadStatus.Birlestiriliyor || Status == DownloadStatus.BilgilerAliniyor;
        public bool CanRetry => Status == DownloadStatus.Hata || Status == DownloadStatus.IptalEdildi;
        public bool CanRemove => Status == DownloadStatus.Tamamlandi || Status == DownloadStatus.Hata || Status == DownloadStatus.IptalEdildi;

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public string DownloadSpeed
        {
            get => _downloadSpeed;
            set => SetProperty(ref _downloadSpeed, value);
        }

        public string Eta
        {
            get => _eta;
            set => SetProperty(ref _eta, value);
        }

        public DownloadStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    StatusText = _status switch
                    {
                        DownloadStatus.Kuyrukta => Loc.Get("StatusQueued"),
                        DownloadStatus.BilgilerAliniyor => Loc.Get("StatusAnalyzing"),
                        DownloadStatus.Indiriliyor => Loc.Get("StatusDownloading"),
                        DownloadStatus.Birlestiriliyor => Loc.Get("StatusProcessing"),
                        DownloadStatus.Tamamlandi => Loc.Get("StatusCompleted"),
                        DownloadStatus.Hata => Loc.Get("StatusError"),
                        DownloadStatus.IptalEdildi => Loc.Get("StatusCancelled"),
                        DownloadStatus.Duraklatildi => Loc.Get("StatusPaused"),
                        _ => _status.ToString()
                    };
                    OnPropertyChanged(nameof(CanCancel));
                    OnPropertyChanged(nameof(CanRetry));
                    OnPropertyChanged(nameof(CanRemove));
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        [JsonIgnore]
        public System.Threading.CancellationTokenSource? Cts { get; set; }
    }
}
