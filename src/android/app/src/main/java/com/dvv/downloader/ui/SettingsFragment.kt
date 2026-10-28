package com.dvv.downloader.ui

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatDelegate
import androidx.core.os.LocaleListCompat
import androidx.fragment.app.Fragment
import com.dvv.downloader.R
import com.dvv.downloader.databinding.FragmentSettingsBinding
import com.dvv.downloader.repository.DownloadStorage
import com.google.android.material.dialog.MaterialAlertDialogBuilder

class SettingsFragment : Fragment() {

    private var _binding: FragmentSettingsBinding? = null
    private val binding get() = _binding!!

    // Dil etiketi -> kendi dilindeki adı. Boş etiket = sistem dili (cihaz dili desteklenmiyorsa İngilizce).
    private val languages = listOf(
        "" to null,
        "en" to "English",
        "tr" to "Türkçe",
        "de" to "Deutsch",
        "es" to "Español",
        "fr" to "Français",
        "pt" to "Português",
        "ru" to "Русский",
        "ar" to "العربية",
        "id" to "Bahasa Indonesia",
        "hi" to "हिन्दी",
        "ja" to "日本語",
        "zh-CN" to "简体中文"
    )

    // Sistem klasör seçicisi; seçilen klasöre kalıcı yazma izni alınır
    private val pickFolder = registerForActivityResult(ActivityResultContracts.OpenDocumentTree()) { uri ->
        if (uri != null) {
            DownloadStorage.setCustomFolder(requireContext(), uri)
            refreshDownloadLocation()
        }
    }

    private fun refreshDownloadLocation() {
        binding.tvDownloadPath.text = DownloadStorage.locationLabel(requireContext())
        binding.btnResetFolder.visibility =
            if (DownloadStorage.customFolder(requireContext()) != null) View.VISIBLE else View.GONE
    }

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentSettingsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        refreshDownloadLocation()
        binding.btnChangeFolder.setOnClickListener { pickFolder.launch(null) }
        binding.btnResetFolder.setOnClickListener {
            DownloadStorage.setCustomFolder(requireContext(), null)
            refreshDownloadLocation()
        }

        val versionName = requireContext().packageManager
            .getPackageInfo(requireContext().packageName, 0).versionName ?: ""
        binding.tvAppVersion.text = getString(R.string.settings_version, versionName)

        binding.tvLanguage.text = languageName(currentLanguageIndex())
        binding.cardLanguage.setOnClickListener { showLanguageDialog() }

        binding.btnViewLicense.setOnClickListener {
            MaterialAlertDialogBuilder(requireContext())
                .setTitle("GNU General Public License v3.0")
                .setMessage(R.string.license_dialog_message)
                .setPositiveButton(R.string.btn_close, null)
                .show()
        }
    }

    private fun languageName(index: Int): String =
        languages[index].second ?: getString(R.string.language_system)

    private fun currentLanguageIndex(): Int {
        val selected = AppCompatDelegate.getApplicationLocales()
        if (selected.isEmpty) return 0
        val tag = selected[0]?.toLanguageTag() ?: return 0
        // "in" eski Endonezce kodudur; "zh-Hans-CN" gibi etiketler dil+bölge ile eşleştirilir
        val normalized = if (tag == "in" || tag.startsWith("in-")) "id" + tag.removePrefix("in") else tag
        return languages.indexOfFirst { it.first.equals(normalized, true) }
            .takeIf { it >= 0 }
            ?: languages.indexOfFirst { it.first.isNotEmpty() && normalized.startsWith(it.first.substringBefore('-'), true) }
                .coerceAtLeast(0)
    }

    private fun showLanguageDialog() {
        val names = languages.indices.map { languageName(it) }.toTypedArray()
        MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.settings_language)
            .setSingleChoiceItems(names, currentLanguageIndex()) { dialog, which ->
                dialog.dismiss()
                val tag = languages[which].first
                // Activity yeniden oluşturulur ve tüm ekranlar seçilen dilde açılır
                AppCompatDelegate.setApplicationLocales(
                    if (tag.isEmpty()) LocaleListCompat.getEmptyLocaleList()
                    else LocaleListCompat.forLanguageTags(tag)
                )
            }
            .show()
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
