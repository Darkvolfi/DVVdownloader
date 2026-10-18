using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DVVdownloader.Models;
using DVVdownloader.Services;
using Xunit;

namespace DVVdownloader.Tests
{
    public class YtDlpEngineTests
    {
        [Fact]
        public async Task ExtractMetadata_WithUserVideo_ParsesWithoutException()
        {
            string url = "https://www.youtube.com/watch?v=Mwc_iNAd5SU";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var metadata = await YtDlpEngine.Instance.ExtractMetadataAsync(url, cts.Token);

            Assert.NotNull(metadata);
            Assert.False(string.IsNullOrWhiteSpace(metadata.Title));
            Assert.NotEmpty(metadata.Formats);
            Assert.True(metadata.Duration > 0);
        }

        [Fact]
        public async Task ExtractMetadata_UserVideo_SelectsDirectStreamsWithAccurateSize()
        {
            string url = "https://www.youtube.com/watch?v=fyvJ2wCTpas";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var metadata = await YtDlpEngine.Instance.ExtractMetadataAsync(url, cts.Token);

            Assert.NotNull(metadata);
            Assert.NotEmpty(metadata.Formats);

            var f1080 = metadata.Formats.Find(f => f.Height == 1080);
            if (f1080 != null)
            {
                // Format ID direct HTTPS stream olmalı (m3u8 617 OLMAMALI, 299 veya 303 veya 399 olmalı)
                Assert.NotEqual("617", f1080.FormatId);
                // MB boyutu şişirilmiş 50 MB olmamalı, gerçekçi (~5 MB - 15 MB) olmalı
                Assert.True(f1080.Filesize.HasValue);
                double mb = f1080.Filesize.Value / (1024.0 * 1024.0);
                Assert.InRange(mb, 2.0, 20.0);
            }
        }

        [Fact]
        public async Task Download_ShortVideo_DownloadsFileSuccessfully()
        {
            string url = "https://www.youtube.com/watch?v=jNQXAC9IVRw"; // 19s test video
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            var metadata = await YtDlpEngine.Instance.ExtractMetadataAsync(url, cts.Token);
            var selectedFormat = metadata.Formats.Find(f => f.Height <= 360 && f.Height > 0) ?? metadata.Formats[0];
            var task = new DownloadTask
            {
                Url = url,
                Title = metadata.Title,
                Platform = metadata.Platform,
                SelectedFormatId = selectedFormat.FormatId,
                SelectedQuality = selectedFormat.QualityLabel,
                SelectedHeight = selectedFormat.Height,
                Status = DownloadStatus.Kuyrukta
            };

            double lastProgress = 0;
            await YtDlpEngine.Instance.DownloadAsync(task, (pct, speed, eta) =>
            {
                lastProgress = pct;
            }, cts.Token);

            Assert.Equal(DownloadStatus.Tamamlandi, task.Status);
            Assert.True(lastProgress >= 90);
            Assert.False(string.IsNullOrEmpty(task.TargetFilePath));
            Assert.True(File.Exists(task.TargetFilePath));
            Assert.Contains($"[{selectedFormat.Height}p]", task.TargetFilePath);

            // Clean up downloaded test file
            try
            {
                if (File.Exists(task.TargetFilePath))
                {
                    File.Delete(task.TargetFilePath);
                }
            }
            catch { }
        }

        [Fact]
        public async Task ExtractMetadata_Pinterest_FormatsAreClassifiedCorrectly()
        {
            string url = "https://www.pinterest.com/pin/326299935525767852/";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var metadata = await YtDlpEngine.Instance.ExtractMetadataAsync(url, cts.Token);

            Assert.NotNull(metadata);
            Assert.NotEmpty(metadata.Formats);

            // Audio formatları video olarak (Height > 0) sınıflandırılmamalı
            var audioFormats = metadata.Formats.FindAll(f => f.IsAudioOnly);
            foreach (var af in audioFormats)
            {
                Assert.Equal(0, af.Height);
            }

            // Video formatları arasında audio-1 akışı olmamalı
            var videoFormats = metadata.Formats.FindAll(f => !f.IsAudioOnly);
            Assert.NotEmpty(videoFormats);
            foreach (var vf in videoFormats)
            {
                Assert.DoesNotContain("audio", vf.FormatId, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("_audio", vf.DirectUrl, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void BinaryResolution_FindsYtDlpAndFfmpegBinaries()
        {
            string ytDlp = YtDlpEngine.Instance.ResolveYtDlpPath();
            Assert.False(string.IsNullOrWhiteSpace(ytDlp));
            Assert.True(File.Exists(ytDlp), $"yt-dlp.exe dosyası bulunamadı: {ytDlp}");

            bool available = YtDlpEngine.Instance.AreBinariesAvailable(out string ytStatus, out string ffStatus);
            Assert.True(available);
            Assert.Equal("Kullanılabilir", ytStatus);

            string ffmpegDir = YtDlpEngine.Instance.ResolveFfmpegDirectory();
            Assert.False(string.IsNullOrWhiteSpace(ffmpegDir));
            Assert.True(File.Exists(Path.Combine(ffmpegDir, "ffmpeg.exe")), $"ffmpeg.exe dosyası bulunamadı: {ffmpegDir}");
        }
    }
}
