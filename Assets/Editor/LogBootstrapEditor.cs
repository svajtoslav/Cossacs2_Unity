#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class LogBootstrapEditor
{
    static LogBootstrapEditor()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

        Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

        Debug.Log("[LogBootstrapEditor] Stack trace logging configured in editor: Log/Warning=None, Assert/Error/Exception=ScriptOnly");
    }
}
#endif
