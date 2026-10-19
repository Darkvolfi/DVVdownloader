package com.dvv.downloader

import android.content.Context
import android.content.res.Configuration
import android.os.LocaleList
import androidx.annotation.StringRes
import androidx.appcompat.app.AppCompatDelegate

/**
 * Ekrana bağlı olmayan katmanlar (indirme motoru, servis) için yerelleştirilmiş metin sağlar.
 * Android 13 öncesinde AppCompat'ın uygulama içi dil seçimi yalnızca Activity bağlamına uygulandığından,
 * seçili dil burada uygulama bağlamına elle uygulanır.
 */
object L10n {

    private lateinit var app: Context

    fun init(context: Context) {
        app = context.applicationContext
    }

    private fun localizedContext(): Context {
        val selected = AppCompatDelegate.getApplicationLocales()
        if (selected.isEmpty) return app
        val config = Configuration(app.resources.configuration)
        config.setLocales(LocaleList.forLanguageTags(selected.toLanguageTags()))
        return app.createConfigurationContext(config)
    }

    fun s(@StringRes id: Int, vararg args: Any): String = localizedContext().getString(id, *args)
}
