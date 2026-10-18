package com.dvv.downloader.service

import android.content.Context
import com.dvv.downloader.models.DownloadTask
import com.dvv.downloader.models.HistoryItem
import com.dvv.downloader.models.TaskStatus
import com.dvv.downloader.repository.DownloadRepository
import com.dvv.downloader.repository.MediaEngine
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.concurrent.ConcurrentHashMap

object DownloadManager {

    private val scope = CoroutineScope(Dispatchers.IO + SupervisorJob())
    private val activeJobs = ConcurrentHashMap<String, Job>()

    fun startDownload(task: DownloadTask, repository: DownloadRepository, context: Context) {
        if (activeJobs.containsKey(task.id)) return

        repository.addTask(task)
        launchJob(task, repository, context)
    }

    fun retryDownload(task: DownloadTask, repository: DownloadRepository, context: Context) {
        activeJobs[task.id]?.cancel()
        MediaEngine.cancelYtDlp(task.id)
        task.status = TaskStatus.KUYRUKTA
        task.progress = 0
        task.speed = "--"
        task.errorMessage = ""
        repository.updateTask(task)

        launchJob(task, repository, context)
    }

    fun cancelDownload(task: DownloadTask, repository: DownloadRepository) {
        task.status = TaskStatus.IPTAL
        task.speed = "İptal Edildi"
        repository.updateTask(task)

        val job = activeJobs.remove(task.id)
        job?.cancel()
        // yt-dlp süreci engelleyici çalışır; coroutine iptali onu durdurmaz
        MediaEngine.cancelYtDlp(task.id)
    }

    fun removeDownload(taskId: String, repository: DownloadRepository) {
        val job = activeJobs.remove(taskId)
        job?.cancel()
        MediaEngine.cancelYtDlp(taskId)
        repository.removeTask(taskId)
    }

    private fun launchJob(task: DownloadTask, repository: DownloadRepository, context: Context) {
        val job = scope.launch {
            try {
                task.status = TaskStatus.INDIRILIYOR
                task.errorMessage = ""
                repository.updateTask(task)

                val dir = context.getExternalFilesDir(null) ?: context.filesDir
                if (!dir.exists()) {
                    dir.mkdirs()
                }

                // ERR-007 Uyumlu Çözünürlük Etiketli Dosya İsimlendirme
                val safeTitle = task.title
                    .replace(Regex("[^a-zA-Z0-9._-]"), "_")
                    .trim('_')
                    .ifBlank { "Media" }
                    .take(60)

                val resolutionTag = if (task.isAudioOnly) {
                    "Ses"
                } else {
                    task.resolution.ifBlank { task.qualityLabel }.replace(Regex("[^a-zA-Z0-9]"), "")
                }

                val baseName = "${safeTitle} [${resolutionTag}]"
                val onProgress: (Int, String) -> Unit = { percent, speed ->
                    task.progress = percent
                    task.speed = speed
                    repository.updateTask(task)
                }

                val targetFile: File
                val success: Boolean
                if (task.formatSelector.isNotEmpty()) {
                    // yt-dlp: sayfa URL'sinden indirir, ses ve görüntüyü FFmpeg ile birleştirir
                    targetFile = MediaEngine.downloadWithYtDlp(task, dir, baseName, onProgress)
                    task.localFilePath = targetFile.absolutePath
                    success = true
                } else {
                    targetFile = File(dir, "$baseName.${task.ext}")
                    task.localFilePath = targetFile.absolutePath
                    success = MediaEngine.downloadFile(
                        sourceUrl = task.url,
                        targetFile = targetFile,
                        onProgress = onProgress,
                        isCancelled = { task.status == TaskStatus.IPTAL }
                    )
                }

                if (success && task.status != TaskStatus.IPTAL) {
                    task.status = TaskStatus.TAMAMLANDI
                    task.progress = 100
                    task.speed = "Tamamlandı"
                    repository.updateTask(task)

                    val fileSizeFormatted = String.format(
                        Locale.getDefault(),
                        "%.1f MB",
                        targetFile.length() / (1024.0 * 1024.0)
                    )

                    val dateFormat = SimpleDateFormat("dd.MM.yyyy HH:mm", Locale.getDefault())
                    val dateStr = dateFormat.format(Date())

                    repository.addHistory(
                        HistoryItem(
                            id = task.id,
                            title = "${task.title} [${resolutionTag}]",
                            platform = task.platform,
                            filePath = targetFile.absolutePath,
                            fileSizeFormatted = fileSizeFormatted,
                            dateFormatted = dateStr
                        )
                    )
                }
            } catch (e: Exception) {
                if (task.status != TaskStatus.IPTAL) {
                    task.status = TaskStatus.HATA
                    task.errorMessage = e.message ?: "Bilinmeyen indirme hatası"
                    task.speed = "Hata"
                    repository.updateTask(task)
                }
            } finally {
                activeJobs.remove(task.id)
            }
        }

        activeJobs[task.id] = job
    }
}
