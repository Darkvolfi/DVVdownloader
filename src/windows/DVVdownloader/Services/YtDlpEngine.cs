using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DVVdownloader.Models;

using DVVdownloader.Localization;

namespace DVVdownloader.Services
{
    public class YtDlpEngine
    {
        private static readonly Lazy<YtDlpEngine> _instance = new(() => new YtDlpEngine());
        public static YtDlpEngine Instance => _instance.Value;

        private static readonly Regex ProgressRegex = new(
            @"\[download\]\s+(?<percent>\d+(?:\.\d+)?)%\s+of\s+~?(?<size>[^\s]+)\s+at\s+(?<speed>[^\s]+)\s+ETA\s+(?<eta>[^\s]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Regex DestRegex = new(
            @"(?:\[Merger\] Merging formats into ""(?<file>[^""]+)""|\[download\] Destination:\s*(?<file>[^\r\n]+)|\[ExtractAudio\] Destination:\s*(?<file>[^\r\n]+)|\[download\]\s+(?<file>[^\r\n]+?)\s+has already been downloaded)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static string? FindExecutableInPath(string fileName)
        {
            try
            {
                string? pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (string.IsNullOrWhiteSpace(pathEnv)) return null;

                foreach (string folder in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        string trimmed = folder.Trim().Trim('"');
                        if (string.IsNullOrEmpty(trimmed)) continue;
                        string full = Path.Combine(trimmed, fileName);
                        if (File.Exists(full)) return full;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        public string ResolveYtDlpPath()
        {
            var settings = SettingsService.Instance.CurrentSettings;
            if (!string.IsNullOrWhiteSpace(settings.CustomYtDlpPath) && File.Exists(settings.CustomYtDlpPath))
            {
                return settings.CustomYtDlpPath;
            }

            var candidates = new List<string>();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                candidates.Add(Path.Combine(baseDir, "tools", "yt-dlp.exe"));
                candidates.Add(Path.Combine(baseDir, "yt-dlp.exe"));

                try
                {
                    var dir = new DirectoryInfo(baseDir);
                    for (int i = 0; i < 5 && dir != null; i++)
                    {
                        candidates.Add(Path.Combine(dir.FullName, "tools", "yt-dlp.exe"));
                        candidates.Add(Path.Combine(dir.FullName, "dist", "windows", "tools", "yt-dlp.exe"));
                        candidates.Add(Path.Combine(dir.FullName, "src", "windows", "DVVdownloader", "tools", "yt-dlp.exe"));
                        dir = dir.Parent;
                    }
                }
                catch { }
            }

            string curDir = Environment.CurrentDirectory;
            if (!string.IsNullOrEmpty(curDir))
            {
                candidates.Add(Path.Combine(curDir, "tools", "yt-dlp.exe"));
                candidates.Add(Path.Combine(curDir, "yt-dlp.exe"));
                candidates.Add(Path.Combine(curDir, "dist", "windows", "tools", "yt-dlp.exe"));
                candidates.Add(Path.Combine(curDir, "src", "windows", "DVVdownloader", "tools", "yt-dlp.exe"));

                try
                {
                    var dir = new DirectoryInfo(curDir);
                    for (int i = 0; i < 5 && dir != null; i++)
                    {
                        candidates.Add(Path.Combine(dir.FullName, "tools", "yt-dlp.exe"));
                        candidates.Add(Path.Combine(dir.FullName, "dist", "windows", "tools", "yt-dlp.exe"));
                        candidates.Add(Path.Combine(dir.FullName, "src", "windows", "DVVdownloader", "tools", "yt-dlp.exe"));
                        dir = dir.Parent;
                    }
                }
                catch { }
            }

            foreach (var candidate in candidates)
            {
                try
                {
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
                catch { }
            }

            string? inPath = FindExecutableInPath("yt-dlp.exe");
            if (!string.IsNullOrEmpty(inPath))
            {
                return inPath;
            }

            return "yt-dlp.exe";
        }

        public string ResolveFfmpegDirectory()
        {
            var settings = SettingsService.Instance.CurrentSettings;
            if (!string.IsNullOrWhiteSpace(settings.CustomFfmpegPath) && File.Exists(settings.CustomFfmpegPath))
            {
                return Path.GetDirectoryName(settings.CustomFfmpegPath) ?? string.Empty;
            }

            var candidates = new List<string>();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                candidates.Add(Path.Combine(baseDir, "tools"));
                candidates.Add(baseDir);

                try
                {
                    var dir = new DirectoryInfo(baseDir);
                    for (int i = 0; i < 5 && dir != null; i++)
                    {
                        candidates.Add(Path.Combine(dir.FullName, "tools"));
                        candidates.Add(Path.Combine(dir.FullName, "dist", "windows", "tools"));
                        candidates.Add(Path.Combine(dir.FullName, "src", "windows", "DVVdownloader", "tools"));
                        dir = dir.Parent;
                    }
                }
                catch { }
            }

            string curDir = Environment.CurrentDirectory;
            if (!string.IsNullOrEmpty(curDir))
            {
                candidates.Add(Path.Combine(curDir, "tools"));
                candidates.Add(curDir);
                candidates.Add(Path.Combine(curDir, "dist", "windows", "tools"));
                candidates.Add(Path.Combine(curDir, "src", "windows", "DVVdownloader", "tools"));

                try
                {
                    var dir = new DirectoryInfo(curDir);
                    for (int i = 0; i < 5 && dir != null; i++)
                    {
                        candidates.Add(Path.Combine(dir.FullName, "tools"));
                        candidates.Add(Path.Combine(dir.FullName, "dist", "windows", "tools"));
                        candidates.Add(Path.Combine(dir.FullName, "src", "windows", "DVVdownloader", "tools"));
                        dir = dir.Parent;
                    }
                }
                catch { }
            }

            foreach (var candidate in candidates)
            {
                try
                {
                    if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "ffmpeg.exe")))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
                catch { }
            }

            string? inPath = FindExecutableInPath("ffmpeg.exe");
            if (!string.IsNullOrEmpty(inPath))
            {
                return Path.GetDirectoryName(inPath) ?? string.Empty;
            }

            return string.Empty;
        }

        public bool AreBinariesAvailable(out string ytDlpStatus, out string ffmpegStatus)
        {
            string ytDlp = ResolveYtDlpPath();
            bool ytDlpOk = File.Exists(ytDlp) || FindExecutableInPath(Path.GetFileName(ytDlp)) != null;
            ytDlpStatus = ytDlpOk ? Loc.Get("EngineAvailable") : Loc.Get("YtDlpMissing");

            string ffmpegDir = ResolveFfmpegDirectory();
            bool ffmpegOk = (!string.IsNullOrEmpty(ffmpegDir) && File.Exists(Path.Combine(ffmpegDir, "ffmpeg.exe")))
                || FindExecutableInPath("ffmpeg.exe") != null;
            ffmpegStatus = ffmpegOk ? Loc.Get("EngineAvailable") : Loc.Get("FfmpegMissing");

            return ytDlpOk;
        }

        public async Task<MediaMetadata> ExtractMetadataAsync(string url, CancellationToken ct)
        {
            string ytDlpPath = ResolveYtDlpPath();
            if (!File.Exists(ytDlpPath) && FindExecutableInPath(Path.GetFileName(ytDlpPath)) == null)
            {
                throw new FileNotFoundException(Loc.Get("ErrYtDlpNotFound"));
            }

            string normalizedUrl = await PlatformDetector.NormalizeUrlAsync(url, ct);
            LogService.Instance.Info($"Analyzing media: {normalizedUrl}", "YtDlpEngine");

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            // Güvenli parametre listesi - ASLA string birleştirme yapılmaz
            psi.ArgumentList.Add("--dump-json");
            psi.ArgumentList.Add("--no-warnings");
            psi.ArgumentList.Add("--no-playlist");
            psi.ArgumentList.Add("--no-check-certificates");
            psi.ArgumentList.Add("--geo-bypass");
            psi.ArgumentList.Add("--socket-timeout");
            psi.ArgumentList.Add("20");

            // Gerçekçi tarayıcı başlıkları (Bot duvarlarını ve TCP 10054 sıfırlamalarını önler)
            psi.ArgumentList.Add("--user-agent");
            psi.ArgumentList.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
            psi.ArgumentList.Add("--add-header");
            psi.ArgumentList.Add("Accept-Language:en-US,en;q=0.9,tr;q=0.8");
            psi.ArgumentList.Add("--add-header");
            psi.ArgumentList.Add("Sec-Ch-Ua:\"Chromium\";v=\"128\", \"Not;A=Brand\";v=\"24\"");

            if (normalizedUrl.Contains("instagram.com", StringComparison.OrdinalIgnoreCase))
            {
                psi.ArgumentList.Add("--add-header");
                psi.ArgumentList.Add("Referer:https://www.instagram.com/");
            }
            else if (normalizedUrl.Contains("pinterest.com", StringComparison.OrdinalIgnoreCase))
            {
                psi.ArgumentList.Add("--add-header");
                psi.ArgumentList.Add("Referer:https://www.pinterest.com/");
            }

            string ffmpegDir = ResolveFfmpegDirectory();
            if (!string.IsNullOrEmpty(ffmpegDir))
            {
                psi.ArgumentList.Add("--ffmpeg-location");
                psi.ArgumentList.Add(ffmpegDir);
            }

            psi.ArgumentList.Add(normalizedUrl);

            using var process = new Process { StartInfo = psi };
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                LogService.Instance.Error($"Could not start yt-dlp ({ytDlpPath}): {ex.Message}", "YtDlpEngine");
                throw new InvalidOperationException(Loc.Format("ErrEngineStart", ex.Message));
            }

            using var registration = ct.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); } catch { }
            });

            string stdout = await process.StandardOutput.ReadToEndAsync(ct);
            string stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
            {
                LogService.Instance.Warning($"yt-dlp analysis failed (exit code {process.ExitCode}): {stderr.Trim()}", "YtDlpEngine");

                // Pinterest için dahili CDN / HTML fallback
                if (normalizedUrl.Contains("pinterest.com", StringComparison.OrdinalIgnoreCase) || normalizedUrl.Contains("pin.it", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return await ExtractPinterestFallbackAsync(normalizedUrl, ct);
                    }
                    catch (Exception pinEx)
                    {
                        LogService.Instance.Error($"Pinterest fallback analysis failed: {pinEx.Message}", "YtDlpEngine");
                    }
                }

                // Instagram için dahili embed fallback
                if (normalizedUrl.Contains("instagram.com", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return await ExtractInstagramFallbackAsync(normalizedUrl, ct);
                    }
                    catch (Exception igEx)
                    {
                        LogService.Instance.Error($"Instagram fallback analysis failed: {igEx.Message}", "YtDlpEngine");
                    }
                }

                LogService.Instance.Error($"Analysis failed. Error: {stderr}", "YtDlpEngine");
                throw new InvalidOperationException(!string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : Loc.Get("ErrMediaInfo"));
            }

            var meta = ParseMetadataJson(stdout, normalizedUrl);
            if (meta.Platform == "Pinterest")
            {
                await TryEnrichPinterestMetadataAsync(meta, normalizedUrl, ct);
            }
            return meta;
        }

        #region Güvenli JSON Ayrıştırma Yardımcıları
        private static int GetIntOrDefault(JsonElement element, string propName, int defaultValue = 0)
        {
            if (element.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val))
            {
                return val;
            }
            return defaultValue;
        }

        private static long? GetInt64OrNull(JsonElement element, string propName)
        {
            if (element.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var val))
            {
                return val;
            }
            return null;
        }

        private static double GetDoubleOrDefault(JsonElement element, string propName, double defaultValue = 0.0)
        {
            if (element.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var val))
            {
                return val;
            }
            return defaultValue;
        }

        private static double? GetDoubleOrNull(JsonElement element, string propName)
        {
            if (element.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var val))
            {
                return val;
            }
            return null;
        }

        private static string GetStringOrDefault(JsonElement element, string propName, string defaultValue = "")
        {
            if (element.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString() ?? defaultValue;
                }
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetRawText();
                }
            }
            return defaultValue;
        }
        #endregion

        private MediaMetadata ParseMetadataJson(string json, string originalUrl)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var meta = new MediaMetadata
            {
                Id = GetStringOrDefault(root, "id"),
                Title = GetStringOrDefault(root, "title", "Media"),
                Description = GetStringOrDefault(root, "description"),
                Uploader = GetStringOrDefault(root, "uploader"),
                Duration = GetDoubleOrDefault(root, "duration", 0),
                ThumbnailUrl = GetStringOrDefault(root, "thumbnail"),
                WebpageUrl = originalUrl,
                Platform = PlatformDetector.DetectPlatform(originalUrl)
            };

            // Formatları ayıkla
            if (root.TryGetProperty("formats", out var formatsProp) && formatsProp.ValueKind == JsonValueKind.Array)
            {
                var bestFormatsByHeight = new Dictionary<int, MediaFormatItem>();
                var scoresByHeight = new Dictionary<int, double>();
                var seenAudioBitrates = new HashSet<int>();

                // 1. Önce en iyi ses akışının boyutunu ve bilgilerini tespit et
                long bestAudioBytes = 0;
                foreach (var f in formatsProp.EnumerateArray())
                {
                    string vcodec = GetStringOrDefault(f, "vcodec", "none");
                    string acodec = GetStringOrDefault(f, "acodec", "none");
                    if ((vcodec == "none" || string.IsNullOrEmpty(vcodec)) && acodec != "none")
                    {
                        long? sz = GetInt64OrNull(f, "filesize") ?? GetInt64OrNull(f, "filesize_approx");
                        if (!sz.HasValue && meta.Duration > 0)
                        {
                            double? abr = GetDoubleOrNull(f, "abr") ?? GetDoubleOrNull(f, "tbr");
                            if (abr.HasValue && abr.Value > 0)
                            {
                                sz = (long)((abr.Value * 1000.0 / 8.0) * meta.Duration);
                            }
                        }
                        if (sz.HasValue && sz.Value > bestAudioBytes)
                        {
                            bestAudioBytes = sz.Value;
                        }
                    }
                }

                if (bestAudioBytes <= 0 && meta.Duration > 0)
                {
                    // Standart 128 kbps ses boyutu tahmini
                    bestAudioBytes = (long)((128.0 * 1000.0 / 8.0) * meta.Duration);
                }

                // 2. Formatları ayrıştır ve en kaliteli/uyumlu olanları seç
                foreach (var f in formatsProp.EnumerateArray())
                {
                    string formatId = GetStringOrDefault(f, "format_id");
                    
                    // Storyboard önizleme resimlerini (sb0, sb1 vb.) format olarak atla
                    if (formatId.StartsWith("sb", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string vcodec = GetStringOrDefault(f, "vcodec", "none");
                    string acodec = GetStringOrDefault(f, "acodec", "none");
                    string ext = GetStringOrDefault(f, "ext", "mp4");
                    string proto = GetStringOrDefault(f, "protocol", "https");
                    
                    if (ext.Equals("mhtml", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    int height = GetIntOrDefault(f, "height", 0);
                    int width = GetIntOrDefault(f, "width", 0);
                    long? filesize = GetInt64OrNull(f, "filesize");
                    long? filesizeApprox = GetInt64OrNull(f, "filesize_approx");
                    double? fps = GetDoubleOrNull(f, "fps");
                    double? tbr = GetDoubleOrNull(f, "tbr");

                    string directUrl = GetStringOrDefault(f, "url");
                    directUrl = PlatformDetector.RewriteCdnHostIfNeeded(directUrl);

                    bool isM3u8 = proto.Contains("m3u8", StringComparison.OrdinalIgnoreCase);
                    bool hasAudioName = formatId.Contains("audio", StringComparison.OrdinalIgnoreCase) || 
                                        directUrl.Contains("_audio", StringComparison.OrdinalIgnoreCase);

                    // Ses akışı tespiti: vcodec yoksa veya format/URL audio içeriyorsa kesinlikle ses akışıdır
                    bool isAudioOnly = hasAudioName || 
                                       ((vcodec == "none" || string.IsNullOrEmpty(vcodec)) && 
                                        (acodec != "none" || (height <= 0 && width <= 0)));

                    if (isAudioOnly)
                    {
                        height = 0;
                        width = 0;
                    }

                    // Yalnızca video (ayrık görüntü) akışı tespiti (ses içermez, harici ses ile birleştirilmelidir)
                    bool isVideoOnly = !isAudioOnly && 
                                       vcodec != "none" && !string.IsNullOrEmpty(vcodec) && 
                                       (acodec == "none" || string.IsNullOrEmpty(acodec) || directUrl.Contains("_video", StringComparison.OrdinalIgnoreCase));

                    // Birleşik akış (görüntü ve ses bir arada progressive container)
                    bool isCombined = !isAudioOnly && !isVideoOnly &&
                                      ((vcodec != "none" && acodec != "none" && !string.IsNullOrEmpty(acodec)) ||
                                       (!isM3u8 && ext.Equals("mp4", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(directUrl)));

                    // Çözünürlük dizesinden yükseklik tamamlama (Örn: "1920x1080")
                    if (height <= 0 && !isAudioOnly && vcodec != "none")
                    {
                        string res = GetStringOrDefault(f, "resolution");
                        if (!string.IsNullOrEmpty(res) && res.Contains('x'))
                        {
                            var parts = res.Split('x');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int parsedH))
                            {
                                height = parsedH;
                            }
                        }
                    }

                    if (height <= 0 && width > 0 && !isAudioOnly)
                    {
                        height = width >= 1080 ? 1080 : (width >= 720 ? 720 : (width >= 480 ? 480 : width));
                    }

                    // Eğer yükseklik hala 0 ama GERÇEK bir video akışı ise makul varsayılan ver
                    if (height <= 0 && !isAudioOnly && (vcodec != "none" || (!isM3u8 && !string.IsNullOrEmpty(directUrl))))
                    {
                        height = 720;
                    }

                    // Gerçekçi dosya boyutu hesabı (DASH video akışına ses akışı eklenir)
                    long? effectiveSize = filesize ?? filesizeApprox;
                    if (!effectiveSize.HasValue && tbr.HasValue && tbr.Value > 0 && meta.Duration > 0)
                    {
                        double rate = isM3u8 ? Math.Min(tbr.Value, 4000.0) : tbr.Value;
                        effectiveSize = (long)((rate * 1000.0 / 8.0) * meta.Duration);
                    }

                    if (isVideoOnly && effectiveSize.HasValue && bestAudioBytes > 0)
                    {
                        effectiveSize += bestAudioBytes;
                    }

                    var item = new MediaFormatItem
                    {
                        FormatId = formatId,
                        VCodec = vcodec,
                        ACodec = acodec,
                        Ext = ext,
                        Height = height,
                        Width = width,
                        Filesize = effectiveSize,
                        FilesizeApprox = effectiveSize,
                        Fps = fps,
                        Tbr = tbr,
                        DirectUrl = directUrl
                    };

                    if (isAudioOnly)
                    {
                        int approxBitrate = (int)Math.Round(tbr ?? 128);
                        if (!seenAudioBitrates.Contains(approxBitrate))
                        {
                            seenAudioBitrates.Add(approxBitrate);
                            meta.Formats.Add(item);
                        }
                    }
                    else if (height > 0)
                    {
                        // Kalite ve uyumluluk skorlaması:
                        // 1. Birleşik progressive akışlar (m3u8 OLMAYAN, ses+video bir arada) EN YÜKSEK önceliğe sahiptir (+20000 puan)
                        //    Bu, Instagram ve benzeri platformlarda TCP 10054 kopma sorunlarını tamamen engeller!
                        // 2. Doğrudan HTTPS akışları önceliklidir (+10000 puan)
                        // 3. H.264 (avc1) tüm Windows oynatıcılarıyla sorunsuz çalışır (+1500 puan)
                        // 4. VP9 (+800 puan), AV01 (+400 puan)
                        // 5. Yüksek FPS ve gerçek bitrate bonusu
                        double score = 0;
                        if (isCombined) score += 20000;
                        if (!isM3u8) score += 10000;
                        if (!string.IsNullOrEmpty(directUrl) && !isM3u8) score += 5000;

                        if (vcodec.StartsWith("avc1", StringComparison.OrdinalIgnoreCase) || vcodec.Contains("h264", StringComparison.OrdinalIgnoreCase))
                        {
                            score += 1500;
                        }
                        else if (vcodec.StartsWith("vp09", StringComparison.OrdinalIgnoreCase) || vcodec.StartsWith("vp9", StringComparison.OrdinalIgnoreCase))
                        {
                            score += 800;
                        }
                        else if (vcodec.StartsWith("av01", StringComparison.OrdinalIgnoreCase))
                        {
                            score += 400;
                        }

                        score += (fps ?? 30) * 10;
                        score += Math.Min(tbr ?? 0, 4000);

                        if (bestFormatsByHeight.TryGetValue(height, out var existing))
                        {
                            double existingScore = scoresByHeight.GetValueOrDefault(height, 0);
                            if (score > existingScore)
                            {
                                bestFormatsByHeight[height] = item;
                                scoresByHeight[height] = score;
                            }
                        }
                        else
                        {
                            bestFormatsByHeight[height] = item;
                            scoresByHeight[height] = score;
                        }
                    }
                }

                // En iyi video formatlarını ekle
                meta.Formats.AddRange(bestFormatsByHeight.Values);

                // Yüksek çözünürlükten düşüğe doğru sırala
                meta.Formats.Sort((a, b) => b.Height.CompareTo(a.Height));
            }

            // Eğer özel formatlar listelenemediyse standart yedek formatlar ekle
            if (meta.Formats.Count == 0)
            {
                meta.Formats.Add(new MediaFormatItem
                {
                    FormatId = "best",
                    Height = 1080,
                    Ext = "mp4",
                    Note = Loc.Get("QualityBest")
                });
                meta.Formats.Add(new MediaFormatItem
                {
                    FormatId = "bestaudio",
                    VCodec = "none",
                    ACodec = "mp3",
                    Ext = "mp3",
                    Note = Loc.Get("QualityMp3")
                });
            }

            return meta;
        }

        private static readonly Regex FnaCdnRegex = new(@"https://[A-Za-z0-9.-]+\.fna\.fbcdn\.net/", RegexOptions.Compiled);

        /// <summary>
        /// Türkiye'deki bazı servis sağlayıcılar *.fna.fbcdn.net adreslerini engelliyor; indirme bağlantı hatasıyla takılıyor.
        /// Instagram/Facebook'ta yt-dlp'nin bilgi JSON'u alınır, bu adresler scontent.cdninstagram.com'a çevrilip geçici dosyaya yazılır.
        /// Çevrilecek adres yoksa ya da bir şey ters giderse null döner ve normal indirme yapılır.
        /// </summary>
        private static async Task<string?> WriteCdnRewrittenInfoAsync(string ytDlpPath, string url, CancellationToken ct)
        {
            string platform = PlatformDetector.DetectPlatform(url);
            if (platform is not ("Instagram" or "Facebook")) return null;

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };
            psi.ArgumentList.Add("--dump-single-json");
            psi.ArgumentList.Add("--no-warnings");
            psi.ArgumentList.Add("--no-playlist");
            psi.ArgumentList.Add("--socket-timeout");
            psi.ArgumentList.Add("20");
            psi.ArgumentList.Add(url);

            try
            {
                using var process = Process.Start(psi)!;
                using var registration = ct.Register(() => { try { process.Kill(entireProcessTree: true); } catch { } });
                var stderrTask = process.StandardError.ReadToEndAsync();
                string json = await process.StandardOutput.ReadToEndAsync();
                await stderrTask;
                await process.WaitForExitAsync(ct);
                if (process.ExitCode != 0 || !FnaCdnRegex.IsMatch(json)) return null;

                string path = Path.Combine(Path.GetTempPath(), $"dvv-{Guid.NewGuid():N}.info.json");
                await File.WriteAllTextAsync(path, FnaCdnRegex.Replace(json, "https://scontent.cdninstagram.com/"), ct);
                return path;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogService.Instance.Warning($"CDN rewrite skipped: {ex.Message}", "YtDlpEngine");
                return null;
            }
        }

        public async Task DownloadAsync(
            DownloadTask task,
            Action<double, string, string> onProgress,
            CancellationToken ct)
        {
            string ytDlpPath = ResolveYtDlpPath();
            if (!File.Exists(ytDlpPath) && FindExecutableInPath(Path.GetFileName(ytDlpPath)) == null)
            {
                task.Status = DownloadStatus.Hata;
                task.ErrorMessage = Loc.Get("ErrYtDlpNotFoundShort");
                LogService.Instance.Error(task.ErrorMessage, "YtDlpEngine");
                throw new FileNotFoundException(Loc.Get("ErrYtDlpNotFound"));
            }
            string saveDir = SettingsService.Instance.CurrentSettings.DownloadDirectory;
            Directory.CreateDirectory(saveDir);

            // İndirme öncesi mevcut dosyaları kaydet (HATA-21: Yanlış dosya tespitini önler)
            var existingFiles = new HashSet<string>(
                Directory.GetFiles(saveDir).Select(Path.GetFullPath),
                StringComparer.OrdinalIgnoreCase);

            task.Status = DownloadStatus.Indiriliyor;
            LogService.Instance.Info($"Download started: {task.Title} ({task.SelectedQuality})", "YtDlpEngine");

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = saveDir,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            // Güvenli ArgumentList kullanımı
            psi.ArgumentList.Add("--newline");
            psi.ArgumentList.Add("--no-warnings");
            psi.ArgumentList.Add("--no-playlist");
            psi.ArgumentList.Add("--no-check-certificates");
            psi.ArgumentList.Add("--no-mtime");
            psi.ArgumentList.Add("--windows-filenames");
            psi.ArgumentList.Add("--force-overwrites");
            psi.ArgumentList.Add("--no-continue");
            psi.ArgumentList.Add("--no-cache-dir");

            string ffmpegDir = ResolveFfmpegDirectory();
            if (!string.IsNullOrEmpty(ffmpegDir))
            {
                psi.ArgumentList.Add("--ffmpeg-location");
                psi.ArgumentList.Add(ffmpegDir);
            }

            // Çıktı şablonu (Çözünürlük bazlı isimlendirme ile önceki indirmeyle çakışma ve eski dosyada takılı kalma önlenir)
            string outTemplate;
            if (task.IsAudioOnly)
            {
                outTemplate = Path.Combine(saveDir, "%(title)s.%(ext)s");
            }
            else if (task.SelectedHeight > 0)
            {
                outTemplate = Path.Combine(saveDir, $"%(title)s [{task.SelectedHeight}p].%(ext)s");
            }
            else
            {
                outTemplate = Path.Combine(saveDir, "%(title)s [%(resolution)s].%(ext)s");
            }
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add(outTemplate);

            // Format seçimi
            if (task.IsAudioOnly)
            {
                psi.ArgumentList.Add("-x");
                psi.ArgumentList.Add("--audio-format");
                psi.ArgumentList.Add("mp3");
                psi.ArgumentList.Add("--audio-quality");
                psi.ArgumentList.Add("0");
                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add("bestaudio/best");
            }
            else
            {
                int h = task.SelectedHeight > 0 ? task.SelectedHeight : 1080;
                string fmtId = !string.IsNullOrEmpty(task.SelectedFormatId) && task.SelectedFormatId != "best"
                    ? task.SelectedFormatId
                    : string.Empty;

                // Windows Media Player ve tüm standart oynatıcılarla maksimum uyum için format tercihi
                psi.ArgumentList.Add("--format-sort");
                psi.ArgumentList.Add("res,fps,vcodec:h264,acodec:m4a");

                bool isSocial = PlatformDetector.IsSocialPlatform(task.Url) || !string.IsNullOrEmpty(task.DirectUrl);
                if (isSocial)
                {
                    // Sosyal medya platformlarında (Instagram, Pinterest, TikTok vb.) ayrık ses+video akışlarını da birleştir
                    psi.ArgumentList.Add("-f");
                    if (!string.IsNullOrEmpty(fmtId))
                    {
                        psi.ArgumentList.Add($"{fmtId}+ba[ext=m4a]/{fmtId}+ba/{fmtId}/bv*[height<={h}]+ba/b/best/(bv*+ba/b)");
                    }
                    else
                    {
                        psi.ArgumentList.Add($"bv*[height<={h}]+ba/b/best/(bv*+ba/b)");
                    }
                }
                else
                {
                    // YouTube ve ayrık DASH desteği sunan platformlar
                    psi.ArgumentList.Add("-f");
                    if (!string.IsNullOrEmpty(fmtId))
                    {
                        psi.ArgumentList.Add($"{fmtId}+ba[ext=m4a]/{fmtId}+ba/{fmtId}/bv*[height={h}]+ba[ext=m4a]/bv*[height={h}]+ba/bv*[height<={h}]+ba/b[height<={h}]/best");
                    }
                    else
                    {
                        psi.ArgumentList.Add($"bv*[height={h}]+ba[ext=m4a]/bv*[height={h}]+ba/bv*[height<={h}]+ba/b[height<={h}]/best");
                    }
                }

                psi.ArgumentList.Add("--merge-output-format");
                psi.ArgumentList.Add("mp4");
            }

            // Gerçekçi tarayıcı başlıkları ve parametreleri
            psi.ArgumentList.Add("--geo-bypass");
            psi.ArgumentList.Add("--socket-timeout");
            psi.ArgumentList.Add("20");
            psi.ArgumentList.Add("--user-agent");
            psi.ArgumentList.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
            psi.ArgumentList.Add("--add-header");
            psi.ArgumentList.Add("Accept-Language:en-US,en;q=0.9,tr;q=0.8");

            // yt-dlp'ye her zaman sayfa URL'si verilir: başlık, ses+video birleştirme ve site başlıkları ancak böyle çalışır.
            // Format CDN adresi yalnızca yt-dlp'nin tanımadığı, yedek ayrıştırıcıların ürettiği formatlarda kullanılır.
            bool isFallbackFormat = task.SelectedFormatId is "pinterest_direct" or "pinterest_hls" or "instagram_direct";

            string downloadTarget = (isFallbackFormat || string.IsNullOrEmpty(task.Url)) && !string.IsNullOrEmpty(task.DirectUrl)
                ? PlatformDetector.RewriteCdnHostIfNeeded(task.DirectUrl)
                : task.Url;

            if (task.Platform == "Instagram" || downloadTarget.Contains("instagram.com", StringComparison.OrdinalIgnoreCase) || downloadTarget.Contains("cdninstagram.com", StringComparison.OrdinalIgnoreCase))
            {
                psi.ArgumentList.Add("--add-header");
                psi.ArgumentList.Add("Referer:https://www.instagram.com/");
            }
            else if (task.Platform == "Pinterest" || downloadTarget.Contains("pinterest.com", StringComparison.OrdinalIgnoreCase) || downloadTarget.Contains("pinimg.com", StringComparison.OrdinalIgnoreCase))
            {
                psi.ArgumentList.Add("--add-header");
                psi.ArgumentList.Add("Referer:https://www.pinterest.com/");
            }

            string? infoJsonPath = downloadTarget == task.Url
                ? await WriteCdnRewrittenInfoAsync(ytDlpPath, task.Url, ct)
                : null;
            if (infoJsonPath != null)
            {
                psi.ArgumentList.Add("--load-info-json");
                psi.ArgumentList.Add(infoJsonPath);
            }
            else
            {
                psi.ArgumentList.Add(downloadTarget);
            }

            using var process = new Process { StartInfo = psi };
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                task.Status = DownloadStatus.Hata;
                task.ErrorMessage = ex.Message;
                LogService.Instance.Error($"Could not start the download process: {ex.Message}", "YtDlpEngine");
                throw;
            }

            using var registration = ct.Register(() =>
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    task.Status = DownloadStatus.IptalEdildi;
                    LogService.Instance.Warning($"Download cancelled: {task.Title}", "YtDlpEngine");
                }
                catch { }
            });

            string? lastDownloadedFile = null;

            // Satır satır stdout dinleme
            var readOutputTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardOutput.ReadLineAsync(ct)) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    LogService.Instance.Engine(line, "yt-dlp");

                    // İlerleme eşleme
                    var match = ProgressRegex.Match(line);
                    if (match.Success)
                    {
                        if (double.TryParse(match.Groups["percent"].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double p))
                        {
                            string speed = match.Groups["speed"].Value;
                            string eta = match.Groups["eta"].Value;
                            onProgress(p, speed, eta);
                        }
                    }

                    // İşleme veya birleştirme adımları
                    if (line.Contains("[Merger]") || line.Contains("[ExtractAudio]") || line.Contains("[Fixup"))
                    {
                        task.Status = DownloadStatus.Birlestiriliyor;
                    }

                    // Hedef dosya adı tespiti
                    var destMatch = DestRegex.Match(line);
                    if (destMatch.Success)
                    {
                        string candidate = destMatch.Groups["file"].Value.Trim();
                        if (!string.IsNullOrEmpty(candidate))
                        {
                            lastDownloadedFile = candidate;
                        }
                    }
                }
            });

            string stderr = await process.StandardError.ReadToEndAsync();
            await readOutputTask;
            try
            {
                await process.WaitForExitAsync(ct);
            }
            finally
            {
                if (infoJsonPath != null) try { File.Delete(infoJsonPath); } catch { }
            }

            if (ct.IsCancellationRequested)
            {
                task.Status = DownloadStatus.IptalEdildi;
                return;
            }

            if (process.ExitCode != 0)
            {
                task.Status = DownloadStatus.Hata;
                task.ErrorMessage = !string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : Loc.Get("ErrDownloadFailed");
                LogService.Instance.Error($"Download error: {task.ErrorMessage}", "YtDlpEngine");
                throw new InvalidOperationException(task.ErrorMessage);
            }

            task.Status = DownloadStatus.Tamamlandi;
            task.ProgressPercentage = 100;
            task.CompletedAt = DateTime.Now;
            task.DownloadSpeed = Loc.Get("StatusCompleted");
            task.Eta = "00:00";

            // Dosya yolu doğrulama veya son indirilen dosyayı bulma
            if (string.IsNullOrEmpty(lastDownloadedFile) || !File.Exists(lastDownloadedFile))
            {
                try
                {
                    var recentFile = Directory.GetFiles(saveDir)
                        .Where(f => !existingFiles.Contains(Path.GetFullPath(f)))
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (recentFile != null && (DateTime.Now - recentFile.LastWriteTime).TotalMinutes < 5)
                    {
                        lastDownloadedFile = recentFile.FullName;
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(lastDownloadedFile) && File.Exists(lastDownloadedFile))
            {
                task.TargetFilePath = lastDownloadedFile;
                var fi = new FileInfo(lastDownloadedFile);
                double mb = fi.Length / (1024.0 * 1024.0);
                task.FileSizeFormatted = mb >= 1024 ? $"{mb / 1024.0:F2} GB" : $"{mb:F1} MB";
            }

            LogService.Instance.Info($"Download completed: {task.Title}", "YtDlpEngine");

            // Geçmişe ekle
            HistoryService.Instance.AddHistoryItem(new HistoryItem
            {
                Id = task.Id,
                Title = task.Title,
                Url = task.Url,
                Platform = task.Platform,
                ThumbnailUrl = task.ThumbnailUrl,
                FilePath = task.TargetFilePath,
                FileSizeFormatted = task.FileSizeFormatted,
                CompletedAt = DateTime.Now
            });
        }

        #region Yedek (Fallback) Ayrıştırıcılar
        private async Task<MediaMetadata> ExtractPinterestFallbackAsync(string url, CancellationToken ct)
        {
            LogService.Instance.Info($"Starting Pinterest page parsing: {url}", "YtDlpEngine");

            string targetUrl = url;
            if (targetUrl.Contains("pin.it", StringComparison.OrdinalIgnoreCase))
            {
                targetUrl = await PlatformDetector.NormalizeUrlAsync(targetUrl, ct);
            }

            var match = Regex.Match(targetUrl, @"pinterest\.com/pin/(\d+)", RegexOptions.IgnoreCase);
            string pinId = match.Success ? match.Groups[1].Value : string.Empty;
            string fetchUrl = !string.IsNullOrEmpty(pinId)
                ? $"https://tr.pinterest.com/pin/{pinId}/"
                : targetUrl;

            using var req = new HttpRequestMessage(HttpMethod.Get, fetchUrl);
            req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
            req.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            req.Headers.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
            req.Headers.Add("Referer", "https://www.pinterest.com/");

            using var resp = await PlatformDetector.HttpClient.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            string html = await resp.Content.ReadAsStringAsync(ct);

            string title = "Pinterest Video";
            var titleMatch = Regex.Match(html, @"<title>([^<]+)</title>", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[1].Value.Replace(" | Pinterest", "").Trim();
            }

            string thumbnail = string.Empty;
            var thumbMatch = Regex.Match(html, @"""thumbnail""\s*:\s*""([^""]+)""");
            if (thumbMatch.Success)
            {
                thumbnail = thumbMatch.Groups[1].Value.Replace(@"\/", "/");
            }
            else
            {
                var ogThumb = Regex.Match(html, @"property=[""']og:image[""']\s+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                if (ogThumb.Success) thumbnail = ogThumb.Groups[1].Value;
            }

            string directMp4 = string.Empty;
            var mp4Matches = Regex.Matches(html, @"""url""\s*:\s*""(https?:[^""]+\.mp4[^""]*)""");
            var candidates = new List<string>();
            foreach (Match m in mp4Matches)
            {
                string candidate = m.Groups[1].Value.Replace(@"\/", "/");
                if (candidate.Contains(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    if (candidate.Contains("_audio", StringComparison.OrdinalIgnoreCase) || candidate.Contains("audio.", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                var generalMp4 = Regex.Matches(html, @"(https?://[^""'\s<>]+\.mp4[^""'\s<>]*)", RegexOptions.IgnoreCase);
                foreach (Match m in generalMp4)
                {
                    string candidate = m.Groups[1].Value.Replace(@"\/", "/");
                    if (candidate.Contains(".mp4", StringComparison.OrdinalIgnoreCase))
                    {
                        if (candidate.Contains("_audio", StringComparison.OrdinalIgnoreCase) || candidate.Contains("audio.", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        candidates.Add(candidate);
                    }
                }
            }

            directMp4 = candidates.FirstOrDefault(c => c.Contains("720p", StringComparison.OrdinalIgnoreCase) || c.Contains("720w", StringComparison.OrdinalIgnoreCase))
                     ?? candidates.FirstOrDefault(c => c.Contains("expMp4", StringComparison.OrdinalIgnoreCase) && !c.Contains("_t1", StringComparison.OrdinalIgnoreCase))
                     ?? candidates.FirstOrDefault() ?? string.Empty;

            string m3u8Url = string.Empty;
            if (string.IsNullOrEmpty(directMp4))
            {
                var m3u8Matches = Regex.Matches(html, @"""url""\s*:\s*""(https?:[^""]+\.m3u8[^""]*)""");
                foreach (Match m in m3u8Matches)
                {
                    string candidate = m.Groups[1].Value.Replace(@"\/", "/");
                    if (candidate.Contains("_audio", StringComparison.OrdinalIgnoreCase)) continue;
                    m3u8Url = candidate;
                    if (!Regex.IsMatch(candidate, @"_\d+w\.m3u8")) // Master manifest tercih edilir
                    {
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(directMp4) && string.IsNullOrEmpty(m3u8Url))
            {
                throw new InvalidOperationException(Loc.Get("ErrPinterestNoVideo"));
            }

            var meta = new MediaMetadata
            {
                WebpageUrl = targetUrl,
                Title = title,
                Platform = "Pinterest",
                ThumbnailUrl = thumbnail,
                Duration = 0
            };

            if (!string.IsNullOrEmpty(directMp4))
            {
                meta.Formats.Add(new MediaFormatItem
                {
                    FormatId = "pinterest_direct",
                    Note = Loc.Get("Quality720Original"),
                    Resolution = "720p",
                    Height = 720,
                    Ext = "mp4",
                    DirectUrl = directMp4
                });
            }

            if (!string.IsNullOrEmpty(m3u8Url))
            {
                meta.Formats.Add(new MediaFormatItem
                {
                    FormatId = "pinterest_hls",
                    Note = Loc.Get("QualityHlsAuto"),
                    Resolution = Loc.Get("QualityAuto"),
                    Height = 720,
                    Ext = "mp4",
                    DirectUrl = m3u8Url
                });
            }

            return meta;
        }

        private async Task TryEnrichPinterestMetadataAsync(MediaMetadata meta, string url, CancellationToken ct)
        {
            try
            {
                // Zaten doğrudan progressive MP4 (720p/720w) formatı varsa zenginleştirmeye gerek yok
                bool hasDirectMp4 = meta.Formats.Any(f => !f.IsAudioOnly && f.Ext.Equals("mp4", StringComparison.OrdinalIgnoreCase) &&
                    (f.FormatId.Equals("pinterest_direct", StringComparison.OrdinalIgnoreCase) ||
                     f.DirectUrl.Contains("720p", StringComparison.OrdinalIgnoreCase) ||
                     f.DirectUrl.Contains("720w", StringComparison.OrdinalIgnoreCase) ||
                     (f.DirectUrl.Contains("expMp4", StringComparison.OrdinalIgnoreCase) && !f.DirectUrl.Contains("_audio", StringComparison.OrdinalIgnoreCase))));

                if (hasDirectMp4) return;

                string targetUrl = url;
                if (targetUrl.Contains("pin.it", StringComparison.OrdinalIgnoreCase))
                {
                    targetUrl = await PlatformDetector.NormalizeUrlAsync(targetUrl, ct);
                }

                var match = Regex.Match(targetUrl, @"pinterest\.com/pin/(\d+)", RegexOptions.IgnoreCase);
                string pinId = match.Success ? match.Groups[1].Value : string.Empty;
                string fetchUrl = !string.IsNullOrEmpty(pinId)
                    ? $"https://tr.pinterest.com/pin/{pinId}/"
                    : targetUrl;

                using var req = new HttpRequestMessage(HttpMethod.Get, fetchUrl);
                req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
                req.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                req.Headers.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
                req.Headers.Add("Referer", "https://www.pinterest.com/");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                using var resp = await PlatformDetector.HttpClient.SendAsync(req, cts.Token);
                if (!resp.IsSuccessStatusCode) return;

                string html = await resp.Content.ReadAsStringAsync(cts.Token);

                // Gerçek başlığı tamamla
                if (string.IsNullOrEmpty(meta.Title) || meta.Title.StartsWith("Pinterest video #", StringComparison.OrdinalIgnoreCase))
                {
                    var titleMatch = Regex.Match(html, @"<title>([^<]+)</title>", RegexOptions.IgnoreCase);
                    if (titleMatch.Success)
                    {
                        string cleanTitle = titleMatch.Groups[1].Value.Replace(" | Pinterest", "").Trim();
                        if (!string.IsNullOrWhiteSpace(cleanTitle))
                        {
                            meta.Title = cleanTitle;
                        }
                    }
                }

                // Doğrudan progressive MP4 (720w/720p/expMp4 ses ve video birleşik) akışını tespit et
                var mp4Matches = Regex.Matches(html, @"""url""\s*:\s*""(https?:[^""]+\.mp4[^""]*)""");
                var candidates = new List<string>();
                foreach (Match m in mp4Matches)
                {
                    string candidate = m.Groups[1].Value.Replace(@"\/", "/");
                    if (candidate.Contains(".mp4", StringComparison.OrdinalIgnoreCase))
                    {
                        if (candidate.Contains("_audio", StringComparison.OrdinalIgnoreCase) || candidate.Contains("audio.", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        candidates.Add(candidate);
                    }
                }

                if (candidates.Count == 0)
                {
                    var generalMp4 = Regex.Matches(html, @"(https?://[^""'\s<>]+\.mp4[^""'\s<>]*)", RegexOptions.IgnoreCase);
                    foreach (Match m in generalMp4)
                    {
                        string candidate = m.Groups[1].Value.Replace(@"\/", "/");
                        if (candidate.Contains(".mp4", StringComparison.OrdinalIgnoreCase))
                        {
                            if (candidate.Contains("_audio", StringComparison.OrdinalIgnoreCase) || candidate.Contains("audio.", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            candidates.Add(candidate);
                        }
                    }
                }

                string bestMp4 = candidates.FirstOrDefault(c => c.Contains("720p", StringComparison.OrdinalIgnoreCase) || c.Contains("720w", StringComparison.OrdinalIgnoreCase))
                              ?? candidates.FirstOrDefault(c => c.Contains("expMp4", StringComparison.OrdinalIgnoreCase) && !c.Contains("_t1", StringComparison.OrdinalIgnoreCase))
                              ?? candidates.FirstOrDefault() ?? string.Empty;

                if (!string.IsNullOrEmpty(bestMp4))
                {
                    var directItem = new MediaFormatItem
                    {
                        FormatId = "pinterest_direct",
                        Note = Loc.Get("Quality720Original"),
                        Resolution = "720p",
                        Height = 720,
                        Width = 1280,
                        Ext = "mp4",
                        DirectUrl = bestMp4
                    };

                    meta.Formats.Insert(0, directItem);
                    LogService.Instance.Info($"Found Pinterest direct HD stream: {bestMp4}", "YtDlpEngine");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Warning($"Skipped Pinterest metadata enrichment: {ex.Message}", "YtDlpEngine");
            }
        }

        private async Task<MediaMetadata> ExtractInstagramFallbackAsync(string url, CancellationToken ct)
        {
            LogService.Instance.Info($"Starting Instagram embed parsing: {url}", "YtDlpEngine");

            string cleanUrl = await PlatformDetector.NormalizeUrlAsync(url, ct);
            if (!cleanUrl.EndsWith("/")) cleanUrl += "/";
            string embedUrl = $"{cleanUrl}embed/captioned/";

            using var req = new HttpRequestMessage(HttpMethod.Get, embedUrl);
            req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
            req.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            req.Headers.Add("Accept-Language", "en-US,en;q=0.9,tr;q=0.8");
            req.Headers.Add("Referer", "https://www.instagram.com/");

            using var resp = await PlatformDetector.HttpClient.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            string html = await resp.Content.ReadAsStringAsync(ct);

            string videoUrl = string.Empty;
            var videoMatch = Regex.Match(html, @"""video_url""\s*:\s*""([^""]+)""");
            if (videoMatch.Success)
            {
                videoUrl = videoMatch.Groups[1].Value;
            }
            else
            {
                var rawMp4 = Regex.Match(html, @"https?://[^\s""'<>]+\.mp4[^\s""'<>]*", RegexOptions.IgnoreCase);
                if (rawMp4.Success)
                {
                    videoUrl = rawMp4.Value;
                }
            }

            if (string.IsNullOrEmpty(videoUrl))
            {
                throw new InvalidOperationException(Loc.Get("ErrInstagramNoVideo"));
            }

            videoUrl = Regex.Unescape(videoUrl).Replace(@"\/", "/");
            videoUrl = PlatformDetector.RewriteCdnHostIfNeeded(videoUrl);

            string title = "Instagram Video";
            var captionMatch = Regex.Match(html, @"class=[""']Caption[""'][^>]*>([^<]+)<", RegexOptions.IgnoreCase);
            if (captionMatch.Success)
            {
                title = captionMatch.Groups[1].Value.Trim();
            }
            else
            {
                var titleMatch = Regex.Match(html, @"<title>([^<]+)</title>", RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                {
                    title = titleMatch.Groups[1].Value.Replace("Instagram:", "").Trim();
                }
            }

            string thumb = string.Empty;
            var thumbMatch = Regex.Match(html, @"""display_url""\s*:\s*""([^""]+)""");
            if (thumbMatch.Success)
            {
                thumb = Regex.Unescape(thumbMatch.Groups[1].Value).Replace(@"\/", "/");
                thumb = PlatformDetector.RewriteCdnHostIfNeeded(thumb);
            }

            var meta = new MediaMetadata
            {
                WebpageUrl = cleanUrl,
                Title = title,
                Platform = "Instagram",
                ThumbnailUrl = thumb,
                Duration = 0
            };

            meta.Formats.Add(new MediaFormatItem
            {
                FormatId = "instagram_direct",
                Note = Loc.Get("Quality1080Original"),
                Resolution = "1080p",
                Height = 1080,
                Ext = "mp4",
                DirectUrl = videoUrl
            });

            return meta;
        }
        #endregion
    }
}
