package com.dvv.downloader.ui

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import com.dvv.downloader.MainActivity
import com.dvv.downloader.adapter.QueueAdapter
import com.dvv.downloader.databinding.FragmentQueueBinding
import com.dvv.downloader.service.DownloadManager

class QueueFragment : Fragment() {

    private var _binding: FragmentQueueBinding? = null
    private val binding get() = _binding!!
    private lateinit var adapter: QueueAdapter

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentQueueBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val mainActivity = activity as? MainActivity ?: return

        adapter = QueueAdapter(
            tasks = emptyList(),
            onCancel = { task ->
                DownloadManager.cancelDownload(task, mainActivity.repository)
            },
            onRetry = { task ->
                DownloadManager.retryDownload(
                    task = task,
                    repository = mainActivity.repository,
                    context = requireContext().applicationContext
                )
            },
            onRemove = { task ->
                DownloadManager.removeDownload(task.id, mainActivity.repository)
            }
        )

        binding.rvQueue.layoutManager = LinearLayoutManager(requireContext())
        binding.rvQueue.adapter = adapter

        mainActivity.repository.activeTasks.observe(viewLifecycleOwner) { tasks ->
            adapter.updateTasks(tasks)
            binding.tvQueueEmpty.visibility = if (tasks.isEmpty()) View.VISIBLE else View.GONE
            binding.rvQueue.visibility = if (tasks.isEmpty()) View.GONE else View.VISIBLE
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
