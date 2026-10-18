package com.dvv.downloader.adapter

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.core.content.ContextCompat
import androidx.recyclerview.widget.RecyclerView
import com.dvv.downloader.R
import com.dvv.downloader.databinding.ItemFormatBinding
import com.dvv.downloader.models.MediaFormatItem

class FormatAdapter(
    private val items: List<MediaFormatItem>,
    private val onSelected: (MediaFormatItem) -> Unit
) : RecyclerView.Adapter<FormatAdapter.ViewHolder>() {

    var selectedIndex = 0

    inner class ViewHolder(val binding: ItemFormatBinding) : RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val binding = ItemFormatBinding.inflate(LayoutInflater.from(parent.context), parent, false)
        return ViewHolder(binding)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = items[position]
        val context = holder.itemView.context
        val isSelected = position == selectedIndex

        holder.binding.tvFormatQuality.text = item.qualityLabel
        holder.binding.tvFormatExt.text = if (item.isAudioOnly) {
            "Ses Akışı (${item.ext.uppercase()})"
        } else {
            "Video (${item.ext.uppercase()})"
        }
        holder.binding.tvFormatSize.text = item.formattedSize
        holder.binding.rbSelected.isChecked = isSelected

        // Seçim Vurgusu (ERR-006)
        val strokeColor = if (isSelected) {
            ContextCompat.getColor(context, R.color.primary_accent)
        } else {
            ContextCompat.getColor(context, R.color.divider_color)
        }
        holder.binding.root.strokeColor = strokeColor
        holder.binding.root.strokeWidth = if (isSelected) 2 else 1

        holder.itemView.setOnClickListener {
            val prev = selectedIndex
            selectedIndex = holder.bindingAdapterPosition
            notifyItemChanged(prev)
            notifyItemChanged(selectedIndex)
            onSelected(item)
        }
    }

    override fun getItemCount(): Int = items.size
}
