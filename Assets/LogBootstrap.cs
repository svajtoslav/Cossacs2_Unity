using UnityEngine;

public static class LogBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ConfigureStackTraces()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

        Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

        Debug.Log("[LogBootstrap] Stack trace logging configured: Log/Warning=None, Assert/Error/Exception=ScriptOnly");
    }
}
