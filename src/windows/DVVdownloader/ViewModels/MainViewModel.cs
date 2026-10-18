using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DVVdownloader.Common;
using DVVdownloader.Models;
using DVVdownloader.Services;

namespace DVVdownloader.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private int _selectedTab = 0;
        private string _urlInput = string.Empty;
        private string _detectedPlatform = "Bağlantı bekleniyor";
        private bool _isAnalyzing;
        private string _analysisStatusText = string.Empty;
        private string _analysisErrorMessage = string.Empty;
        private bool _hasAnalysisError;
        private MediaMetadata? _currentMetadata;
        private MediaFormatItem? _selectedFormat;
        private bool _isAudioMode;
        private string _selectedAudioFormat = "mp3";
        private AppSettings _settings;
        private string _ytDlpStatus = "Kontrol ediliyor...";
        private string _ffmpegStatus = "Kontrol ediliyor...";
        private bool _isLicenseModalOpen;

        public MainViewModel()
        {
            _settings = SettingsService.Instance.CurrentSettings;

            ActiveDownloads = new ObservableCollection<DownloadTask>();
            CompletedDownloads = new ObservableCollection<HistoryItem>();
            AvailableVideoFormats = new ObservableCollection<MediaFormatItem>();
            AvailableAudioFormats = new ObservableCollection<MediaFormatItem>();

            // Komutlar
            PasteFromClipboardCommand = new RelayCommand(PasteFromClipboard);
            ClearUrlCommand = new RelayCommand(() => UrlInput = string.Empty);
            AnalyzeUrlCommand = new AsyncRelayCommand(AnalyzeUrlAsync, () => !IsAnalyzing && !string.IsNullOrWhiteSpace(UrlInput));
            StartDownloadCommand = new AsyncRelayCommand(StartDownloadAsync, () => CurrentMetadata != null && !IsAnalyzing);
            CancelDownloadCommand = new RelayCommand(CancelDownload);
            RetryDownloadCommand = new AsyncRelayCommand(RetryDownloadAsync);
            OpenFileCommand = new RelayCommand(OpenFile);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            BrowseDownloadFolderCommand = new RelayCommand(BrowseDownloadFolder);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ClearHistoryCommand = new RelayCommand(ClearHistory);
            ClearLogsCommand = new RelayCommand(() => LogService.Instance.Logs.Clear());
            RemoveDownloadCommand = new RelayCommand(p =>
            {
                if (p is DownloadTask task)
                {
                    task.Cts?.Cancel();
                    ActiveDownloads.Remove(task);
                    OnPropertyChanged(nameof(ActiveDownloadsCount));
                }
            });
            SelectTabCommand = new RelayCommand(p =>
            {
                if (p != null && int.TryParse(p.ToString(), out int tab))
                {
                    SelectedTab = tab;
                }
            });
            OpenLicenseModalCommand = new RelayCommand(() => IsLicenseModalOpen = true);
            CloseLicenseModalCommand = new RelayCommand(() => IsLicenseModalOpen = false);

            // İlk verileri yükle
            RefreshHistory();
            CheckBinaries();

            LogService.Instance.Info("DVVdownloader Windows yerel arayüzü başlatıldı.", "MainViewModel");
        }

        public int SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        public string UrlInput
        {
            get => _urlInput;
            set
            {
                if (SetProperty(ref _urlInput, value))
                {
                    DetectedPlatform = PlatformDetector.DetectPlatform(_urlInput);
                    HasAnalysisError = false;
                    AnalysisErrorMessage = string.Empty;
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string DetectedPlatform
        {
            get => _detectedPlatform;
            set => SetProperty(ref _detectedPlatform, value);
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set => SetProperty(ref _isAnalyzing, value);
        }

        public string AnalysisStatusText
        {
            get => _analysisStatusText;
            set => SetProperty(ref _analysisStatusText, value);
        }

        public string AnalysisErrorMessage
        {
            get => _analysisErrorMessage;
            set => SetProperty(ref _analysisErrorMessage, value);
        }

        public bool HasAnalysisError
        {
            get => _hasAnalysisError;
            set => SetProperty(ref _hasAnalysisError, value);
        }

        public MediaMetadata? CurrentMetadata
        {
            get => _currentMetadata;
            set
            {
                if (SetProperty(ref _currentMetadata, value))
                {
                    OnPropertyChanged(nameof(HasPreview));
                }
            }
        }

        public bool HasPreview => CurrentMetadata != null;

        public MediaFormatItem? SelectedFormat
        {
            get => _selectedFormat;
            set => SetProperty(ref _selectedFormat, value);
        }

        public bool IsAudioMode
        {
            get => _isAudioMode;
            set => SetProperty(ref _isAudioMode, value);
        }

        public string SelectedAudioFormat
        {
            get => _selectedAudioFormat;
            set => SetProperty(ref _selectedAudioFormat, value);
        }

        public AppSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public string YtDlpStatus
        {
            get => _ytDlpStatus;
            set => SetProperty(ref _ytDlpStatus, value);
        }

        public string FfmpegStatus
        {
            get => _ffmpegStatus;
            set => SetProperty(ref _ffmpegStatus, value);
        }

        public ObservableCollection<DownloadTask> ActiveDownloads { get; }
        public ObservableCollection<HistoryItem> CompletedDownloads { get; }
        public ObservableCollection<MediaFormatItem> AvailableVideoFormats { get; }
        public ObservableCollection<MediaFormatItem> AvailableAudioFormats { get; }
        public ObservableCollection<LogEntry> Logs => LogService.Instance.Logs;

        public int ActiveDownloadsCount => ActiveDownloads.Count(t => t.Status == DownloadStatus.Indiriliyor || t.Status == DownloadStatus.Kuyrukta || t.Status == DownloadStatus.Birlestiriliyor || t.Status == DownloadStatus.BilgilerAliniyor);

        // Komut Referansları
        public ICommand PasteFromClipboardCommand { get; }
        public ICommand ClearUrlCommand { get; }
        public ICommand AnalyzeUrlCommand { get; }
        public ICommand StartDownloadCommand { get; }
        public ICommand CancelDownloadCommand { get; }
        public ICommand RetryDownloadCommand { get; }
        public ICommand RemoveDownloadCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand BrowseDownloadFolderCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand SelectTabCommand { get; }
        public ICommand OpenLicenseModalCommand { get; }
        public ICommand CloseLicenseModalCommand { get; }

        public bool IsLicenseModalOpen
        {
            get => _isLicenseModalOpen;
            set => SetProperty(ref _isLicenseModalOpen, value);
        }

        public string FullGplLicenseText =>
            "                    GNU GENERAL PUBLIC LICENSE\n" +
            "                       Version 3, 29 June 2007\n\n" +
            " Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>\n" +
            " Everyone is permitted to copy and distribute verbatim copies\n" +
            " of this license document, but changing it is not allowed.\n\n" +
            "                            Preamble\n\n" +
            " The GNU General Public License is a free, copyleft license for\n" +
            " software and other kinds of works.\n\n" +
            " The licenses for most software and other practical works are designed\n" +
            " to take away your freedom to share and change the works.  By contrast,\n" +
            " the GNU General Public License is intended to guarantee your freedom to\n" +
            " share and change all versions of a program--to make sure it remains free\n" +
            " software for all its users.  We, the Free Software Foundation, use the\n" +
            " GNU General Public License for most of our software; it applies also to\n" +
            " any other work released this way by its authors.  You can apply it to\n" +
            " your programs, too.\n\n" +
            " When we speak of free software, we are referring to freedom, not\n" +
            " price.  Our General Public Licenses are designed to make sure that you\n" +
            " have the freedom to distribute copies of free software (and charge for\n" +
            " them if you wish), that you receive source code or can get it if you\n" +
            " want it, that you can change the software or use pieces of it in new\n" +
            " free programs, and that you know you can do these things.\n\n" +
            "                     TERMS AND CONDITIONS\n\n" +
            " 0. Definitions.\n" +
            " \"This License\" refers to version 3 of the GNU General Public License.\n" +
            " \"The Program\" refers to any copyrightable work licensed under this License.\n" +
            " Each licensee is addressed as \"you\". \"Licensees\" and \"recipients\" may be\n" +
            " individuals or organizations.\n\n" +
            " 1. Source Code.\n" +
            " The \"source code\" for a work means the preferred form of the work for\n" +
            " making modifications to it. \"Object code\" means any non-source form of a work.\n\n" +
            " 2. Basic Permissions.\n" +
            " All rights granted under this License are granted for the term of copyright\n" +
            " on the Program, and are irrevocable provided the stated conditions are met.\n" +
            " This License explicitly affirms your unlimited permission to run the\n" +
            " unmodified Program.\n\n" +
            " 4. Conveying Verbatim Copies.\n" +
            " You may convey verbatim copies of the Program's source code as you receive it,\n" +
            " in any medium, provided that you conspicuously and appropriately publish on\n" +
            " each copy an appropriate copyright notice; keep intact all notices stating that\n" +
            " this License and any non-permissive terms added in accord with section 7 apply\n" +
            " to the code; keep intact all notices of the absence of any warranty; and give all\n" +
            " recipients a copy of this License along with the Program.\n\n" +
            " 5. Conveying Modified Source Versions.\n" +
            " You may convey a work based on the Program, or the modifications to produce it\n" +
            " from the Program, in the form of source code under the terms of section 4,\n" +
            " provided that you also meet all of these conditions:\n" +
            "   a) The work must carry prominent notices stating that you modified it.\n" +
            "   b) The work must carry prominent notices stating that it is released under this License.\n" +
            "   c) You must license the entire work, as a whole, under this License.\n\n" +
            " 15. Disclaimer of Warranty.\n" +
            " THERE IS NO WARRANTY FOR THE PROGRAM, TO THE EXTENT PERMITTED BY APPLICABLE LAW.\n" +
            " EXCEPT WHEN OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR OTHER PARTIES\n" +
            " PROVIDE THE PROGRAM \"AS IS\" WITHOUT WARRANTY OF ANY KIND.\n\n" +
            " 16. Limitation of Liability.\n" +
            " IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING WILL ANY\n" +
            " COPYRIGHT HOLDER BE LIABLE TO YOU FOR DAMAGES, INCLUDING ANY GENERAL, SPECIAL,\n" +
            " INCIDENTAL OR CONSEQUENTIAL DAMAGES ARISING OUT OF THE USE OR INABILITY TO USE THE PROGRAM.\n\n" +
            " ------------------------------------------------------------\n" +
            " DVVdownloader — Telif Hakkı (C) 2026 DVVdownloader Geliştiricileri\n" +
            " Bu yazılım GNU GPL v3.0 lisansına tabidir. Özgürce kullanılabilir ve dağıtılabilir.";

        private void PasteFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText().Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        UrlInput = text;
                        if (AnalyzeUrlCommand.CanExecute(null))
                        {
                            AnalyzeUrlCommand.Execute(null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Error($"Pano okunamadı: {ex.Message}", "UI");
            }
        }

        public async Task AnalyzeUrlAsync()
        {
            string url = UrlInput.Trim();
            if (string.IsNullOrEmpty(url)) return;

            IsAnalyzing = true;
            HasAnalysisError = false;
            AnalysisErrorMessage = string.Empty;
            AnalysisStatusText = "Medya kaynakları ve formatları taranıyor...";
            CurrentMetadata = null;
            AvailableVideoFormats.Clear();
            AvailableAudioFormats.Clear();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                var metadata = await YtDlpEngine.Instance.ExtractMetadataAsync(url, cts.Token);

                CurrentMetadata = metadata;

                // Formatları ayır ve ekle
                foreach (var f in metadata.Formats)
                {
                    if (f.IsAudioOnly)
                    {
                        AvailableAudioFormats.Add(f);
                    }
                    else
                    {
                        AvailableVideoFormats.Add(f);
                    }
                }

                // Varsayılan format seçimi
                SelectedFormat = AvailableVideoFormats.FirstOrDefault() ?? AvailableAudioFormats.FirstOrDefault();

                LogService.Instance.Info($"Analiz başarılı: {metadata.Title} ({metadata.Formats.Count} format bulundu)", "UI");
            }
            catch (Exception ex)
            {
                HasAnalysisError = true;
                AnalysisErrorMessage = $"Medya analizi başarısız oldu: {ex.Message}";
                LogService.Instance.Error(AnalysisErrorMessage, "UI");
            }
            finally
            {
                IsAnalyzing = false;
                AnalysisStatusText = string.Empty;
            }
        }

        public async Task StartDownloadAsync()
        {
            if (CurrentMetadata == null) return;

            var task = new DownloadTask
            {
                Url = CurrentMetadata.WebpageUrl,
                Title = CurrentMetadata.Title,
                Platform = CurrentMetadata.Platform,
                ThumbnailUrl = CurrentMetadata.ThumbnailUrl,
                IsAudioOnly = IsAudioMode,
                SelectedFormatId = SelectedFormat?.FormatId ?? "best",
                SelectedHeight = IsAudioMode ? 0 : (SelectedFormat?.Height ?? 1080),
                DirectUrl = SelectedFormat?.DirectUrl ?? string.Empty,
                SelectedQuality = IsAudioMode ? $"MP3 Ses ({SelectedAudioFormat.ToUpper()})" : (SelectedFormat?.QualityLabel ?? "En İyi"),
                FileSizeFormatted = IsAudioMode ? "" : (SelectedFormat?.FormattedSize ?? ""),
                Status = DownloadStatus.Kuyrukta,
                Cts = new CancellationTokenSource()
            };

            ActiveDownloads.Insert(0, task);
            OnPropertyChanged(nameof(ActiveDownloadsCount));

            // İndirme ekranına yönlendir
            SelectedTab = 1;

            // Arka planda indirmeyi başlat
            _ = Task.Run(async () =>
            {
                try
                {
                    task.Status = DownloadStatus.Indiriliyor;
                    await YtDlpEngine.Instance.DownloadAsync(task, (pct, speed, eta) =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            task.ProgressPercentage = pct;
                            task.DownloadSpeed = speed;
                            task.Eta = eta;
                        });
                    }, task.Cts.Token);

                    // Başarılı tamamlandığında
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnPropertyChanged(nameof(ActiveDownloadsCount));
                        RefreshHistory();

                        if (Settings.AutoOpenFolderAfterDownload && !string.IsNullOrEmpty(task.TargetFilePath))
                        {
                            OpenFolder(Path.GetDirectoryName(task.TargetFilePath));
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    task.Status = DownloadStatus.IptalEdildi;
                }
                catch (Exception ex)
                {
                    task.Status = DownloadStatus.Hata;
                    task.ErrorMessage = ex.Message;
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnPropertyChanged(nameof(ActiveDownloadsCount));
                    });
                }
            });
        }

        private void CancelDownload(object? param)
        {
            if (param is DownloadTask task)
            {
                task.Cts?.Cancel();
                task.Status = DownloadStatus.IptalEdildi;
                OnPropertyChanged(nameof(ActiveDownloadsCount));
            }
        }

        private async Task RetryDownloadAsync(object? param)
        {
            if (param is DownloadTask task)
            {
                task.Status = DownloadStatus.Kuyrukta;
                task.ProgressPercentage = 0;
                task.ErrorMessage = string.Empty;
                task.Cts?.Dispose();
                task.Cts = new CancellationTokenSource();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        task.Status = DownloadStatus.Indiriliyor;
                        await YtDlpEngine.Instance.DownloadAsync(task, (pct, speed, eta) =>
                        {
                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                task.ProgressPercentage = pct;
                                task.DownloadSpeed = speed;
                                task.Eta = eta;
                            });
                        }, task.Cts.Token);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            OnPropertyChanged(nameof(ActiveDownloadsCount));
                            RefreshHistory();
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        task.Status = DownloadStatus.IptalEdildi;
                    }
                    catch (Exception ex)
                    {
                        task.Status = DownloadStatus.Hata;
                        task.ErrorMessage = ex.Message;
                    }
                });
            }
        }

        public void RefreshHistory()
        {
            CompletedDownloads.Clear();
            var list = HistoryService.Instance.LoadHistory();
            foreach (var item in list)
            {
                CompletedDownloads.Add(item);
            }
        }

        private void OpenFile(object? param)
        {
            try
            {
                string? path = param as string ?? (param as HistoryItem)?.FilePath;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Hedef dosya bulunamadı veya taşınmış olabilir.", "Dosya Açılamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Error($"Dosya açılamadı: {ex.Message}", "UI");
            }
        }

        private void OpenFolder(object? param)
        {
            try
            {
                string? path = param as string ?? (param as HistoryItem)?.FilePath;
                string? folder = !string.IsNullOrEmpty(path) && File.Exists(path) ? Path.GetDirectoryName(path) : path;
                folder ??= Settings.DownloadDirectory;

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogService.Instance.Error($"Klasör açılamadı: {ex.Message}", "UI");
            }
        }

        private void BrowseDownloadFolder()
        {
            try
            {
                // .NET 8+ / .NET 10 yerel OpenFolderDialog
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "İndirme Klasörünü Seçin",
                    InitialDirectory = Settings.DownloadDirectory
                };

                if (dialog.ShowDialog() == true)
                {
                    Settings.DownloadDirectory = dialog.FolderName;
                    SaveSettings();
                    OnPropertyChanged(nameof(Settings));
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Error($"Klasör seçici açılamadı: {ex.Message}", "UI");
            }
        }

        private void SaveSettings()
        {
            SettingsService.Instance.SaveSettings(Settings);
            CheckBinaries();
        }

        private void ClearHistory()
        {
            if (MessageBox.Show("İndirme geçmişini temizlemek istediğinizden emin misiniz? (İndirilen dosyalarınız silinmez)", "Geçmişi Temizle", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                HistoryService.Instance.ClearHistory();
                RefreshHistory();
            }
        }

        private void CheckBinaries()
        {
            YtDlpEngine.Instance.AreBinariesAvailable(out string ytStatus, out string ffStatus);
            YtDlpStatus = ytStatus;
            FfmpegStatus = ffStatus;
        }
    }
}
