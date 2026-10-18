using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DVVdownloader.Services
{
    public static class PlatformDetector
    {
        private static readonly HttpClient s_httpClient = new(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CheckCertificateRevocationList = false
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public static HttpClient HttpClient => s_httpClient;

        public static string DetectPlatform(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "Bilinmiyor";

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return "Geçersiz URL";
            }

            string host = uri.Host.ToLowerInvariant();

            if (host.Contains("youtube.com") || host.Contains("youtu.be"))
                return "YouTube";

            if (host.Contains("tiktok.com"))
                return "TikTok";

            if (host.Contains("instagram.com"))
                return "Instagram";

            if (host.Contains("twitter.com") || host.Contains("x.com"))
                return "Twitter/X";

            if (host.Contains("pinterest.com") || host.Contains("pin.it"))
                return "Pinterest";

            if (host.Contains("facebook.com") || host.Contains("fb.watch"))
                return "Facebook";

            if (host.Contains("twitch.tv"))
                return "Twitch";

            if (host.Contains("vimeo.com"))
                return "Vimeo";

            if (host.Contains("soundcloud.com"))
                return "SoundCloud";

            if (host.Contains("reddit.com"))
                return "Reddit";

            if (url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase))
            {
                return "Doğrudan Medya";
            }

            return "Web Medyası";
        }

        public static bool IsSocialPlatform(string url)
        {
            string platform = DetectPlatform(url);
            return platform is "Instagram" or "Pinterest" or "TikTok" or "Twitter/X" or "Facebook" or "Reddit";
        }

        public static string RewriteCdnHostIfNeeded(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            // Meta/Instagram CDN: Türk telekom ve yerel ISP'lerin kilitlendiği *.fna.fbcdn.net
            // adreslerini sorunsuz global scontent.cdninstagram.com adresine yönlendir
            if (url.Contains(".fna.fbcdn.net", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(url);
                    var builder = new UriBuilder(uri)
                    {
                        Host = "scontent.cdninstagram.com"
                    };
                    return builder.Uri.ToString();
                }
                catch { }
            }

            return url;
        }

        public static async Task<string> NormalizeUrlAsync(string url, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            url = url.Trim();

            // 1. Pinterest pin.it kısaltılmış URL çözme
            if (url.Contains("pin.it", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
                    using var resp = await s_httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (resp.RequestMessage?.RequestUri != null)
                    {
                        string resolved = resp.RequestMessage.RequestUri.ToString();
                        if (resolved.Contains("pinterest.com/pin/", StringComparison.OrdinalIgnoreCase))
                        {
                            return CleanPinterestUrl(resolved);
                        }
                    }
                }
                catch { }

                // Fallback: unshorten.me servisi
                try
                {
                    string unshortenApi = $"https://unshorten.me/json/{Uri.EscapeDataString(url)}";
                    using var unshortReq = new HttpRequestMessage(HttpMethod.Get, unshortenApi);
                    unshortReq.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    var resp = await s_httpClient.SendAsync(unshortReq, ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        string body = await resp.Content.ReadAsStringAsync(ct);
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("resolved_url", out var resProp))
                        {
                            string resolved = resProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(resolved) && resolved.Contains("pinterest.com", StringComparison.OrdinalIgnoreCase))
                            {
                                return CleanPinterestUrl(resolved);
                            }
                        }
                    }
                }
                catch { }
            }

            // 2. Standart Pinterest URL temizleme (sent/?invite_code vb. takip parametrelerini temizle)
            if (url.Contains("pinterest.com/pin/", StringComparison.OrdinalIgnoreCase))
            {
                return CleanPinterestUrl(url);
            }

            // 3. Instagram URL temizleme (dlrf, igsh, utm parametrelerini ayıkla)
            if (url.Contains("instagram.com/", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(url);
                    string path = uri.AbsolutePath;
                    if (!path.EndsWith("/")) path += "/";
                    return $"https://www.instagram.com{path}";
                }
                catch { }
            }

            return url;
        }

        private static string CleanPinterestUrl(string url)
        {
            var match = Regex.Match(url, @"pinterest\.com/pin/(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return $"https://www.pinterest.com/pin/{match.Groups[1].Value}/";
            }
            return url.Split('?')[0];
        }
    }
}
