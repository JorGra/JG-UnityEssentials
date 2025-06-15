// FileSizeColumn.cs
// Project-window “Size” column with a performance-friendly toggle.
//
// © 2025 – public-domain; modify freely.

using UnityEditor;
using UnityEngine;
using System.IO;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Toolbars;
using UnityEngine.UIElements;      // DisplayStyle
#endif

[InitializeOnLoad]
public static class FileSizeColumn
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Constants / prefs
    const float kColumnWidth = 70f;
    const string kPrefKey = "FileSizeColumnEnabled";

    public static bool Enabled => EditorPrefs.GetBool(kPrefKey, true);

    // ─────────────────────────────────────────────────────────────────────────────
    // Initialise callback
    static FileSizeColumn()
    {
        EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Draw size column
    private static void OnProjectWindowItemGUI(string guid, Rect rect)
    {
        if (!Enabled) return;       // feature disabled – fast-exit
        if (rect.height > 20) return;       // skip grid-view thumbnails

        var labelRect = new Rect(rect.xMax - kColumnWidth, rect.y, kColumnWidth, rect.height);

        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return;

        long bytes = File.Exists(path) ? new FileInfo(path).Length
                   : Directory.Exists(path) ? GetDirectorySize(path)
                   : 0;

        GUI.Label(labelRect, FormatBytes(bytes), EditorStyles.miniLabel);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Toolbar toggle (Unity 2021.2+)
#if UNITY_2021_2_OR_NEWER
    [EditorToolbarElement("File Size Column/Toggle")]
    public sealed class FileSizeColumnToggle : EditorToolbarToggle
    {
        public FileSizeColumnToggle()
        {
            text = "Size";
            tooltip = "Show/Hide File Size column in the Project window";
            value = FileSizeColumn.Enabled;

            // Update preference when the user clicks.
            this.RegisterValueChangedCallback(evt => FileSizeColumn.SetEnabled(evt.newValue));

            // Keep the button visible only while a Project window is focused.
            EditorApplication.update += UpdateVisibility;
            UpdateVisibility();             // run once immediately
        }

        void UpdateVisibility()
        {
            bool show =
                EditorWindow.focusedWindow &&
                EditorWindow.focusedWindow.GetType().Name == "ProjectBrowser";

            style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        ~FileSizeColumnToggle() => EditorApplication.update -= UpdateVisibility;
    }
#endif

    // ─────────────────────────────────────────────────────────────────────────────
    // Menu fallback (works in every Unity version)
    [MenuItem("Tools/Show File Sizes", false, 3020)]
    private static void MenuToggle() => SetEnabled(!Enabled);

    [MenuItem("Tools/Show File Sizes", true)]
    private static bool MenuToggleValidate()
    {
        Menu.SetChecked("Tools/Show File Sizes", Enabled);
        return true;
    }

    public static void SetEnabled(bool on)
    {
        EditorPrefs.SetBool(kPrefKey, on);
        EditorApplication.RepaintProjectWindow();     // refresh immediately
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    private static string FormatBytes(long bytes)
    {
        const int k = 1024;
        if (bytes >= k * k * k) return $"{bytes / (float)(k * k * k):0.#} GB";
        if (bytes >= k * k) return $"{bytes / (float)(k * k):0.#} MB";
        return $"{bytes / (float)k:0.#} kB";
    }

    private static long GetDirectorySize(string directory)
    {
        long total = 0;
        try
        {
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                total += new FileInfo(file).Length;
        }
        catch { /* silently ignore permission issues */ }
        return total;
    }
}
