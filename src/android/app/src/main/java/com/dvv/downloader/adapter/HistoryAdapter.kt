package com.dvv.downloader.adapter

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.dvv.downloader.databinding.ItemHistoryBinding
import com.dvv.downloader.models.HistoryItem
import com.dvv.downloader.repository.MediaEngine

class HistoryAdapter(
    private var items: List<HistoryItem>,
    private val onOpen: (HistoryItem) -> Unit,
    private val onShare: (HistoryItem) -> Unit
) : RecyclerView.Adapter<HistoryAdapter.ViewHolder>() {

    fun updateItems(newItems: List<HistoryItem>) {
        items = newItems
        notifyDataSetChanged()
    }

    inner class ViewHolder(val binding: ItemHistoryBinding) : RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val binding = ItemHistoryBinding.inflate(LayoutInflater.from(parent.context), parent, false)
        return ViewHolder(binding)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = items[position]
        holder.binding.tvHistoryTitle.text = item.title
        holder.binding.tvHistoryPlatform.text = MediaEngine.platformLabel(item.platform)
        holder.binding.tvHistorySize.text = item.fileSizeFormatted
        holder.binding.tvHistoryDate.text = item.dateFormatted

        holder.binding.btnOpenFile.setOnClickListener {
            onOpen(item)
        }
        holder.binding.btnShareFile.setOnClickListener {
            onShare(item)
        }
    }

    override fun getItemCount(): Int = items.size
}
