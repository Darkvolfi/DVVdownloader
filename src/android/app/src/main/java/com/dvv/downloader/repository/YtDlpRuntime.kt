package com.dvv.downloader.repository

import android.content.Context
import com.yausername.ffmpeg.FFmpeg
import com.yausername.youtubedl_android.YoutubeDL
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Deferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.async
import kotlinx.coroutines.withTimeoutOrNull

/**
 * Gömülü yt-dlp (Python) + FFmpeg çalışma ortamı.
 * Uygulama açılışında arka planda hazırlanır ve yt-dlp en son kararlı sürüme güncellenir.
 */
object YtDlpRuntime {

    // İlk açılışta güncelleme bitmediyse analiz en fazla bu kadar bekler, sonra mevcut sürümle devam eder
    private const val UPDATE_WAIT_MS = 60_000L

    private val scope = CoroutineScope(Dispatchers.IO + SupervisorJob())
    private var initJob: Deferred<Unit>? = null
    private var updateJob: Deferred<Unit>? = null

    @Synchronized
    fun start(context: Context) {
        if (initJob != null) return
        val app = context.applicationContext

        val init = scope.async {
            YoutubeDL.getInstance().init(app)
            FFmpeg.getInstance().init(app)
        }
        initJob = init

        // Paketle gelen yt-dlp eskiyebilir; YouTube değişikliklerine uyum için açılışta güncellenir
        updateJob = scope.async {
            init.await()
            try {
                YoutubeDL.getInstance().updateYoutubeDL(app, YoutubeDL.UpdateChannel.STABLE)
            } catch (_: Exception) {}
        }
    }

    suspend fun awaitReady() {
        val init = initJob ?: throw IllegalStateException("İndirme motoru başlatılmadı.")
        try {
            init.await()
        } catch (e: Exception) {
            throw IllegalStateException("İndirme motoru (yt-dlp) başlatılamadı: ${e.message}")
        }
        withTimeoutOrNull(UPDATE_WAIT_MS) { updateJob?.await() }
    }

    fun version(context: Context): String =
        YoutubeDL.getInstance().versionName(context.applicationContext) ?: "?"
}
