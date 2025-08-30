// Assets/Editor/ToggleScriptWrap.cs
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public static class ToggleScriptWrap
{
    private const string Menu = "Assets/Toggle Script (wrap)";
    private const string Begin = "// __SCRIPT_TOGGLER_BEGIN__";
    private const string End = "// __SCRIPT_TOGGLER_END__";

    [MenuItem(Menu)]
    private static void Toggle()
    {
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0) return;

        int changed = 0;
        EditorApplication.LockReloadAssemblies();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".cs")) continue;

                var full = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
                var text = File.ReadAllText(full);

                if (IsWrapped(text))
                {
                    // Enable: unwrap to original content
                    var enabled = Unwrap(text);
                    if (enabled != null) { File.WriteAllText(full, enabled); changed++; }
                }
                else
                {
                    // Disable: wrap whole file in #if false
                    var nl = System.Environment.NewLine;
                    var wrapped = $"{Begin}{nl}#if false // toggled by Script Toggler{nl}{text}{nl}#endif{nl}{End}{nl}";
                    File.WriteAllText(full, wrapped);
                    changed++;
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorApplication.UnlockReloadAssemblies();
        }

        if (changed > 0)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            EditorApplication.delayCall += () => CompilationPipeline.RequestScriptCompilation();
            Debug.Log($"Toggled {changed} script(s) via #if false wrapper.");
        }
    }

    [MenuItem(Menu, true)]
    private static bool Validate() =>
        Selection.assetGUIDs.Any(g => AssetDatabase.GUIDToAssetPath(g)?.EndsWith(".cs") == true);

    private static bool IsWrapped(string s) =>
        s.Contains(Begin) && s.Contains(End) && s.Contains("#if false");

    private static string Unwrap(string s)
    {
        // Extract original content between the wrapper’s "#if false" and its matching "#endif"
        int begin = s.IndexOf(Begin);
        int endMarker = s.IndexOf(End, begin >= 0 ? begin : 0);
        if (begin < 0 || endMarker < 0) return null;

        int ifIdx = s.IndexOf("#if false", begin);
        if (ifIdx < 0 || ifIdx > endMarker) return null;

        // start of payload = first newline after "#if false"
        int payloadStart = s.IndexOf('\n', ifIdx);
        if (payloadStart < 0) return null;
        payloadStart++; // move past newline

        // wrapper endif is the last "#endif" before End marker
        int endifIdx = s.LastIndexOf("#endif", endMarker);
        if (endifIdx < 0 || endifIdx <= payloadStart) return null;

        var inner = s.Substring(payloadStart, endifIdx - payloadStart);

        // trim a single leading/trailing newline that we added
        inner = inner.Trim('\r', '\n');
        return inner;
    }
}
