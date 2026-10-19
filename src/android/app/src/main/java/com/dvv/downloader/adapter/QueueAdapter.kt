package com.dvv.downloader.adapter

import android.content.res.ColorStateList
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.core.content.ContextCompat
import androidx.recyclerview.widget.RecyclerView
import com.dvv.downloader.R
import com.dvv.downloader.databinding.ItemQueueBinding
import com.dvv.downloader.models.DownloadTask
import com.dvv.downloader.models.TaskStatus
import com.dvv.downloader.repository.MediaEngine

class QueueAdapter(
    private var tasks: List<DownloadTask>,
    private val onCancel: (DownloadTask) -> Unit,
    private val onRetry: (DownloadTask) -> Unit,
    private val onRemove: (DownloadTask) -> Unit
) : RecyclerView.Adapter<QueueAdapter.ViewHolder>() {

    fun updateTasks(newTasks: List<DownloadTask>) {
        tasks = newTasks
        notifyDataSetChanged()
    }

    inner class ViewHolder(val binding: ItemQueueBinding) : RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val binding = ItemQueueBinding.inflate(LayoutInflater.from(parent.context), parent, false)
        return ViewHolder(binding)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val task = tasks[position]
        val context = holder.itemView.context

        holder.binding.tvTaskTitle.text = task.title
        holder.binding.tvTaskPlatform.text = "${MediaEngine.platformLabel(task.platform)} • ${task.qualityLabel}"
        holder.binding.tvTaskPercent.text = "${task.progress}%"
        holder.binding.pbTaskProgress.progress = task.progress

        // Hız ve Durum
        val speedText = if (task.status == TaskStatus.INDIRILIYOR) {
            context.getString(R.string.queue_speed, task.speed)
        } else {
            ""
        }
        holder.binding.tvTaskSpeed.text = speedText

        // Durum Rozeti ve Renkleri
        when (task.status) {
            TaskStatus.KUYRUKTA -> {
                holder.binding.tvTaskStatus.text = context.getString(R.string.status_queued)
                holder.binding.tvTaskStatus.setTextColor(ContextCompat.getColor(context, R.color.text_secondary))
            }
            TaskStatus.INDIRILIYOR -> {
                holder.binding.tvTaskStatus.text = context.getString(R.string.status_downloading)
                holder.binding.tvTaskStatus.setTextColor(ContextCompat.getColor(context, R.color.primary_accent))
                holder.binding.pbTaskProgress.progressTintList = ColorStateList.valueOf(
                    ContextCompat.getColor(context, R.color.primary_accent)
                )
            }
            TaskStatus.TAMAMLANDI -> {
                holder.binding.tvTaskStatus.text = context.getString(R.string.status_completed)
                holder.binding.tvTaskStatus.setTextColor(ContextCompat.getColor(context, R.color.success_color))
                holder.binding.pbTaskProgress.progressTintList = ColorStateList.valueOf(
                    ContextCompat.getColor(context, R.color.success_color)
                )
            }
            TaskStatus.HATA -> {
                holder.binding.tvTaskStatus.text = context.getString(R.string.status_error)
                holder.binding.tvTaskStatus.setTextColor(ContextCompat.getColor(context, R.color.error_color))
                holder.binding.pbTaskProgress.progressTintList = ColorStateList.valueOf(
                    ContextCompat.getColor(context, R.color.error_color)
                )
            }
            TaskStatus.IPTAL -> {
                holder.binding.tvTaskStatus.text = context.getString(R.string.status_cancelled)
                holder.binding.tvTaskStatus.setTextColor(ContextCompat.getColor(context, R.color.warning_color))
                holder.binding.pbTaskProgress.progressTintList = ColorStateList.valueOf(
                    ContextCompat.getColor(context, R.color.warning_color)
                )
            }
        }

        // Hata metni gösterimi
        if (task.status == TaskStatus.HATA && task.errorMessage.isNotBlank()) {
            holder.binding.tvTaskError.visibility = View.VISIBLE
            holder.binding.tvTaskError.text = task.errorMessage
        } else {
            holder.binding.tvTaskError.visibility = View.GONE
        }

        // Aksiyon Butonları Görünürlüğü (ERR-005)
        holder.binding.btnCancelTask.visibility = if (task.canCancel) View.VISIBLE else View.GONE
        holder.binding.btnRetryTask.visibility = if (task.canRetry) View.VISIBLE else View.GONE
        holder.binding.btnRemoveTask.visibility = if (task.canRemove) View.VISIBLE else View.GONE

        holder.binding.btnCancelTask.setOnClickListener {
            onCancel(task)
        }

        holder.binding.btnRetryTask.setOnClickListener {
            onRetry(task)
        }

        holder.binding.btnRemoveTask.setOnClickListener {
            onRemove(task)
        }
    }

    override fun getItemCount(): Int = tasks.size
}
