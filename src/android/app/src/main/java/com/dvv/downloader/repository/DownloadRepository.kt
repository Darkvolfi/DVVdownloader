package com.dvv.downloader.repository

import android.content.Context
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import com.dvv.downloader.models.DownloadTask
import com.dvv.downloader.models.HistoryItem
import org.json.JSONArray
import org.json.JSONObject

class DownloadRepository(private val context: Context) {

    private val _activeTasks = MutableLiveData<List<DownloadTask>>(emptyList())
    val activeTasks: LiveData<List<DownloadTask>> = _activeTasks

    private val _historyItems = MutableLiveData<List<HistoryItem>>(emptyList())
    val historyItems: LiveData<List<HistoryItem>> = _historyItems

    private val prefs = context.getSharedPreferences("dvv_history", Context.MODE_PRIVATE)

    init {
        loadHistory()
    }

    fun addTask(task: DownloadTask) {
        val current = _activeTasks.value?.toMutableList() ?: mutableListOf()
        current.add(0, task)
        _activeTasks.postValue(current)
    }

    fun updateTask(task: DownloadTask) {
        val current = _activeTasks.value?.toMutableList() ?: return
        val index = current.indexOfFirst { it.id == task.id }
        if (index != -1) {
            current[index] = task
            _activeTasks.postValue(current)
        }
    }

    fun removeTask(taskId: String) {
        val current = _activeTasks.value?.toMutableList() ?: return
        current.removeAll { it.id == taskId }
        _activeTasks.postValue(current)
    }

    fun addHistory(item: HistoryItem) {
        val current = _historyItems.value?.toMutableList() ?: mutableListOf()
        current.add(0, item)
        _historyItems.postValue(current)
        saveHistory(current)
    }

    fun clearHistory() {
        _historyItems.postValue(emptyList())
        prefs.edit().remove("history_json").apply()
    }

    private fun loadHistory() {
        val jsonStr = prefs.getString("history_json", null) ?: return
        val list = mutableListOf<HistoryItem>()
        try {
            val arr = JSONArray(jsonStr)
            for (i in 0 until arr.length()) {
                val obj = arr.getJSONObject(i)
                list.add(
                    HistoryItem(
                        id = obj.getString("id"),
                        title = obj.getString("title"),
                        platform = obj.getString("platform"),
                        filePath = obj.getString("filePath"),
                        fileSizeFormatted = obj.getString("fileSizeFormatted"),
                        dateFormatted = obj.getString("dateFormatted")
                    )
                )
            }
            _historyItems.postValue(list)
        } catch (_: Exception) {}
    }

    private fun saveHistory(list: List<HistoryItem>) {
        try {
            val arr = JSONArray()
            for (item in list) {
                val obj = JSONObject()
                obj.put("id", item.id)
                obj.put("title", item.title)
                obj.put("platform", item.platform)
                obj.put("filePath", item.filePath)
                obj.put("fileSizeFormatted", item.fileSizeFormatted)
                obj.put("dateFormatted", item.dateFormatted)
                arr.put(obj)
            }
            prefs.edit().putString("history_json", arr.toString()).apply()
        } catch (_: Exception) {}
    }
}
