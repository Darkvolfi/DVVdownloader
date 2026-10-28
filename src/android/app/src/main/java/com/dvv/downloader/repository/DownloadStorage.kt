package com.dvv.downloader.repository

import android.content.ContentValues
import android.content.Context
import android.content.Intent
import android.media.MediaScannerConnection
import android.net.Uri
import android.os.Build
import android.os.Environment
import android.provider.DocumentsContract
import android.provider.MediaStore
import android.webkit.MimeTypeMap
import androidx.documentfile.provider.DocumentFile
import java.io.File

/**
 * İndirilen dosyaların kalıcı konumu.
 * Varsayılan: herkese açık İndirilenler/DVVdownloader (galeride ve dosya yöneticisinde görünür).
 * Kullanıcı Ayarlar'dan başka bir klasör seçebilir (Storage Access Framework).
 * yt-dlp gerçek bir dosya yolu istediği için indirme önce uygulama klasöründe yapılır, sonra buraya taşınır.
 */
object DownloadStorage {

    private const val PREFS = "dvv_storage"
    private const val KEY_TREE_URI = "download_tree_uri"
    const val DEFAULT_FOLDER = "DVVdownloader"
    const val DEFAULT_LABEL = "Download/DVVdownloader"

    fun customFolder(context: Context): Uri? =
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .getString(KEY_TREE_URI, null)?.let(Uri::parse)
            // İzin geri alınmışsa (klasör silinmiş vb.) varsayılana düşülür
            ?.takeIf { uri -> context.contentResolver.persistedUriPermissions.any { it.uri == uri && it.isWritePermission } }

    fun setCustomFolder(context: Context, treeUri: Uri?) {
        val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        prefs.getString(KEY_TREE_URI, null)?.let { old ->
            if (old != treeUri?.toString()) {
                try {
                    context.contentResolver.releasePersistableUriPermission(
                        Uri.parse(old),
                        Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_GRANT_WRITE_URI_PERMISSION
                    )
                } catch (_: Exception) {}
            }
        }
        if (treeUri == null) {
            prefs.edit().remove(KEY_TREE_URI).apply()
        } else {
            context.contentResolver.takePersistableUriPermission(
                treeUri,
                Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_GRANT_WRITE_URI_PERMISSION
            )
            prefs.edit().putString(KEY_TREE_URI, treeUri.toString()).apply()
        }
    }

    /** Ayarlar'da gösterilecek konum, ör. "Download/DVVdownloader" veya "Movies/Videolar". */
    fun locationLabel(context: Context): String {
        val tree = customFolder(context) ?: return DEFAULT_LABEL
        return try {
            DocumentsContract.getTreeDocumentId(tree).substringAfter(':').ifBlank { tree.toString() }
        } catch (_: Exception) {
            tree.toString()
        }
    }

    fun mimeType(file: File): String =
        MimeTypeMap.getSingleton().getMimeTypeFromExtension(file.extension.lowercase())
            ?: if (file.extension.equals("mp3", true)) "audio/mpeg" else "video/mp4"

    /**
     * Geçici dosyayı kalıcı konuma taşır ve yeni konumu döndürür (content:// adresi ya da dosya yolu).
     * Taşıma başarısız olursa dosya yerinde bırakılır ve eski yolu döndürülür; indirilen dosya kaybolmaz.
     */
    fun publish(context: Context, temp: File): String {
        return try {
            val target = customFolder(context)?.let { copyToTree(context, it, temp) }
                ?: if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) copyToMediaStore(context, temp)
                else copyToPublicDownloads(context, temp)
            temp.delete()
            target
        } catch (_: Exception) {
            temp.absolutePath
        }
    }

    private fun copyToTree(context: Context, tree: Uri, temp: File): String {
        val folder = DocumentFile.fromTreeUri(context, tree) ?: error("folder")
        // Uzantı MIME türünden eklendiği için ad uzantısız verilir; aynı ad varsa sistem "(1)" ekler
        val doc = folder.createFile(mimeType(temp), temp.nameWithoutExtension) ?: error("create")
        context.contentResolver.openOutputStream(doc.uri)!!.use { out -> temp.inputStream().use { it.copyTo(out) } }
        return doc.uri.toString()
    }

    private fun copyToMediaStore(context: Context, temp: File): String {
        val resolver = context.contentResolver
        val values = ContentValues().apply {
            put(MediaStore.MediaColumns.DISPLAY_NAME, temp.name)
            put(MediaStore.MediaColumns.MIME_TYPE, mimeType(temp))
            put(MediaStore.MediaColumns.RELATIVE_PATH, "${Environment.DIRECTORY_DOWNLOADS}/$DEFAULT_FOLDER")
            put(MediaStore.MediaColumns.IS_PENDING, 1)
        }
        val uri = resolver.insert(MediaStore.Downloads.EXTERNAL_CONTENT_URI, values) ?: error("insert")
        try {
            resolver.openOutputStream(uri)!!.use { out -> temp.inputStream().use { it.copyTo(out) } }
            resolver.update(uri, ContentValues().apply { put(MediaStore.MediaColumns.IS_PENDING, 0) }, null, null)
        } catch (e: Exception) {
            resolver.delete(uri, null, null)
            throw e
        }
        return uri.toString()
    }

    /** Android 8–9: WRITE_EXTERNAL_STORAGE izniyle doğrudan İndirilenler klasörüne yazılır. */
    @Suppress("DEPRECATION")
    private fun copyToPublicDownloads(context: Context, temp: File): String {
        val dir = File(Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS), DEFAULT_FOLDER)
        if (!dir.exists() && !dir.mkdirs()) error("mkdirs")
        var target = File(dir, temp.name)
        var n = 1
        while (target.exists()) target = File(dir, "${temp.nameWithoutExtension} ($n).${temp.extension}").also { n++ }
        temp.copyTo(target)
        MediaScannerConnection.scanFile(context, arrayOf(target.absolutePath), null, null)
        return target.absolutePath
    }
}
