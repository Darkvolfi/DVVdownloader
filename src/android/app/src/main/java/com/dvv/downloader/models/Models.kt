package com.dvv.downloader.models

data class MediaFormatItem(
    val formatId: String,
    val qualityLabel: String,
    val resolution: String,
    val ext: String,
    val formattedSize: String,
    val isAudioOnly: Boolean = false,
    val directUrl: String = "",
    // Boş değilse indirme gömülü yt-dlp ile sayfa URL'si üzerinden bu -f seçicisiyle yapılır
    val formatSelector: String = ""
)

data class MediaMetadata(
    val id: String,
    val title: String,
    val uploader: String,
    val duration: String,
    val thumbnailUrl: String,
    val webpageUrl: String,
    val platform: String,
    val formats: List<MediaFormatItem>
)

enum class TaskStatus {
    KUYRUKTA, INDIRILIYOR, TAMAMLANDI, HATA, IPTAL
}

data class DownloadTask(
    val id: String,
    val url: String,
    val title: String,
    val platform: String,
    val thumbnailUrl: String,
    val qualityLabel: String,
    var progress: Int = 0,
    var speed: String = "--",
    var status: TaskStatus = TaskStatus.KUYRUKTA,
    var localFilePath: String = "",
    var errorMessage: String = "",
    var resolution: String = "",
    var ext: String = "mp4",
    var isAudioOnly: Boolean = false,
    var formatSelector: String = ""
) {
    val canCancel: Boolean
        get() = status == TaskStatus.KUYRUKTA || status == TaskStatus.INDIRILIYOR

    val canRetry: Boolean
        get() = status == TaskStatus.HATA || status == TaskStatus.IPTAL

    val canRemove: Boolean
        get() = status == TaskStatus.TAMAMLANDI || status == TaskStatus.HATA || status == TaskStatus.IPTAL
}

data class HistoryItem(
    val id: String,
    val title: String,
    val platform: String,
    val filePath: String,
    val fileSizeFormatted: String,
    val dateFormatted: String
)

