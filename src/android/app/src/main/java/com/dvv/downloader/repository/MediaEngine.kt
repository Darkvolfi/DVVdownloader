package com.dvv.downloader.repository

import com.dvv.downloader.models.DownloadTask
import com.dvv.downloader.models.MediaFormatItem
import com.dvv.downloader.models.MediaMetadata
import com.yausername.youtubedl_android.YoutubeDL
import com.yausername.youtubedl_android.YoutubeDLRequest
import com.yausername.youtubedl_android.mapper.VideoFormat
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import org.json.JSONObject
import java.io.File
import java.io.FileOutputStream
import java.net.URI
import java.util.concurrent.TimeUnit
import java.util.regex.Pattern

object MediaEngine {

    private const val USER_AGENT =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36"

    private val client = OkHttpClient.Builder()
        .connectTimeout(25, TimeUnit.SECONDS)
        .readTimeout(120, TimeUnit.SECONDS)
        .followRedirects(true)
        .build()

    private val YOUTUBE_ID_REGEX = Pattern.compile(
        "(?:v=|youtu\\.be/|shorts/|embed/|live/)([a-zA-Z0-9_-]{11})",
        Pattern.CASE_INSENSITIVE
    )

    fun detectPlatform(url: String): String {
        return try {
            val host = URI(url).host?.lowercase() ?: return "Bilinmiyor"
            when {
                host.contains("youtube.com") || host.contains("youtu.be") -> "YouTube"
                host.contains("tiktok.com") -> "TikTok"
                host.contains("instagram.com") -> "Instagram"
                host.contains("twitter.com") || host.contains("x.com") -> "Twitter/X"
                host.contains("pinterest.com") || host.contains("pin.it") -> "Pinterest"
                host.contains("facebook.com") || host.contains("fb.watch") -> "Facebook"
                host.contains("twitch.tv") -> "Twitch"
                url.endsWith(".mp4", true) || url.endsWith(".mkv", true) ||
                        url.endsWith(".webm", true) || url.endsWith(".mp3", true) ||
                        url.endsWith(".m4a", true) -> "Doğrudan Medya"
                else -> "Web Medyası"
            }
        } catch (_: Exception) {
            "Geçersiz URL"
        }
    }

    suspend fun extractMetadata(url: String): MediaMetadata = withContext(Dispatchers.IO) {
        val platform = detectPlatform(url)

        when (platform) {
            "YouTube" -> extractYouTube(url)
            "TikTok" -> extractTikTok(url)
            "Instagram" -> extractInstagram(url)
            "Pinterest" -> extractPinterest(url)
            "Doğrudan Medya" -> extractDirectMedia(url)
            else -> extractSocialOrWeb(url, platform)
        }
    }

    private suspend fun extractYouTube(url: String): MediaMetadata {
        if (!YOUTUBE_ID_REGEX.matcher(url).find()) {
            throw IllegalArgumentException("Geçersiz YouTube bağlantısı veya video kimliği bulunamadı.")
        }
        return extractViaYtDlp(url, "YouTube")
    }

    private fun isNoCodec(codec: String?): Boolean = codec.isNullOrBlank() || codec == "none"

    private fun formatBytes(f: VideoFormat?): Long =
        f?.let { if (it.fileSize > 0) it.fileSize else it.fileSizeApproximate } ?: 0L

    // Windows sürümüyle aynı seçim: istenen yüksekliğe kadar en iyi görüntü + en iyi ses, yoksa birleşik akış
    private fun videoSelector(height: Int) = "bv*[height<=$height]+ba/b[height<=$height]/b"

    private suspend fun extractViaYtDlp(url: String, platform: String): MediaMetadata {
        YtDlpRuntime.awaitReady()

        val request = YoutubeDLRequest(url).apply {
            addOption("--no-playlist")
            addOption("--socket-timeout", 20)
        }
        val info = try {
            YoutubeDL.getInstance().getInfo(request)
        } catch (e: Exception) {
            throw IllegalStateException(cleanYtDlpError(e))
        }

        val all = info.formats ?: arrayListOf()
        val bestAudioBytes = all.filter { isNoCodec(it.vcodec) && !isNoCodec(it.acodec) }
            .maxOfOrNull { formatBytes(it) } ?: 0L
        val videoByHeight = all.filter { !isNoCodec(it.vcodec) && it.height > 0 }.groupBy { it.height }

        val formats = mutableListOf<MediaFormatItem>()
        for (height in videoByHeight.keys.sortedDescending()) {
            val group = videoByHeight.getValue(height)
            val best = group.maxByOrNull { formatBytes(it) }
            val bytes = formatBytes(best) + if (isNoCodec(best?.acodec)) bestAudioBytes else 0L
            // Dikey videolarda (ör. 1080x1920) etiket kısa kenara göre verilir
            val width = group.maxOf { it.width }
            val label = if (width in 1 until height) width else height
            val fps = group.maxOf { it.fps }
            formats.add(
                MediaFormatItem(
                    formatId = "ytdlp_${height}",
                    qualityLabel = if (fps > 30) "${label}p ${fps}fps" else "${label}p",
                    resolution = "${label}p",
                    ext = "mp4",
                    formattedSize = if (bytes > 0) formatFileSize(bytes) else "Bilinmiyor",
                    formatSelector = videoSelector(height)
                )
            )
        }

        if (formats.isEmpty()) {
            formats.add(
                MediaFormatItem(
                    formatId = "ytdlp_best",
                    qualityLabel = "En İyi Kalite",
                    resolution = "Orijinal",
                    ext = "mp4",
                    formattedSize = "Bilinmiyor",
                    formatSelector = "bv*+ba/b"
                )
            )
        }

        formats.add(
            MediaFormatItem(
                formatId = "ytdlp_audio",
                qualityLabel = "MP3 Ses",
                resolution = "Audio",
                ext = "mp3",
                formattedSize = if (bestAudioBytes > 0) formatFileSize(bestAudioBytes) else "Ses Dosyası",
                isAudioOnly = true,
                formatSelector = "ba/b"
            )
        )

        val durationSec = info.duration.toLong()
        return MediaMetadata(
            id = info.id ?: System.currentTimeMillis().toString(),
            title = info.title?.ifBlank { null } ?: "$platform Medya",
            uploader = info.uploader ?: platform,
            duration = if (durationSec > 0) formatDuration(durationSec) else "--:--",
            thumbnailUrl = info.thumbnail ?: "",
            webpageUrl = info.webpageUrl?.ifBlank { null } ?: url,
            platform = platform,
            formats = formats
        )
    }

    /** yt-dlp hata çıktısından kullanıcıya gösterilecek ERROR satırını ayıklar. */
    private fun cleanYtDlpError(e: Exception): String {
        val text = e.message ?: return "Medya bilgileri alınamadı."
        val errorLine = text.lines().lastOrNull { it.contains("ERROR:") }
        return (errorLine ?: text.lines().firstOrNull { it.isNotBlank() } ?: text)
            .substringAfter("ERROR: ")
            .take(300)
    }

    suspend fun downloadWithYtDlp(
        task: DownloadTask,
        dir: File,
        baseName: String,
        onProgress: (percent: Int, speed: String) -> Unit
    ): File = withContext(Dispatchers.IO) {
        YtDlpRuntime.awaitReady()

        val request = YoutubeDLRequest(task.url).apply {
            addOption("--no-playlist")
            addOption("--no-mtime")
            addOption("--force-overwrites")
            addOption("--socket-timeout", 20)
            addOption("-o", File(dir, "$baseName.%(ext)s").absolutePath)
            addOption("-f", task.formatSelector)
            if (task.isAudioOnly) {
                addOption("-x")
                addOption("--audio-format", "mp3")
                addOption("--audio-quality", 0)
            } else {
                // Tüm Android oynatıcılarıyla uyum için H.264 + M4A tercih edilir
                addOption("-S", "res,fps,vcodec:h264,acodec:m4a")
                addOption("--merge-output-format", "mp4")
            }
        }

        val speedRegex = Regex("""at\s+(\S+/s)""")
        try {
            YoutubeDL.getInstance().execute(request, task.id) { progress, _, line ->
                if (progress >= 0f) {
                    val speed = speedRegex.find(line)?.groupValues?.get(1) ?: ""
                    onProgress(progress.toInt().coerceIn(0, 99), speed)
                }
            }
        } catch (e: YoutubeDL.CanceledException) {
            // İptalde yt-dlp yarım .part/.ytdl dosyalarını bırakır; telefonda yer kaplamasın
            dir.listFiles { f -> f.name.startsWith("$baseName.") }?.forEach { it.delete() }
            throw e
        } catch (e: Exception) {
            throw IllegalStateException(cleanYtDlpError(e))
        }

        val expected = File(dir, "$baseName.${if (task.isAudioOnly) "mp3" else "mp4"}")
        if (expected.exists()) return@withContext expected

        dir.listFiles { f -> f.name.startsWith("$baseName.") && !f.name.endsWith(".part") && !f.name.endsWith(".ytdl") }
            ?.maxByOrNull { it.lastModified() }
            ?: throw IllegalStateException("İndirme tamamlandı ancak dosya bulunamadı.")
    }

    fun cancelYtDlp(taskId: String) {
        try {
            YoutubeDL.getInstance().destroyProcessById(taskId)
        } catch (_: Exception) {}
    }

    private fun extractTikTok(url: String): MediaMetadata {
        val apiUrl = "https://www.tikwm.com/api/?url=$url"
        val request = Request.Builder()
            .url(apiUrl)
            .header("User-Agent", USER_AGENT)
            .build()

        val response = client.newCall(request).execute()
        if (!response.isSuccessful) {
            throw IllegalStateException("TikTok sunucusuna bağlanılamadı: HTTP ${response.code}")
        }

        val json = JSONObject(response.body?.string() ?: "")
        val code = json.optInt("code", -1)
        val data = json.optJSONObject("data")
            ?: throw IllegalStateException("TikTok videosu bulunamadı veya gizli.")

        if (code != 0) {
            throw IllegalStateException(json.optString("msg", "TikTok videosu çözülemedi."))
        }

        val title = data.optString("title", "TikTok Video").ifBlank { "TikTok Video Paylaşımı" }
        val author = data.optJSONObject("author")?.optString("nickname", "TikTok") ?: "TikTok"
        val cover = data.optString("cover", "")
        val durationSec = data.optLong("duration", 0L)
        val duration = if (durationSec > 0) formatDuration(durationSec) else "--:--"

        val playUrl = data.optString("play", "")
        val musicUrl = data.optString("music", "")
        val sizeBytes = data.optLong("size", 0L)
        val formattedSize = if (sizeBytes > 0) formatFileSize(sizeBytes) else "Orijinal Boyut"

        val formats = mutableListOf<MediaFormatItem>()
        if (playUrl.isNotEmpty()) {
            formats.add(
                MediaFormatItem(
                    formatId = "tiktok_hd",
                    qualityLabel = "HD Video (Filigransız)",
                    resolution = "1080p",
                    ext = "mp4",
                    formattedSize = formattedSize,
                    isAudioOnly = false,
                    directUrl = playUrl
                )
            )
        }

        if (musicUrl.isNotEmpty()) {
            formats.add(
                MediaFormatItem(
                    formatId = "tiktok_audio",
                    qualityLabel = "Orijinal Ses (MP3)",
                    resolution = "Audio",
                    ext = "mp3",
                    formattedSize = "Ses Dosyası",
                    isAudioOnly = true,
                    directUrl = musicUrl
                )
            )
        }

        return MediaMetadata(
            id = data.optString("id", System.currentTimeMillis().toString()),
            title = title,
            uploader = author,
            duration = duration,
            thumbnailUrl = cover,
            webpageUrl = url,
            platform = "TikTok",
            formats = formats
        )
    }

    private fun extractDirectMedia(url: String): MediaMetadata {
        val fileName = url.substringAfterLast("/").substringBefore("?")
        val isAudio = url.endsWith(".mp3", true) || url.endsWith(".m4a", true)

        var sizeFormatted = "Bilinmiyor"
        try {
            val headReq = Request.Builder()
                .url(url)
                .head()
                .header("User-Agent", USER_AGENT)
                .build()
            val resp = client.newCall(headReq).execute()
            val cl = resp.header("Content-Length")?.toLongOrNull() ?: 0L
            if (cl > 0) {
                sizeFormatted = formatFileSize(cl)
            }
        } catch (_: Exception) {}

        val ext = if (isAudio) "mp3" else "mp4"
        val formats = listOf(
            MediaFormatItem(
                formatId = "direct",
                qualityLabel = if (isAudio) "Doğrudan Ses Akışı" else "Doğrudan Video Akışı",
                resolution = if (isAudio) "Audio" else "Kaynak",
                ext = ext,
                formattedSize = sizeFormatted,
                isAudioOnly = isAudio,
                directUrl = url
            )
        )

        return MediaMetadata(
            id = System.currentTimeMillis().toString(),
            title = fileName.ifBlank { "Medya Dosyası" },
            uploader = "Doğrudan İndirme",
            duration = "--:--",
            thumbnailUrl = "",
            webpageUrl = url,
            platform = "Doğrudan Medya",
            formats = formats
        )
    }

    fun rewriteCdnHostIfNeeded(url: String): String {
        if (url.isBlank()) return url
        if (url.contains(".fna.fbcdn.net", ignoreCase = true)) {
            return try {
                val uri = URI(url)
                URI(uri.scheme, "scontent.cdninstagram.com", uri.path, uri.query, uri.fragment).toString()
            } catch (_: Exception) {
                url
            }
        }
        return url
    }

    private fun unshortenUrlIfNeeded(url: String): String {
        if (!url.contains("pin.it", ignoreCase = true)) return url
        return try {
            val unshortReq = Request.Builder()
                .url("https://unshorten.me/json/${java.net.URLEncoder.encode(url, "UTF-8")}")
                .header("User-Agent", USER_AGENT)
                .build()
            val resp = client.newCall(unshortReq).execute()
            if (resp.isSuccessful) {
                val json = JSONObject(resp.body?.string() ?: "")
                val resolved = json.optString("resolved_url", "")
                if (resolved.isNotBlank()) resolved else url
            } else url
        } catch (_: Exception) {
            url
        }
    }

    /** pin.it kısa bağlantısını yönlendirmeleri izleyerek çözer; olmazsa unshorten.me'ye düşer. */
    private fun resolvePinterestUrl(url: String): String {
        if (!url.contains("pin.it", ignoreCase = true)) return url
        try {
            val req = Request.Builder().url(url).header("User-Agent", USER_AGENT).build()
            client.newCall(req).execute().use { resp ->
                val finalUrl = resp.request.url.toString()
                if (finalUrl.contains("pinterest.", ignoreCase = true) && finalUrl.contains("/pin/", ignoreCase = true)) {
                    return finalUrl
                }
            }
        } catch (_: Exception) {}
        return unshortenUrlIfNeeded(url)
    }

    private suspend fun extractPinterest(url: String): MediaMetadata {
        val targetUrl = resolvePinterestUrl(url)
        val pinMatcher = Pattern.compile("pinterest\\.[a-z.]+/pin/(\\d+)", Pattern.CASE_INSENSITIVE).matcher(targetUrl)
        val cleanUrl = if (pinMatcher.find()) "https://www.pinterest.com/pin/${pinMatcher.group(1)}/" else targetUrl

        return try {
            extractViaYtDlp(cleanUrl, "Pinterest")
        } catch (e: Exception) {
            // yt-dlp çözemezse sayfa HTML'inden doğrudan MP4 aranır
            try {
                extractPinterestFromHtml(cleanUrl)
            } catch (_: Exception) {
                throw e
            }
        }
    }

    private fun extractPinterestFromHtml(url: String): MediaMetadata {
        val targetUrl = unshortenUrlIfNeeded(url)
        val pinMatcher = Pattern.compile("pinterest\\.com/pin/(\\d+)", Pattern.CASE_INSENSITIVE).matcher(targetUrl)
        val pinId = if (pinMatcher.find()) pinMatcher.group(1) else ""
        val fetchUrl = if (pinId.isNotBlank()) "https://tr.pinterest.com/pin/$pinId/" else targetUrl

        val req = Request.Builder()
            .url(fetchUrl)
            .header("User-Agent", USER_AGENT)
            .header("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8")
            .header("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7")
            .header("Referer", "https://www.pinterest.com/")
            .build()

        val resp = client.newCall(req).execute()
        if (!resp.isSuccessful) {
            throw IllegalStateException("Pinterest sayfasına erişilemedi: HTTP ${resp.code}")
        }

        val html = resp.body?.string() ?: ""

        val titleMatcher = Pattern.compile("<title>([^<]+)</title>", Pattern.CASE_INSENSITIVE).matcher(html)
        val title = if (titleMatcher.find()) {
            titleMatcher.group(1)?.replace(" | Pinterest", "")?.trim() ?: "Pinterest Video"
        } else "Pinterest Video"

        var thumb = ""
        val thumbMatcher = Pattern.compile("\"thumbnail\"\\s*:\\s*\"([^\"]+)\"").matcher(html)
        if (thumbMatcher.find()) {
            thumb = thumbMatcher.group(1)?.replace("\\/", "/") ?: ""
        } else {
            val ogThumb = Pattern.compile("property=[\"']og:image[\"']\\s+content=[\"']([^\"']+)[\"']", Pattern.CASE_INSENSITIVE).matcher(html)
            if (ogThumb.find()) thumb = ogThumb.group(1) ?: ""
        }

        var directMp4 = ""
        val candidates = mutableListOf<String>()
        val mp4Matcher = Pattern.compile("\"url\"\\s*:\\s*\"(https?:[^\"]+\\.mp4[^\"]*)\"").matcher(html)
        while (mp4Matcher.find()) {
            val candidate = mp4Matcher.group(1)?.replace("\\/", "/") ?: ""
            if (candidate.isNotBlank() && candidate.contains(".mp4", ignoreCase = true)) {
                if (candidate.contains("_audio", ignoreCase = true) || candidate.contains("audio.", ignoreCase = true)) {
                    continue
                }
                candidates.add(candidate)
            }
        }

        if (candidates.isEmpty()) {
            val genMatcher = Pattern.compile("(https?://[^\"'\\s<>]+\\.mp4[^\"'\\s<>]*)", Pattern.CASE_INSENSITIVE).matcher(html)
            while (genMatcher.find()) {
                val candidate = genMatcher.group(1)?.replace("\\/", "/") ?: ""
                if (candidate.isNotBlank() && candidate.contains(".mp4", ignoreCase = true)) {
                    if (candidate.contains("_audio", ignoreCase = true) || candidate.contains("audio.", ignoreCase = true)) {
                        continue
                    }
                    candidates.add(candidate)
                }
            }
        }

        directMp4 = candidates.firstOrNull { it.contains("720p", ignoreCase = true) || it.contains("720w", ignoreCase = true) }
            ?: candidates.firstOrNull { it.contains("expMp4", ignoreCase = true) && !it.contains("_t1", ignoreCase = true) }
            ?: candidates.firstOrNull() ?: ""

        if (directMp4.isBlank()) {
            throw IllegalStateException("Pinterest gönderisinde video bulunamadı veya gönderi sadece resim.")
        }

        return MediaMetadata(
            id = pinId.ifBlank { System.currentTimeMillis().toString() },
            title = title,
            uploader = "Pinterest",
            duration = "--:--",
            thumbnailUrl = thumb,
            webpageUrl = targetUrl,
            platform = "Pinterest",
            formats = listOf(
                MediaFormatItem(
                    formatId = "pinterest_720p",
                    qualityLabel = "720p HD (Orijinal)",
                    resolution = "720p",
                    ext = "mp4",
                    formattedSize = "Orijinal Kalite",
                    isAudioOnly = false,
                    directUrl = directMp4
                )
            )
        )
    }

    private fun extractInstagram(url: String): MediaMetadata {
        var cleanUrl = url.split("?")[0]
        if (!cleanUrl.endsWith("/")) cleanUrl += "/"
        val embedUrl = "${cleanUrl}embed/captioned/"

        val req = Request.Builder()
            .url(embedUrl)
            .header("User-Agent", USER_AGENT)
            .header("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
            .header("Accept-Language", "en-US,en;q=0.9,tr;q=0.8")
            .header("Referer", "https://www.instagram.com/")
            .build()

        val resp = client.newCall(req).execute()
        if (!resp.isSuccessful) {
            throw IllegalStateException("Instagram sayfasına erişilemedi: HTTP ${resp.code}")
        }

        val html = resp.body?.string() ?: ""

        var videoUrl = ""
        val vMatcher = Pattern.compile("\"video_url\"\\s*:\\s*\"([^\"]+)\"").matcher(html)
        if (vMatcher.find()) {
            videoUrl = vMatcher.group(1) ?: ""
        } else {
            val rawMatcher = Pattern.compile("https?://[^\\s\"'<>]+\\.mp4[^\\s\"'<>]*", Pattern.CASE_INSENSITIVE).matcher(html)
            if (rawMatcher.find()) {
                videoUrl = rawMatcher.group(0) ?: ""
            }
        }

        if (videoUrl.isBlank()) {
            throw IllegalStateException("Instagram videosu bulunamadı veya gönderi gizli.")
        }

        videoUrl = videoUrl.replace("\\u0026", "&").replace("\\/", "/")
        videoUrl = rewriteCdnHostIfNeeded(videoUrl)

        val captionMatcher = Pattern.compile("class=[\"']Caption[\"'][^>]*>([^<]+)<", Pattern.CASE_INSENSITIVE).matcher(html)
        val title = if (captionMatcher.find()) {
            captionMatcher.group(1)?.trim() ?: "Instagram Video"
        } else {
            val tMatcher = Pattern.compile("<title>([^<]+)</title>", Pattern.CASE_INSENSITIVE).matcher(html)
            if (tMatcher.find()) tMatcher.group(1)?.replace("Instagram:", "")?.trim() ?: "Instagram Video" else "Instagram Video"
        }

        var thumb = ""
        val thumbMatcher = Pattern.compile("\"display_url\"\\s*:\\s*\"([^\"]+)\"").matcher(html)
        if (thumbMatcher.find()) {
            val g1 = thumbMatcher.group(1)
            if (g1 != null) {
                thumb = rewriteCdnHostIfNeeded(g1.replace("\\u0026", "&").replace("\\/", "/"))
            }
        }

        val id = cleanUrl.substringAfterLast("/p/").substringAfterLast("/reel/").substringBefore("/").ifBlank { System.currentTimeMillis().toString() }

        return MediaMetadata(
            id = id,
            title = title,
            uploader = "Instagram",
            duration = "--:--",
            thumbnailUrl = thumb,
            webpageUrl = cleanUrl,
            platform = "Instagram",
            formats = listOf(
                MediaFormatItem(
                    formatId = "instagram_hd",
                    qualityLabel = "1080p HD (Orijinal MP4)",
                    resolution = "1080p",
                    ext = "mp4",
                    formattedSize = "Orijinal Boyut",
                    isAudioOnly = false,
                    directUrl = videoUrl
                )
            )
        )
    }

    private fun extractSocialOrWeb(url: String, platform: String): MediaMetadata {
        val request = Request.Builder()
            .url(url)
            .header("User-Agent", USER_AGENT)
            .header("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
            .build()

        val response = client.newCall(request).execute()
        if (!response.isSuccessful) {
            throw IllegalStateException("$platform sayfasına ulaşılamadı: HTTP ${response.code}")
        }

        val html = response.body?.string() ?: ""

        // OpenGraph ve Twitter video etiketlerini ayıkla
        val ogVideoPattern = Pattern.compile("<meta\\s+(?:property|name)=[\"'](?:og:video|og:video:secure_url|twitter:player:stream)[\"']\\s+content=[\"']([^\"']+)[\"']", Pattern.CASE_INSENSITIVE)
        val matcher = ogVideoPattern.matcher(html)
        var streamUrl = if (matcher.find()) matcher.group(1) else ""

        // HTML5 <video src="..."> fallback
        if (streamUrl.isEmpty()) {
            val videoSrcPattern = Pattern.compile("<video[^>]+src=[\"']([^\"']+)[\"']", Pattern.CASE_INSENSITIVE)
            val vMatcher = videoSrcPattern.matcher(html)
            if (vMatcher.find()) {
                streamUrl = vMatcher.group(1)
            }
        }

        // Başlık bul
        val titlePattern = Pattern.compile("<title>([^<]+)</title>", Pattern.CASE_INSENSITIVE)
        val tMatcher = titlePattern.matcher(html)
        val pageTitle = if (tMatcher.find()) tMatcher.group(1)?.trim() ?: "$platform Medya" else "$platform Medya"

        if (streamUrl.isEmpty()) {
            throw IllegalStateException("$platform sayfasında indirilebilir açık medya akışı bulunamadı. Lütfen doğrudan medya URL'si girin veya YouTube/TikTok bağlantısı deneyin.")
        }

        val formats = listOf(
            MediaFormatItem(
                formatId = "web_video",
                qualityLabel = "En Yüksek Kalite",
                resolution = "Orijinal",
                ext = "mp4",
                formattedSize = "Kaynak Akış",
                isAudioOnly = false,
                directUrl = streamUrl
            )
        )

        return MediaMetadata(
            id = System.currentTimeMillis().toString(),
            title = pageTitle,
            uploader = platform,
            duration = "--:--",
            thumbnailUrl = "",
            webpageUrl = url,
            platform = platform,
            formats = formats
        )
    }

    suspend fun downloadFile(
        sourceUrl: String,
        targetFile: File,
        onProgress: (percent: Int, speed: String) -> Unit,
        isCancelled: () -> Boolean
    ): Boolean = withContext(Dispatchers.IO) {
        val effectiveUrl = rewriteCdnHostIfNeeded(sourceUrl)
        val reqBuilder = Request.Builder()
            .url(effectiveUrl)
            .header("User-Agent", USER_AGENT)
            .header("Accept", "*/*")
            .header("Connection", "keep-alive")

        if (effectiveUrl.contains("instagram.com", ignoreCase = true) || effectiveUrl.contains("cdninstagram.com", ignoreCase = true)) {
            reqBuilder.header("Referer", "https://www.instagram.com/")
        } else if (effectiveUrl.contains("pinterest.com", ignoreCase = true) || effectiveUrl.contains("pinimg.com", ignoreCase = true)) {
            reqBuilder.header("Referer", "https://www.pinterest.com/")
        }

        val request = reqBuilder.build()
        val response = client.newCall(request).execute()

        if (!response.isSuccessful) {
            throw IllegalStateException("Medya sunucusu hatası: HTTP ${response.code} (${response.message})")
        }

        val contentType = response.header("Content-Type", "")?.lowercase() ?: ""
        if (contentType.contains("text/html")) {
            throw IllegalStateException("Sunucu medya dosyası yerine web sayfası döndürdü. Bağlantı erişim kısıtlamalı veya süresi dolmuş olabilir.")
        }

        val body = response.body ?: throw IllegalStateException("İndirilecek medya gövdesi boş.")
        val totalLength = body.contentLength()

        var downloadedBytes = 0L
        var lastTime = System.currentTimeMillis()
        var lastBytes = 0L

        body.byteStream().use { input ->
            FileOutputStream(targetFile).use { output ->
                val buffer = ByteArray(64 * 1024)
                var bytesRead: Int

                while (input.read(buffer).also { bytesRead = it } != -1) {
                    if (isCancelled()) {
                        targetFile.delete()
                        return@withContext false
                    }

                    output.write(buffer, 0, bytesRead)
                    downloadedBytes += bytesRead

                    val now = System.currentTimeMillis()
                    if (now - lastTime >= 400) {
                        val timeDiffSec = (now - lastTime) / 1000.0
                        val bytesDiff = downloadedBytes - lastBytes
                        val speedMb = (bytesDiff / (1024.0 * 1024.0)) / timeDiffSec
                        val speedFormatted = if (speedMb >= 1.0) {
                            String.format("%.1f MB/s", speedMb)
                        } else {
                            val speedKb = (bytesDiff / 1024.0) / timeDiffSec
                            String.format("%.0f KB/s", speedKb)
                        }

                        val percent = if (totalLength > 0) {
                            ((downloadedBytes * 100) / totalLength).toInt().coerceIn(0, 99)
                        } else {
                            50
                        }

                        onProgress(percent, speedFormatted)
                        lastTime = now
                        lastBytes = downloadedBytes
                    }
                }
            }
        }

        onProgress(100, "Tamamlandı")
        true
    }

    private fun formatFileSize(bytes: Long): String {
        val mb = bytes / (1024.0 * 1024.0)
        return if (mb >= 1000.0) {
            String.format("%.2f GB", mb / 1024.0)
        } else {
            String.format("%.1f MB", mb)
        }
    }

    private fun formatDuration(seconds: Long): String {
        val min = seconds / 60
        val sec = seconds % 60
        return String.format("%02d:%02d", min, sec)
    }
}
