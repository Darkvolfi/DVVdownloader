package com.dvv.downloader

import android.app.Application
import com.dvv.downloader.repository.YtDlpRuntime

class DvvApp : Application() {

    override fun onCreate() {
        super.onCreate()
        YtDlpRuntime.start(this)
    }
}
