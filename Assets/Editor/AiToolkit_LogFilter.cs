#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class AiToolkit_LogFilter
{
    static AiToolkit_LogFilter()
    {
        Application.logMessageReceived += OnLog;
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        // Мы не можем отменить уже выведенный лог, но можем:
        // 1) не давать спамить дальше в обработчиках,
        // 2) и главное — можно автоматически чистить консоль (не советую),
        // поэтому делаем мягко: ничего не трогаем.
        //
        // Этот файл полезен только если ты захочешь подключить авто-clear или
        // перекинуть эти сообщения в отдельный файл.
    }
}
#endif
