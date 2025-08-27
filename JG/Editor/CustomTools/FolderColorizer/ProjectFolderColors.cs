#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectFolderColors
{
    [Serializable]
    public class FolderColorRule
    {
        public DefaultAsset folder;                 // Assign via object field
        public string folderPath = "Assets";        // Normalized "Assets/..." path
        public Color baseColor = new Color(0.20f, 0.60f, 1f, 0.15f);
    }

    // Stored under ProjectSettings (kept out of Assets/)
    [FilePath("ProjectSettings/FolderColorsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class FolderColorsSettings : ScriptableSingleton<FolderColorsSettings>
    {
        public bool enabled = true;

        // When false, selected rows are left un-tinted so Unity's selection highlight stays clear.
        public bool drawOnSelected = false;

        [Range(0f, 0.8f)] public float subfolderLighten = 0.25f;
        [Range(0.2f, 1f)] public float subfolderAlphaMultiplier = 0.75f;

        public List<FolderColorRule> rules = new List<FolderColorRule>();

        void OnEnable()
        {
            // Unity 6.x: DO NOT use HideAndDontSave (includes NotEditable which greys out the inspector)
            hideFlags = HideFlags.DontSaveInEditor;
            if (rules == null) rules = new List<FolderColorRule>();
        }

        public void SaveSettings() => Save(true);
    }

    [InitializeOnLoad]
    public static class FolderColorizer
    {
        static readonly Dictionary<string, Color> rootColorByPath =
            new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        static string[] cachedSelectionGuids = Array.Empty<string>();

        static FolderColorizer()
        {
            BuildCache();
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            Selection.selectionChanged += () =>
            {
                cachedSelectionGuids = Selection.assetGUIDs ?? Array.Empty<string>();
            };
            EditorApplication.delayCall += EditorApplication.RepaintProjectWindow;
        }

        internal static void BuildCache()
        {
            rootColorByPath.Clear();

            var s = FolderColorsSettings.instance;
            if (s.rules == null) s.rules = new List<FolderColorRule>();

            foreach (var r in s.rules)
            {
                if (r == null) continue;

                var path = r.folder ? AssetDatabase.GetAssetPath(r.folder) : r.folderPath;
                path = NormalizePath(path);
                if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) continue;

                // Keep both fields in sync for reliability
                r.folderPath = path;
                if (r.folder == null)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                    if (obj) r.folder = obj;
                }

                rootColorByPath[path] = r.baseColor; // last rule wins
            }
        }

        static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            var s = FolderColorsSettings.instance;
            if (!s.enabled) return;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) return;

            if (!s.drawOnSelected && IsSelected(guid)) return;

            var (hasColor, color, isRoot) = GetColorForPath(path, s);
            if (!hasColor || Event.current.type != EventType.Repaint) return;

            // full row background
            var rowRect = selectionRect;
            rowRect.x = 0f;
            rowRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(rowRect, color);

            // small left stripe to hint hierarchy
            var stripe = new Rect(0f, selectionRect.y, 4f, selectionRect.height);
            EditorGUI.DrawRect(stripe, isRoot ? color : Darken(color, 0.08f));
        }

        static bool IsSelected(string guid)
        {
            var arr = cachedSelectionGuids;
            if (arr == null || arr.Length == 0) return false;
            for (int i = 0; i < arr.Length; i++) if (arr[i] == guid) return true;
            return false;
        }

        static (bool ok, Color color, bool isRoot) GetColorForPath(string folderPath, FolderColorsSettings s)
        {
            folderPath = NormalizePath(folderPath);
            if (rootColorByPath.TryGetValue(folderPath, out var baseCol))
                return (true, baseCol, true);

            string best = null;
            Color bestCol = default;
            int bestLen = -1;
            foreach (var kv in rootColorByPath)
            {
                var root = kv.Key;
                if (folderPath.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase) && root.Length > bestLen)
                { bestLen = root.Length; best = root; bestCol = kv.Value; }
            }

            if (best != null)
            {
                var subCol = LightenForSubfolder(bestCol, s.subfolderLighten, s.subfolderAlphaMultiplier);
                return (true, subCol, false);
            }

            return (false, default, false);
        }

        internal static string NormalizePath(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return null;
            p = p.Replace('\\', '/').Trim();
            if (p.EndsWith("/")) p = p.TrimEnd('/');
            return p;
        }

        static Color LightenForSubfolder(Color c, float lighten, float alphaMul)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            v = Mathf.Clamp01(v + Mathf.Abs(lighten));
            s = Mathf.Clamp01(s * (1f - 0.15f * Mathf.Clamp01(lighten)));
            var outC = Color.HSVToRGB(h, s, v);
            outC.a = Mathf.Clamp01(c.a * Mathf.Clamp01(alphaMul));
            return outC;
        }

        static Color Darken(Color c, float amount)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            v = Mathf.Clamp01(v - Mathf.Abs(amount));
            var d = Color.HSVToRGB(h, s, v);
            d.a = c.a;
            return d;
        }

        [MenuItem("Assets/Folder Colors/Add Selected Folder", priority = 2200)]
        static void AddSelectedFolder()
        {
            var sel = Selection.activeObject as DefaultAsset;
            var path = sel ? AssetDatabase.GetAssetPath(sel) : null;
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            {
                EditorUtility.DisplayDialog("Folder Colors", "Please select a folder in the Project window.", "OK");
                return;
            }

            var s = FolderColorsSettings.instance;
            if (s.rules == null) s.rules = new List<FolderColorRule>();

            s.rules.Add(new FolderColorRule
            {
                folder = sel,
                folderPath = NormalizePath(path),
                baseColor = new Color(0.20f, 0.60f, 1f, 0.15f)
            });

            s.SaveSettings();
            BuildCache();
            EditorApplication.RepaintProjectWindow();
            SettingsService.OpenProjectSettings("Project/Folder Colors");
        }

        [MenuItem("Window/Folder Colors/Project Settings...", priority = 2010)]
        static void OpenSettings() => SettingsService.OpenProjectSettings("Project/Folder Colors");
    }

    // Settings UI (IMGUI) — streamlined, auto-save, no extra buttons
    public class FolderColorsSettingsProvider : SettingsProvider
    {
        SerializedObject so;
        SerializedProperty enabledProp;
        SerializedProperty drawOnSelectedProp;
        SerializedProperty lightenProp;
        SerializedProperty alphaMulProp;
        SerializedProperty rulesProp;

        public FolderColorsSettingsProvider(string path, SettingsScope scope) : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var s = FolderColorsSettings.instance;

            // Heal legacy NotEditable bit if present (Unity 6.x)
            s.hideFlags &= ~HideFlags.NotEditable;

            if (s.rules == null) s.rules = new List<FolderColorRule>();
            so = new SerializedObject(s);
            enabledProp = so.FindProperty("enabled");
            drawOnSelectedProp = so.FindProperty("drawOnSelected");
            lightenProp = so.FindProperty("subfolderLighten");
            alphaMulProp = so.FindProperty("subfolderAlphaMultiplier");
            rulesProp = so.FindProperty("rules");
        }

        static void ApplyAndRefresh(SerializedObject so)
        {
            so.ApplyModifiedProperties();
            FolderColorsSettings.instance.SaveSettings();
            FolderColorizer.BuildCache();
            EditorApplication.RepaintProjectWindow();
        }

        public override void OnGUI(string searchContext)
        {
            if (so == null || so.targetObject == null) OnActivate(searchContext, null);
            so.Update();

            EditorGUILayout.LabelField("Folder Colors", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Changes are saved automatically.", MessageType.None);

            EditorGUILayout.PropertyField(enabledProp, new GUIContent("Enabled"));
            EditorGUILayout.PropertyField(drawOnSelectedProp, new GUIContent("Tint Selected Rows"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Subfolder Appearance", EditorStyles.miniBoldLabel);
            EditorGUILayout.Slider(lightenProp, 0f, 0.8f, new GUIContent("Lighten Amount"));
            EditorGUILayout.Slider(alphaMulProp, 0.2f, 1f, new GUIContent("Alpha Multiplier"));

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Rules", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Rule from Selection", GUILayout.Width(190)))
                {
                    if (rulesProp != null && rulesProp.isArray)
                    {
                        int idx = rulesProp.arraySize;
                        rulesProp.arraySize = idx + 1;
                        var elem = rulesProp.GetArrayElementAtIndex(idx);

                        // Seed from current selection (fallback to Assets)
                        var selected = Selection.activeObject as DefaultAsset;
                        string selectedPath = selected ? AssetDatabase.GetAssetPath(selected) : null;
                        if (string.IsNullOrEmpty(selectedPath) || !AssetDatabase.IsValidFolder(selectedPath))
                        {
                            selected = null;
                            selectedPath = "Assets";
                        }

                        elem.FindPropertyRelative("folder").objectReferenceValue = selected;
                        elem.FindPropertyRelative("folderPath").stringValue = FolderColorizer.NormalizePath(selectedPath);
                        elem.FindPropertyRelative("baseColor").colorValue = new Color(0.20f, 0.60f, 1f, 0.15f);

                        ApplyAndRefresh(so);
                    }
                }
            }

            EditorGUILayout.HelpBox("Each rule tints a folder. Subfolders inherit a lighter, slightly more transparent color.", MessageType.Info);

            if (rulesProp != null && rulesProp.isArray)
            {
                for (int i = 0; i < rulesProp.arraySize; i++)
                {
                    var e = rulesProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginVertical("box");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"Rule {i + 1}", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            rulesProp.DeleteArrayElementAtIndex(i);
                            ApplyAndRefresh(so);
                            EditorGUILayout.EndVertical();
                            break;
                        }
                    }

                    var folderProp = e.FindPropertyRelative("folder");
                    var pathProp = e.FindPropertyRelative("folderPath");
                    var colorProp = e.FindPropertyRelative("baseColor");

                    // Folder object & color (apply immediately)
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(folderProp, new GUIContent("Folder"));
                    EditorGUILayout.PropertyField(colorProp, new GUIContent("Base Color"));
                    if (EditorGUI.EndChangeCheck())
                        ApplyAndRefresh(so);

                    // Manual path + Use Selected helper
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    string typed = EditorGUILayout.DelayedTextField("Folder Path", pathProp.stringValue);
                    if (GUILayout.Button("Use Selected", GUILayout.Width(110)))
                    {
                        var sel = Selection.activeObject as DefaultAsset;
                        string p = sel ? AssetDatabase.GetAssetPath(sel) : null;
                        if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
                        {
                            p = FolderColorizer.NormalizePath(p);
                            pathProp.stringValue = p;
                            folderProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<DefaultAsset>(p);
                            ApplyAndRefresh(so);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Folder Colors", "Please select a folder in the Project window.", "OK");
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (EditorGUI.EndChangeCheck())
                    {
                        string normalized = FolderColorizer.NormalizePath(typed);
                        if (!string.IsNullOrEmpty(normalized) && AssetDatabase.IsValidFolder(normalized))
                        {
                            pathProp.stringValue = normalized;
                            var obj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(normalized);
                            folderProp.objectReferenceValue = obj;
                            ApplyAndRefresh(so);
                        }
                        else if (!string.IsNullOrEmpty(typed))
                        {
                            EditorGUILayout.HelpBox("Invalid path. Use a folder under 'Assets/...'", MessageType.Warning);
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            // Final auto-apply in case anything else changed
            if (so.ApplyModifiedProperties())
            {
                FolderColorsSettings.instance.SaveSettings();
                FolderColorizer.BuildCache();
                EditorApplication.RepaintProjectWindow();
            }
        }

        [SettingsProvider]
        public static SettingsProvider Create() =>
            new FolderColorsSettingsProvider("Project/Folder Colors", SettingsScope.Project)
            {
                keywords = new HashSet<string>(new[] { "folder", "color", "project", "subfolder", "tint" })
            };
    }
}
#endif
