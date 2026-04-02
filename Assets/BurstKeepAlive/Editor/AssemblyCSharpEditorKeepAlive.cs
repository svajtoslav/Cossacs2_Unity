#if UNITY_EDITOR
using UnityEditor;

internal static class AssemblyCSharpEditorKeepAlive
{
    [InitializeOnLoadMethod]
    private static void Touch()
    {
    }
}
#endif
