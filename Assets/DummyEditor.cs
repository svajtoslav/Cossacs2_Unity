#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class DummyEditor
{
    static DummyEditor() { }
}
#endif