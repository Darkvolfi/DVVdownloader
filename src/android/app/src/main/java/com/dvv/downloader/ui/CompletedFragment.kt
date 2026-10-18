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

    private fun openFile(item: HistoryItem) {
        val file = File(item.filePath)
        if (!file.exists()) {
            Toast.makeText(requireContext(), "Dosya bulunamadı veya silinmiş", Toast.LENGTH_SHORT).show()
            return
        }

        try {
            val uri: Uri = FileProvider.getUriForFile(
                requireContext(),
                "${requireContext().packageName}.fileprovider",
                file
            )
            val intent = Intent(Intent.ACTION_VIEW).apply {
                val mime = if (file.extension.equals("mp3", true) || file.extension.equals("m4a", true)) "audio/*" else "video/*"
                setDataAndType(uri, mime)
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
            startActivity(intent)
        } catch (_: Exception) {
            Toast.makeText(requireContext(), "Bu dosyayı açabilecek bir uygulama bulunamadı", Toast.LENGTH_SHORT).show()
        }
    }

    private fun shareFile(item: HistoryItem) {
        val file = File(item.filePath)
        if (!file.exists()) {
            Toast.makeText(requireContext(), "Dosya bulunamadı", Toast.LENGTH_SHORT).show()
            return
        }

        try {
            val uri: Uri = FileProvider.getUriForFile(
                requireContext(),
                "${requireContext().packageName}.fileprovider",
                file
            )
            val intent = Intent(Intent.ACTION_SEND).apply {
                val mime = if (file.extension.equals("mp3", true) || file.extension.equals("m4a", true)) "audio/*" else "video/*"
                type = mime
                putExtra(Intent.EXTRA_STREAM, uri)
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
            startActivity(Intent.createChooser(intent, "Medya Paylaş"))
        } catch (e: Exception) {
            Toast.makeText(requireContext(), "Paylaşım hatası: ${e.message}", Toast.LENGTH_SHORT).show()
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
