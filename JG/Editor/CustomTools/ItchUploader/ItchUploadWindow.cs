using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JG.Tools.Itch
{
    /// <summary>
    /// Builds the project for WebGL and pushes it to itch.io via butler.
    /// Configure once per project under Tools > Itch.io Upload.
    /// </summary>
    public class ItchUploadWindow : EditorWindow
    {
        Vector2 scroll;
        GUIStyle headerStyle;

        [MenuItem("Tools/Itch.io Upload")]
        static void Open()
        {
            var win = GetWindow<ItchUploadWindow>("Itch.io Upload");
            win.minSize = new Vector2(420f, 360f);
        }

        void OnEnable() => ButlerRunner.Exited += OnButlerExited;
        void OnDisable() => ButlerRunner.Exited -= OnButlerExited;
        void OnButlerExited(int code) => Repaint();

        void OnGUI()
        {
            headerStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, margin = new RectOffset(0, 0, 8, 4) };

            var settings = ItchUploadSettings.instance;
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Project", headerStyle);
            settings.projectUrl = EditorGUILayout.TextField(new GUIContent("Itch project URL", "https://user.itch.io/game or user/game"), settings.projectUrl);
            settings.channel = EditorGUILayout.TextField("Channel", settings.channel);

            var hasTarget = settings.TryGetTarget(out var target);
            if (hasTarget)
                EditorGUILayout.LabelField("Butler target", $"{target}:{settings.channel}");
            else
                EditorGUILayout.HelpBox("Enter the itch.io project URL, e.g. https://user.itch.io/game", MessageType.Warning);

            GUILayout.Label("Build", headerStyle);
            EditorGUILayout.BeginHorizontal();
            settings.buildPath = EditorGUILayout.TextField("WebGL build path", settings.buildPath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                var picked = EditorUtility.OpenFolderPanel("WebGL build folder", ProjectRoot, "");
                if (!string.IsNullOrEmpty(picked))
                    settings.buildPath = MakeRelative(picked);
            }
            EditorGUILayout.EndHorizontal();
            settings.useBundleVersion = EditorGUILayout.Toggle(new GUIContent("Use bundle version", "Pass PlayerSettings.bundleVersion as --userversion"), settings.useBundleVersion);
            EditorGUILayout.LabelField("Version", PlayerSettings.bundleVersion);

            GUILayout.Label("Butler", headerStyle);
            EditorGUILayout.BeginHorizontal();
            settings.butlerPath = EditorGUILayout.TextField("Butler path", settings.butlerPath);
            if (GUILayout.Button("Detect", GUILayout.Width(60)))
            {
                var found = ButlerRunner.Resolve(settings.butlerPath) ?? ButlerRunner.Resolve("butler");
                if (found != null) settings.butlerPath = found;
                else Debug.LogWarning("[Itch] butler not found in PATH or common install locations.");
            }
            EditorGUILayout.EndHorizontal();
            settings.extraArgs = EditorGUILayout.TextField(new GUIContent("Extra args", "Appended to 'butler push'"), settings.extraArgs);

            if (EditorGUI.EndChangeCheck())
                settings.SaveSettings();

            DrawWarnings(settings);
            DrawButtons(settings, hasTarget, target);

            EditorGUILayout.EndScrollView();
        }

        void DrawWarnings(ItchUploadSettings settings)
        {
            if (ButlerRunner.Resolve(settings.butlerPath) == null)
                EditorGUILayout.HelpBox("butler not found. Install from https://itch.io/docs/butler/ or press Detect.", MessageType.Error);
            else if (!ButlerRunner.HasCredentials())
                EditorGUILayout.HelpBox("butler is not logged in. Run `butler login` in a terminal once.", MessageType.Warning);

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                EditorGUILayout.HelpBox("WebGL build support is not installed for this editor.", MessageType.Error);

            if (PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Brotli && !PlayerSettings.WebGL.decompressionFallback)
                EditorGUILayout.HelpBox("Brotli compression without decompression fallback will not load on itch.io. Use Gzip/Disabled or enable Decompression Fallback.", MessageType.Warning);

            if (ButlerRunner.IsRunning)
                EditorGUILayout.HelpBox($"Running: {ButlerRunner.LastCommand}\nSee the Console for output.", MessageType.Info);
        }

        void DrawButtons(ItchUploadSettings settings, bool hasTarget, string target)
        {
            var buildExists = Directory.Exists(AbsoluteBuildPath(settings)) && File.Exists(Path.Combine(AbsoluteBuildPath(settings), "index.html"));

            GUILayout.Space(8);
            using (new EditorGUI.DisabledScope(ButlerRunner.IsRunning))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Build WebGL", GUILayout.Height(28)))
                    BuildWebGL(settings);

                using (new EditorGUI.DisabledScope(!hasTarget || !buildExists))
                    if (GUILayout.Button("Upload", GUILayout.Height(28)))
                        Push(settings, target);

                using (new EditorGUI.DisabledScope(!hasTarget))
                    if (GUILayout.Button("Build & Upload", GUILayout.Height(28)))
                        if (BuildWebGL(settings))
                            Push(settings, target);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(!hasTarget))
                    if (GUILayout.Button("Status"))
                        ButlerRunner.Run(settings.butlerPath, $"status {target}:{settings.channel}");
                if (GUILayout.Button("butler version"))
                    ButlerRunner.Run(settings.butlerPath, "version");
                using (new EditorGUI.DisabledScope(!buildExists))
                    if (GUILayout.Button("Show build"))
                        EditorUtility.RevealInFinder(Path.Combine(AbsoluteBuildPath(settings), "index.html"));
                using (new EditorGUI.DisabledScope(!hasTarget))
                    if (GUILayout.Button("Open page"))
                        Application.OpenURL(settings.GetPageUrl());
                EditorGUILayout.EndHorizontal();
            }

            if (!buildExists)
                EditorGUILayout.HelpBox($"No WebGL build found at {settings.buildPath}. Build first.", MessageType.None);
        }

        const int MaxItchFiles = 1000;

        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        static string AbsoluteBuildPath(ItchUploadSettings settings)
        {
            var p = string.IsNullOrEmpty(settings.buildPath) ? "Builds/WebGL" : settings.buildPath;
            return Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(ProjectRoot, p));
        }

        static string MakeRelative(string absolute)
        {
            var root = ProjectRoot.Replace('\\', '/').TrimEnd('/') + "/";
            absolute = absolute.Replace('\\', '/');
            return absolute.StartsWith(root) ? absolute.Substring(root.Length) : absolute;
        }

        static bool BuildWebGL(ItchUploadSettings settings)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Itch] No scenes enabled in Build Settings.");
                return false;
            }

            var outPath = AbsoluteBuildPath(settings);
            Directory.CreateDirectory(outPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            Debug.Log($"[Itch] Building WebGL to {outPath} ...");
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Itch] WebGL build succeeded ({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalSeconds:F0}s).");
                return true;
            }

            Debug.LogError($"[Itch] WebGL build failed: {summary.result} ({summary.totalErrors} errors).");
            return false;
        }

        static void Push(ItchUploadSettings settings, string target)
        {
            var buildPath = AbsoluteBuildPath(settings);
            if (!File.Exists(Path.Combine(buildPath, "index.html")))
            {
                Debug.LogError($"[Itch] No index.html in {buildPath}. Is this a WebGL build?");
                return;
            }

            // itch.io rejects browser builds with more than 1000 files.
            var fileCount = Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories).Length;
            if (fileCount > MaxItchFiles)
            {
                Debug.LogError($"[Itch] Build folder contains {fileCount} files; itch.io allows at most {MaxItchFiles} for HTML5 games. Remove stale folders from {buildPath}.");
                return;
            }

            var args = $"push {ButlerRunner.Quote(buildPath)} {target}:{settings.channel}";
            if (settings.useBundleVersion && !string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
                args += $" --userversion {ButlerRunner.Quote(PlayerSettings.bundleVersion.Trim())}";
            if (!string.IsNullOrWhiteSpace(settings.extraArgs))
                args += " " + settings.extraArgs.Trim();

            ButlerRunner.Run(settings.butlerPath, args);
        }
    }
}
