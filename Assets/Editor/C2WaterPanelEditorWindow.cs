#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
internal static class C2WaterPanelWindowBootstrap
{
    static C2WaterPanelWindowBootstrap()
    {
        // water panel bootstrap removed
    }
}

public sealed class C2WaterPanelEditorWindow : EditorWindow
{
    // water panel removed
}
#endif
