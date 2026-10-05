#if UNITY_EDITOR
using UnityEditor;

internal static class DummyEditorAssembly
{
    [InitializeOnLoadMethod]
    private static void Init() { }
}
#endif