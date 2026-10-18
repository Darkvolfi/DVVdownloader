package com.dvv.downloader.ui

import android.content.ClipboardManager
import android.content.Context
import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.dvv.downloader.MainActivity
import com.dvv.downloader.R
import com.dvv.downloader.adapter.FormatAdapter
import com.dvv.downloader.databinding.FragmentHomeBinding
import com.dvv.downloader.models.DownloadTask
import com.dvv.downloader.models.MediaFormatItem
import com.dvv.downloader.models.MediaMetadata
import com.dvv.downloader.models.TaskStatus
import com.dvv.downloader.repository.MediaEngine
import com.dvv.downloader.service.DownloadManager
import kotlinx.coroutines.launch
import java.util.UUID

class HomeFragment : Fragment() {

    private var _binding: FragmentHomeBinding? = null
    private val binding get() = _binding!!

    private var currentMetadata: MediaMetadata? = null
    private var selectedFormat: MediaFormatItem? = null
    private var pendingUrl: String? = null

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentHomeBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        binding.rvFormats.layoutManager = LinearLayoutManager(requireContext())

        binding.etUrl.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {
                val url = s?.toString()?.trim() ?: ""
                binding.tvDetectedPlatform.text = MediaEngine.detectPlatform(url)
            }
            override fun afterTextChanged(s: Editable?) {}
        })

        binding.btnPaste.setOnClickListener {
            pasteFromClipboard()
        }

        binding.btnAnalyze.setOnClickListener {
            val url = binding.etUrl.text?.toString()?.trim() ?: ""
            if (url.isNotEmpty()) {
                analyzeUrl(url)
            } else {
                Toast.makeText(requireContext(), "Lütfen geçerli bir URL girin", Toast.LENGTH_SHORT).show()
            }
        }

        binding.btnStartDownload.setOnClickListener {
            startDownload()
        }

        pendingUrl?.let {
            pendingUrl = null
            setInitialUrl(it)
        }
    }

    fun setInitialUrl(url: String) {
        // Paylaşım ile soğuk açılışta görünüm henüz oluşmamış olabilir; URL onViewCreated'da işlenir
        if (_binding == null) {
            pendingUrl = url
            return
        }
        binding.etUrl.setText(url)
        analyzeUrl(url)
    }

    private fun pasteFromClipboard() {
        val clipboard = requireContext().getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
        val clip = clipboard.primaryClip
        if (clip != null && clip.itemCount > 0) {
            val text = clip.getItemAt(0).text?.toString()?.trim() ?: ""
            if (text.isNotEmpty()) {
                binding.etUrl.setText(text)
                analyzeUrl(text)
            }
        } else {
            Toast.makeText(requireContext(), "Pano boş", Toast.LENGTH_SHORT).show()
        }
    }

    private fun analyzeUrl(url: String) {
        binding.cardLoading.visibility = View.VISIBLE
        binding.cardPreview.visibility = View.GONE
        binding.btnAnalyze.isEnabled = false

        lifecycleScope.launch {
            try {
                val metadata = MediaEngine.extractMetadata(url)
                currentMetadata = metadata

                binding.tvMediaTitle.text = metadata.title
                binding.tvMediaAuthor.text = "${metadata.uploader} • ${metadata.platform}"

                selectedFormat = metadata.formats.firstOrNull()
                binding.rvFormats.adapter = FormatAdapter(metadata.formats) { item ->
                    selectedFormat = item
                }

                binding.cardPreview.visibility = View.VISIBLE
            } catch (e: Exception) {
                Toast.makeText(requireContext(), "Analiz hatası: ${e.message}", Toast.LENGTH_LONG).show()
            } finally {
                binding.cardLoading.visibility = View.GONE
                binding.btnAnalyze.isEnabled = true
            }
        }
    }

    private fun startDownload() {
        val meta = currentMetadata ?: return
        val fmt = selectedFormat ?: return

        val taskId = UUID.randomUUID().toString()
        val task = DownloadTask(
            id = taskId,
            url = fmt.directUrl.ifEmpty { meta.webpageUrl },
            title = meta.title,
            platform = meta.platform,
            thumbnailUrl = meta.thumbnailUrl,
            qualityLabel = fmt.qualityLabel,
            resolution = fmt.resolution,
            ext = fmt.ext,
            isAudioOnly = fmt.isAudioOnly,
            formatSelector = fmt.formatSelector,
            status = TaskStatus.KUYRUKTA
        )

        val mainActivity = activity as? MainActivity ?: return

        // İndirme yöneticisi üzerinden başlat (Sekme değişiminde iptal olmaz)
        DownloadManager.startDownload(
            task = task,
            repository = mainActivity.repository,
            context = requireContext().applicationContext
        )

        // Kuyruk sekmesine geç
        mainActivity.switchToTab(R.id.nav_queue)
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
