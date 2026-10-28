package com.dvv.downloader

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updateLayoutParams
import androidx.core.view.updatePadding
import androidx.fragment.app.Fragment
import com.dvv.downloader.databinding.ActivityMainBinding
import com.dvv.downloader.repository.DownloadRepository
import com.dvv.downloader.ui.CompletedFragment
import com.dvv.downloader.ui.HomeFragment
import com.dvv.downloader.ui.QueueFragment
import com.dvv.downloader.ui.SettingsFragment

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    val repository: DownloadRepository by lazy { DownloadRepository(applicationContext) }

    private val homeFragment = HomeFragment()
    private val queueFragment = QueueFragment()
    private val completedFragment = CompletedFragment()
    private val settingsFragment = SettingsFragment()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        // Android 15 uygulamayı durum çubuğunun altına kadar çizer; üst çubuk saat/simgelerle çakışmasın
        val baseTopBarHeight = binding.topBar.layoutParams.height
        ViewCompat.setOnApplyWindowInsetsListener(binding.topBar) { view, insets ->
            val statusBarTop = insets.getInsets(WindowInsetsCompat.Type.statusBars()).top
            view.updateLayoutParams { height = baseTopBarHeight + statusBarTop }
            view.updatePadding(top = statusBarTop)
            insets
        }

        setupNavigation()

        // Android 8–9'da İndirilenler klasörüne yazmak için depolama izni gerekir (10+ için gerekmez)
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.Q &&
            ContextCompat.checkSelfPermission(this, Manifest.permission.WRITE_EXTERNAL_STORAGE) != PackageManager.PERMISSION_GRANTED
        ) {
            ActivityCompat.requestPermissions(this, arrayOf(Manifest.permission.WRITE_EXTERNAL_STORAGE), 1)
        }

        if (savedInstanceState == null) {
            loadFragment(homeFragment)
            handleIntent(intent)
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        handleIntent(intent)
    }

    private fun handleIntent(intent: Intent?) {
        if (intent?.action == Intent.ACTION_SEND && intent.type == "text/plain") {
            val sharedText = intent.getStringExtra(Intent.EXTRA_TEXT)
            if (!sharedText.isNullOrBlank()) {
                switchToTab(R.id.nav_home)
                homeFragment.setInitialUrl(sharedText)
            }
        }
    }

    private fun setupNavigation() {
        binding.bottomNav.setOnItemSelectedListener { item ->
            when (item.itemId) {
                R.id.nav_home -> loadFragment(homeFragment)
                R.id.nav_queue -> loadFragment(queueFragment)
                R.id.nav_completed -> loadFragment(completedFragment)
                R.id.nav_settings -> loadFragment(settingsFragment)
                else -> false
            }
            true
        }
    }

    fun switchToTab(menuItemId: Int) {
        binding.bottomNav.selectedItemId = menuItemId
    }

    private fun loadFragment(fragment: Fragment) {
        supportFragmentManager.beginTransaction()
            .replace(R.id.fragmentContainer, fragment)
            .commit()
    }
}
