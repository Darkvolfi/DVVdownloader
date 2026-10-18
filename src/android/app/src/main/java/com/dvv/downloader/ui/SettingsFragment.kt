package com.dvv.downloader.ui

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import com.dvv.downloader.databinding.FragmentSettingsBinding

class SettingsFragment : Fragment() {

    private var _binding: FragmentSettingsBinding? = null
    private val binding get() = _binding!!

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentSettingsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val dir = requireContext().getExternalFilesDir(null) ?: requireContext().filesDir
        binding.tvDownloadPath.text = dir.absolutePath

        binding.btnViewLicense.setOnClickListener {
            com.google.android.material.dialog.MaterialAlertDialogBuilder(requireContext())
                .setTitle("GNU General Public License v3.0")
                .setMessage(
                    "DVVdownloader — GNU GPL v3.0\n\n" +
                    "Bu yazılım özgür bir yazılımdır; Free Software Foundation tarafından yayımlanan " +
                    "GNU Genel Kamu Lisansı sürüm 3 hükümleri kapsamında yeniden dağıtabilir ve/veya değiştirebilirsiniz.\n\n" +
                    "Bu program faydalı olması umuduyla dağıtılmaktadır; ancak HİÇBİR GARANTİ VERİLMEZ; " +
                    "hatta PAZARLANABİLİRLİK veya BELİRLİ BİR AMACA UYGUNLUK garantisi dahi verilmez.\n\n" +
                    "Telif Hakkı (C) 2026 DVVdownloader Katkıda Bulunanları."
                )
                .setPositiveButton("Kapat", null)
                .show()
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
