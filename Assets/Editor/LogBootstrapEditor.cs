#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class LogBootstrapEditor
{
    private static readonly object ErrorGate = new object();
    private static readonly HashSet<string> RecordedErrors = new HashSet<string>();
    private static string ErrorLogPath;

    static LogBootstrapEditor()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

        Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

        ErrorLogPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "C2DiagnosticLogs", "UnityErrors.log");
        Application.logMessageReceivedThreaded += RecordError;
        AssemblyReloadEvents.beforeAssemblyReload += () => Application.logMessageReceivedThreaded -= RecordError;

        Debug.Log("[LogBootstrapEditor] Stack trace logging configured in editor: Log/Warning=None, Assert/Error/Exception=ScriptOnly");
    }

    private static void RecordError(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        lock (ErrorGate)
        {
            // Bound repeated exceptions: diagnostics must not stall an already failing frame.
            if (RecordedErrors.Count >= 100 || !RecordedErrors.Add(type + "\n" + message + "\n" + stack)) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ErrorLogPath));
                if (File.Exists(ErrorLogPath) && new FileInfo(ErrorLogPath).Length > 4 * 1024 * 1024)
                    File.Move(ErrorLogPath, ErrorLogPath + "." + DateTime.UtcNow.Ticks);
                File.AppendAllText(ErrorLogPath, DateTime.UtcNow.ToString("O") + " " + type + "\n" +
                    message + "\n" + stack + "\n\n");
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
#endif
