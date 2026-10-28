package com.dvv.downloader.ui

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.core.content.FileProvider
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import com.dvv.downloader.MainActivity
import com.dvv.downloader.R
import com.dvv.downloader.adapter.HistoryAdapter
import com.dvv.downloader.databinding.FragmentCompletedBinding
import com.dvv.downloader.models.HistoryItem
import java.io.File

class CompletedFragment : Fragment() {

    private var _binding: FragmentCompletedBinding? = null
    private val binding get() = _binding!!
    private lateinit var adapter: HistoryAdapter

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentCompletedBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val mainActivity = activity as? MainActivity ?: return

        adapter = HistoryAdapter(
            items = emptyList(),
            onOpen = { item -> openFile(item) },
            onShare = { item -> shareFile(item) }
        )

        binding.rvCompleted.layoutManager = LinearLayoutManager(requireContext())
        binding.rvCompleted.adapter = adapter

        mainActivity.repository.historyItems.observe(viewLifecycleOwner) { items ->
            adapter.updateItems(items)
            binding.tvCompletedEmpty.visibility = if (items.isEmpty()) View.VISIBLE else View.GONE
            binding.rvCompleted.visibility = if (items.isEmpty()) View.GONE else View.VISIBLE
        }

        binding.btnClearHistory.setOnClickListener {
            mainActivity.repository.clearHistory()
        }
    }

    /**
     * Geçmişteki konum ya bir content:// adresi (İndirilenler / seçilen klasör) ya da dosya yoludur
     * (eski sürümlerin kayıtları, Android 8–9). Dosya yoksa null döner.
     */
    private fun resolveUri(item: HistoryItem): Uri? {
        if (item.filePath.startsWith("content://")) {
            val uri = Uri.parse(item.filePath)
            return try {
                requireContext().contentResolver.openFileDescriptor(uri, "r")?.use { uri }
            } catch (_: Exception) {
                null
            }
        }
        val file = File(item.filePath)
        if (!file.exists()) return null
        return FileProvider.getUriForFile(requireContext(), "${requireContext().packageName}.fileprovider", file)
    }

    private fun mimeOf(item: HistoryItem): String =
        if (item.title.contains("[Audio]") || item.filePath.endsWith(".mp3", true) || item.filePath.endsWith(".m4a", true)) "audio/*" else "video/*"

    private fun openFile(item: HistoryItem) {
        val uri = resolveUri(item)
        if (uri == null) {
            Toast.makeText(requireContext(), getString(R.string.toast_file_missing), Toast.LENGTH_SHORT).show()
            return
        }

        try {
            val intent = Intent(Intent.ACTION_VIEW).apply {
                setDataAndType(uri, mimeOf(item))
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
            startActivity(intent)
        } catch (_: Exception) {
            Toast.makeText(requireContext(), getString(R.string.toast_no_app), Toast.LENGTH_SHORT).show()
        }
    }

    private fun shareFile(item: HistoryItem) {
        val uri = resolveUri(item)
        if (uri == null) {
            Toast.makeText(requireContext(), getString(R.string.toast_file_not_found), Toast.LENGTH_SHORT).show()
            return
        }

        try {
            val intent = Intent(Intent.ACTION_SEND).apply {
                type = mimeOf(item)
                putExtra(Intent.EXTRA_STREAM, uri)
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
            startActivity(Intent.createChooser(intent, getString(R.string.share_chooser)))
        } catch (e: Exception) {
            Toast.makeText(requireContext(), getString(R.string.toast_share_error, e.message ?: ""), Toast.LENGTH_SHORT).show()
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
