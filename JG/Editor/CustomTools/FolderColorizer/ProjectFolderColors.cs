#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectFolderColors
{
    public enum TintFillMode { Solid, EdgeGradient }
    public enum EdgeOrientation { BothSides, LeftOnly, RightOnly }

    [Serializable]
    public class FolderColorRule
    {
        // Store only serializable, path-based data
        public string folderPath = "Assets";        // Normalized "Assets/..." path
        public Color baseColor = new Color(0.20f, 0.60f, 1f, 0.45f);
    }

    // -------------------------------------------------------------------------
    // SETTINGS (custom JSON persistence under ProjectSettings/)
    // -------------------------------------------------------------------------
    public sealed class FolderColorsSettings : ScriptableObject
    {
        // ---- persisted data ----
        public bool enabled = true;
        public bool drawOnSelected = false;

        [Range(0f, 0.8f)] public float subfolderLighten = 0.25f;
        [Range(0.2f, 1f)] public float subfolderAlphaMultiplier = 0.75f;

        public TintFillMode fillMode = TintFillMode.EdgeGradient;
        [Range(0.05f, 0.9f)] public float edgeWidthFraction = 0.18f;
        [Range(0f, 1f)] public float edgeFeather = 0.70f;
        public EdgeOrientation edgeOrientation = EdgeOrientation.BothSides;
        [Range(-0.4f, 0.4f)] public float centerOffset = 0f;

        public List<FolderColorRule> rules = new List<FolderColorRule>();

        // ---- singleton facade (keeps the old API) ----
        static FolderColorsSettings _instance;
        public static FolderColorsSettings instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = CreateInstance<FolderColorsSettings>();
                    LoadFromDisk(_instance); // fill from JSON if present
                }
                return _instance;
            }
        }

        // Force-load early so other static ctors can read settings safely
        [InitializeOnLoadMethod]
        static void EnsureLoadedEarly() => _ = instance;

        // ---- persistence paths ----
        const string k_RelativePath = "ProjectSettings/FolderColorsSettings.json";
        public static string FilePath => k_RelativePath; // project-relative (for logs/printing)
        public static string AbsoluteFilePath
        {
            get
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                return Path.Combine(projectRoot, k_RelativePath);
            }
        }

        // ---- save/load ----
        public void SaveSettings()
        {
            var path = AbsoluteFilePath;
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Use EditorJsonUtility so Unity types (Color, etc.) serialize correctly.
            var json = EditorJsonUtility.ToJson(this, prettyPrint: true);
            File.WriteAllText(path, json);
            // Not an imported asset: no AssetDatabase.SaveAssets() or SetDirty needed.
        }

        static void LoadFromDisk(FolderColorsSettings target)
        {
            try
            {
                var path = AbsoluteFilePath;
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                        EditorJsonUtility.FromJsonOverwrite(json, target);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"FolderColorsSettings: failed to load settings: {e.Message}");
            }

            if (target.rules == null) target.rules = new List<FolderColorRule>();
        }
    }

    // -------------------------------------------------------------------------
    // RENDERER / MENU HOOKS
    // -------------------------------------------------------------------------
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
                var path = NormalizePath(r.folderPath);
                if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) continue;

                // keep normalized path stored back
                r.folderPath = path;

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
            if (Event.current.type != EventType.Repaint) return;

            var (hasColor, color, isRoot) = GetColorForPath(path, s);
            if (!hasColor) return;

            var rowRect = GetRowRect(selectionRect); // respects right-side detail panel

            if (s.fillMode == TintFillMode.Solid)
            {
                EditorGUI.DrawRect(rowRect, color);

                var stripe = new Rect(0f, selectionRect.y, 4f, selectionRect.height);
                EditorGUI.DrawRect(stripe, isRoot ? color : Darken(color, 0.08f));
            }
            else // EdgeGradient
            {
                var tex = EdgeGradient.Get(
                    s.edgeWidthFraction,
                    s.edgeFeather,
                    s.edgeOrientation,
                    s.edgeOrientation == EdgeOrientation.BothSides ? s.centerOffset : 0f
                );

                var prev = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(rowRect, tex, ScaleMode.StretchToFill, alphaBlend: true);
                GUI.color = prev;
            }
        }

        static Rect GetRowRect(Rect selectionRect)
        {
            var clip = GUIClipUtil.VisibleRect; // (0,0)-(treeWidth,visibleHeight) in the tree’s GUI group
            float width = clip.width > 0f ? clip.width : Mathf.Max(1f, selectionRect.xMax);
            return new Rect(0f, selectionRect.y, width, selectionRect.height);
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
                folderPath = NormalizePath(path),
                baseColor = new Color(0.20f, 0.60f, 1f, 0.15f)
            });

            s.SaveSettings();
            BuildCache();
            EditorApplication.RepaintProjectWindow();
            SettingsService.OpenProjectSettings("Project/Folder Colors");
        }

        [MenuItem("Tools/Folder Colors/Project Settings...", priority = 2010)]
        static void OpenSettings() => SettingsService.OpenProjectSettings("Project/Folder Colors");

        // ---------- Edge Gradient generator ----------
        static class EdgeGradient
        {
            static readonly Dictionary<int, Texture2D> _cache = new Dictionary<int, Texture2D>();

            public static Texture2D Get(float edgeWidthFraction, float feather01, EdgeOrientation orientation, float centerOffset)
            {
                edgeWidthFraction = Mathf.Clamp(edgeWidthFraction, 0.02f, 0.48f);
                feather01 = Mathf.Clamp01(feather01);
                centerOffset = Mathf.Clamp(centerOffset, -0.40f, 0.40f);

                int key = (Mathf.RoundToInt(edgeWidthFraction * 1000f) << 22)
                        | (Mathf.RoundToInt(feather01 * 1000f) << 12)
                        | ((int)orientation << 10)
                        | (Mathf.RoundToInt((centerOffset + 0.5f) * 1023f) & 0x3FF);

                if (_cache.TryGetValue(key, out var tex) && tex) return tex;

                const int W = 256;
                tex = new Texture2D(W, 1, TextureFormat.RGBA32, false, true)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = $"EdgeGrad_w{edgeWidthFraction:0.00}_f{feather01:0.00}_o{orientation}_c{centerOffset:0.00}"
                };

                float w = Mathf.Max(1e-4f, edgeWidthFraction);
                float gamma = Mathf.Lerp(0.6f, 3.0f, feather01);

                float wL = Mathf.Max(1e-4f, orientation == EdgeOrientation.BothSides ? w + centerOffset : w);
                float wR = Mathf.Max(1e-4f, orientation == EdgeOrientation.BothSides ? w - centerOffset : w);

                var cols = new Color32[W];
                for (int x = 0; x < W; x++)
                {
                    float u = x / (W - 1f);
                    float a;

                    switch (orientation)
                    {
                        case EdgeOrientation.LeftOnly:
                            {
                                float t = Mathf.Clamp01(u / wL);
                                a = 1f - SmoothStep01(t);
                                break;
                            }
                        case EdgeOrientation.RightOnly:
                            {
                                float t = Mathf.Clamp01((1f - u) / wR);
                                a = 1f - SmoothStep01(t);
                                break;
                            }
                        default:
                            {
                                float tL = Mathf.Clamp01(u / wL);
                                float tR = Mathf.Clamp01((1f - u) / wR);
                                float aL = 1f - SmoothStep01(tL);
                                float aR = 1f - SmoothStep01(tR);
                                a = Mathf.Max(aL, aR);
                                break;
                            }
                    }

                    a = Mathf.Pow(a, gamma);
                    cols[x] = new Color(1f, 1f, 1f, a);
                }

                tex.SetPixels32(cols);
                tex.Apply(false, true);
                _cache[key] = tex;
                return tex;
            }

            static float SmoothStep01(float x)
            {
                x = Mathf.Clamp01(x);
                return x * x * (3f - 2f * x);
            }
        }

        // ---------- Internal GUIClip helper ----------
        static class GUIClipUtil
        {
            static Func<Rect> _getter;
            public static Rect VisibleRect
            {
                get
                {
                    if (_getter == null)
                    {
                        var asm = typeof(GUI).Assembly; // UnityEngine
                        var t = asm.GetType("UnityEngine.GUIClip");
                        var prop = t?.GetProperty("visibleRect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (prop != null)
                        {
                            var getMethod = prop.GetGetMethod(true);
                            _getter = (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), getMethod);
                        }
                        else
                        {
                            _getter = () => new Rect(0, 0, 0, 0);
                        }
                    }
                    return _getter();
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // SETTINGS UI (IMGUI)
    // -------------------------------------------------------------------------
    public class FolderColorsSettingsProvider : SettingsProvider
    {
        SerializedObject so;
        SerializedProperty enabledProp;
        SerializedProperty drawOnSelectedProp;
        SerializedProperty lightenProp;
        SerializedProperty alphaMulProp;
        SerializedProperty rulesProp;

        SerializedProperty fillModeProp;
        SerializedProperty edgeWidthProp;
        SerializedProperty edgeFeatherProp;
        SerializedProperty edgeOrientationProp;
        SerializedProperty centerOffsetProp;

        public FolderColorsSettingsProvider(string path, SettingsScope scope) : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var s = FolderColorsSettings.instance;

            if (s.rules == null) s.rules = new List<FolderColorRule>();
            so = new SerializedObject(s);

            enabledProp = so.FindProperty("enabled");
            drawOnSelectedProp = so.FindProperty("drawOnSelected");
            lightenProp = so.FindProperty("subfolderLighten");
            alphaMulProp = so.FindProperty("subfolderAlphaMultiplier");
            rulesProp = so.FindProperty("rules");

            fillModeProp = so.FindProperty("fillMode");
            edgeWidthProp = so.FindProperty("edgeWidthFraction");
            edgeFeatherProp = so.FindProperty("edgeFeather");
            edgeOrientationProp = so.FindProperty("edgeOrientation");
            centerOffsetProp = so.FindProperty("centerOffset");
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
            EditorGUILayout.HelpBox("Changes are saved automatically and persist across editor restarts.", MessageType.None);

            // Rendering
            EditorGUILayout.LabelField("Rendering", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(fillModeProp, new GUIContent("Fill Mode"));
            if ((TintFillMode)fillModeProp.enumValueIndex == TintFillMode.EdgeGradient)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Slider(edgeWidthProp, 0.05f, 0.45f, new GUIContent("Edge Width (%)"));
                EditorGUILayout.Slider(edgeFeatherProp, 0f, 1f, new GUIContent("Feather"));
                EditorGUILayout.PropertyField(edgeOrientationProp, new GUIContent("Edge Orientation"));
                if ((EdgeOrientation)edgeOrientationProp.enumValueIndex == EdgeOrientation.BothSides)
                {
                    EditorGUILayout.Slider(centerOffsetProp, -0.4f, 0.4f, new GUIContent("Center Offset (± row width)"));
                    EditorGUILayout.HelpBox("Shifts the most transparent center left/right. Example: 0.12 ≈ 12% to the right.", MessageType.None);
                }
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
                ApplyAndRefresh(so);

            EditorGUILayout.Space(6);

            // General toggles
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(enabledProp, new GUIContent("Enabled"));
            EditorGUILayout.PropertyField(drawOnSelectedProp, new GUIContent("Tint Selected Rows"));
            if (EditorGUI.EndChangeCheck())
                ApplyAndRefresh(so);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Subfolder Appearance", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(lightenProp, 0f, 0.8f, new GUIContent("Lighten Amount"));
            EditorGUILayout.Slider(alphaMulProp, 0.2f, 1f, new GUIContent("Alpha Multiplier"));
            if (EditorGUI.EndChangeCheck())
                ApplyAndRefresh(so);

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
                            selectedPath = "Assets";
                        }

                        elem.FindPropertyRelative("folderPath").stringValue = FolderColorizer.NormalizePath(selectedPath);
                        elem.FindPropertyRelative("baseColor").colorValue = new Color(0.20f, 0.60f, 1f, 0.15f);

                        ApplyAndRefresh(so);
                    }
                }
            }

            EditorGUILayout.HelpBox("Each rule tints a folder. Subfolders inherit a lighter, slightly more transparent color.", MessageType.None);

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

                    var pathProp = e.FindPropertyRelative("folderPath");
                    var colorProp = e.FindPropertyRelative("baseColor");

                    // Convenience: object field derived from path (writes back to path when changed)
                    DefaultAsset currentObj = null;
                    if (!string.IsNullOrEmpty(pathProp.stringValue))
                        currentObj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(pathProp.stringValue);

                    EditorGUI.BeginChangeCheck();
                    var newObj = (DefaultAsset)EditorGUILayout.ObjectField("Folder", currentObj, typeof(DefaultAsset), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        string newPath = newObj ? AssetDatabase.GetAssetPath(newObj) : pathProp.stringValue;
                        if (!string.IsNullOrEmpty(newPath) && AssetDatabase.IsValidFolder(newPath))
                        {
                            pathProp.stringValue = FolderColorizer.NormalizePath(newPath);
                            ApplyAndRefresh(so);
                        }
                    }

                    // Color (apply immediately)
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(colorProp, new GUIContent("Base Color"));
                    if (EditorGUI.EndChangeCheck())
                        ApplyAndRefresh(so);

                    // Manual path + Use Selected helper
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    string typed = EditorGUILayout.DelayedTextField("Folder Path", pathProp.stringValue);
                    bool useSel = GUILayout.Button("Use Selected", GUILayout.Width(110));
                    EditorGUILayout.EndHorizontal();

                    if (EditorGUI.EndChangeCheck())
                    {
                        string normalized = FolderColorizer.NormalizePath(typed);
                        if (!string.IsNullOrEmpty(normalized) && AssetDatabase.IsValidFolder(normalized))
                        {
                            pathProp.stringValue = normalized;
                            ApplyAndRefresh(so);
                        }
                        else if (!string.IsNullOrEmpty(typed))
                        {
                            EditorGUILayout.HelpBox("Invalid path. Use a folder under 'Assets/...'", MessageType.Warning);
                        }
                    }

                    if (useSel)
                    {
                        var sel = Selection.activeObject as DefaultAsset;
                        string p = sel ? AssetDatabase.GetAssetPath(sel) : null;
                        if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
                        {
                            p = FolderColorizer.NormalizePath(p);
                            pathProp.stringValue = p;
                            ApplyAndRefresh(so);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Folder Colors", "Please select a folder in the Project window.", "OK");
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            // Final catch-all apply
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
                keywords = new HashSet<string>(new[] { "folder", "color", "project", "subfolder", "tint", "gradient", "edge", "right", "left", "center" })
            };
    }
}
#endif
