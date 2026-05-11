// C2BorderlessFullscreenHotkey.cs
// Put this file here:
// Assets/Cossacks2Bridge/Maps/C2BorderlessFullscreenHotkey.cs
//
// Runtime hotkey:
//   LeftShift + Space = toggle borderless fullscreen Game View while Play Mode is running.
//   Esc = close the borderless Game View popup in the Unity Editor.
//
// In a real build it toggles FullScreenMode.FullScreenWindow.

using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    [DefaultExecutionOrder(-32768)]
    public sealed class C2BorderlessFullscreenHotkey : MonoBehaviour
    {
        private static C2BorderlessFullscreenHotkey s_Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (s_Instance != null)
                return;

            GameObject go = new GameObject("[C2] Borderless Fullscreen Hotkey");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);

            s_Instance = go.AddComponent<C2BorderlessFullscreenHotkey>();
        }

        private void Update()
        {
            // Strictly LEFT Shift + Space.
            if (IsLeftShiftSpacePressedThisFrame())
            {
#if UNITY_EDITOR
                C2EditorBorderlessGameView.Toggle();
#else
                C2BuildBorderlessFullscreen.Toggle();
#endif
            }

#if UNITY_EDITOR
            // Safety close key, because borderless popup has no normal title bar.
            if (IsEscapePressedThisFrame())
                C2EditorBorderlessGameView.CloseIfOpen();
#endif
        }

        private static bool IsLeftShiftSpacePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null
                   && kb.leftShiftKey.isPressed
                   && kb.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private static bool IsEscapePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private static class C2BuildBorderlessFullscreen
        {
            private static bool s_WindowedBeforeToggle;

            public static void Toggle()
            {
                if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
                {
                    Screen.fullScreen = false;
                    s_WindowedBeforeToggle = true;
                    return;
                }

                int w = Display.main != null && Display.main.systemWidth > 0
                    ? Display.main.systemWidth
                    : Screen.currentResolution.width;

                int h = Display.main != null && Display.main.systemHeight > 0
                    ? Display.main.systemHeight
                    : Screen.currentResolution.height;

                if (w <= 0) w = Screen.width;
                if (h <= 0) h = Screen.height;

                Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
                s_WindowedBeforeToggle = false;
            }
        }
    }
}

#if UNITY_EDITOR
internal static class C2EditorBorderlessGameView
{
    private static readonly BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly BindingFlags AnyStatic =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static EditorWindow s_FullscreenGameView;
    private static Rect s_LastFullscreenRect;

    [MenuItem("Tools/Cossacks2/Toggle Borderless GameView")]
    private static void ToggleFromMenu()
    {
        Toggle();
    }

    public static void Toggle()
    {
        if (s_FullscreenGameView != null)
        {
            CloseIfOpen();
            return;
        }

        Open();
    }

    public static void CloseIfOpen()
    {
        if (s_FullscreenGameView == null)
            return;

        try
        {
            s_FullscreenGameView.Close();
        }
        catch
        {
            // Ignore: window may already be closed by Unity domain reload / play stop.
        }

        s_FullscreenGameView = null;
    }

    private static void Open()
    {
        Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
        {
            Debug.LogWarning("[C2:FULLSCREEN] UnityEditor.GameView type not found.");
            return;
        }

        CloseIfOpen();

        EditorWindow window = ScriptableObject.CreateInstance(gameViewType) as EditorWindow;
        if (window == null)
        {
            Debug.LogWarning("[C2:FULLSCREEN] Could not create GameView window.");
            return;
        }

        s_FullscreenGameView = window;
        s_LastFullscreenRect = GetCurrentDesktopRect();

        window.titleContent = new GUIContent("C2 Borderless Game");
        TryHideGameViewToolbar(window, gameViewType);

        // Position before and after ShowPopup: Unity can clamp/adjust once during creation.
        window.position = s_LastFullscreenRect;
        window.ShowPopup();
        window.position = s_LastFullscreenRect;
        window.Focus();

        EditorApplication.delayCall += () =>
        {
            if (s_FullscreenGameView == null)
                return;

            TryHideGameViewToolbar(s_FullscreenGameView, gameViewType);
            s_FullscreenGameView.position = s_LastFullscreenRect;
            s_FullscreenGameView.Focus();
            s_FullscreenGameView.Repaint();
        };

        Debug.Log("[C2:FULLSCREEN] Borderless GameView opened. LeftShift+Space toggles it, Esc closes it.");
    }

    private static Rect GetCurrentDesktopRect()
    {
        try
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            Vector2 point = new Vector2(main.x + main.width * 0.5f, main.y + main.height * 0.5f);

            Type internalUtilityType = typeof(UnityEditorInternal.InternalEditorUtility);
            MethodInfo getBoundsMethod = internalUtilityType.GetMethod(
                "GetBoundsOfDesktopAtPoint",
                AnyStatic
            );

            if (getBoundsMethod != null)
            {
                object result = getBoundsMethod.Invoke(null, new object[] { point });
                if (result is Rect rect && rect.width > 100.0f && rect.height > 100.0f)
                    return rect;
            }
        }
        catch
        {
            // Fallback below.
        }

        Resolution res = Screen.currentResolution;
        int w = res.width > 100 ? res.width : Mathf.Max(1280, Screen.width);
        int h = res.height > 100 ? res.height : Mathf.Max(720, Screen.height);
        return new Rect(0, 0, w, h);
    }

    private static void TryHideGameViewToolbar(EditorWindow window, Type gameViewType)
    {
        if (window == null || gameViewType == null)
            return;

        // Unity versions differ. Try all known toolbar flags/properties.
        TrySetBoolProperty(window, gameViewType, "showToolbar", false);
        TrySetBoolProperty(window, gameViewType, "m_ShowToolbar", false);
        TrySetBoolField(window, gameViewType, "showToolbar", false);
        TrySetBoolField(window, gameViewType, "m_ShowToolbar", false);

        // Some Unity versions store it on PlayModeView base class.
        Type baseType = gameViewType.BaseType;
        while (baseType != null)
        {
            TrySetBoolProperty(window, baseType, "showToolbar", false);
            TrySetBoolProperty(window, baseType, "m_ShowToolbar", false);
            TrySetBoolField(window, baseType, "showToolbar", false);
            TrySetBoolField(window, baseType, "m_ShowToolbar", false);
            baseType = baseType.BaseType;
        }
    }

    private static void TrySetBoolProperty(object target, Type type, string name, bool value)
    {
        try
        {
            PropertyInfo prop = type.GetProperty(name, AnyInstance);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
                prop.SetValue(target, value, null);
        }
        catch
        {
            // Ignore unsupported Unity internals.
        }
    }

    private static void TrySetBoolField(object target, Type type, string name, bool value)
    {
        try
        {
            FieldInfo field = type.GetField(name, AnyInstance);
            if (field != null && field.FieldType == typeof(bool))
                field.SetValue(target, value);
        }
        catch
        {
            // Ignore unsupported Unity internals.
        }
    }
}
#endif
