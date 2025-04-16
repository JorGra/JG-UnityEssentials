using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class CustomBuildWindow : EditorWindow
{
    private enum VersionIncrement
    {
        Build,
        Minor,
        Major
    }

    private VersionIncrement versionIncrement = VersionIncrement.Build;
    private readonly string[] developmentStages = { "Pre-Alpha", "Alpha", "Beta", "Release" };
    private int selectedDevelopmentStageIndex;

    private int major;
    private int minor;
    private int build;

    private string customVersion = "";

    // Toggle for enabling/disabling version increment
    private bool enableVersionIncrement = true;

    // Android-specific bundle version code
    private int androidBundleVersionCode = 1;
    private bool incrementAndroidBundleVersion = false;

    // iOS-specific build number
    private int iosBuildNumber = 1;
    private bool incrementIOSBuildNumber = false;

    // We capture the "current" full version string here (e.g. "beta-0.8.12" or "1.2.3")
    private string currentFullVersion = "";

    // The BuildPlayerOptions from the normal Build dialog
    private static BuildPlayerOptions cachedBuildPlayerOptions;

    // Scroll position for the window
    private Vector2 scrollPosition;

    // Style variables
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle boxStyle;

    [InitializeOnLoadMethod]
    private static void Init()
    {
        // Intercept the normal build button in the Build Settings window
        BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
    }

    private static void BuildPlayerHandler(BuildPlayerOptions options)
    {
        cachedBuildPlayerOptions = options;
        var window = GetWindow<CustomBuildWindow>("Build Version");
        window.minSize = new Vector2(450, 500);
        window.Show();
    }

    private void OnEnable()
    {
        LoadCurrentVersion();
    }

    private void InitStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.margin = new RectOffset(0, 0, 10, 5);
        }

        if (subHeaderStyle == null)
        {
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            subHeaderStyle.margin = new RectOffset(0, 0, 6, 2);
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(EditorStyles.helpBox);
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
            boxStyle.margin = new RectOffset(0, 0, 8, 8);
        }
    }

    private void OnGUI()
    {
        InitStyles();

        // Begin scrollable area
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Use larger, prettier sections
        DrawCurrentVersionSection();
        DrawVersionIncrementToggleSection();
        DrawNextVersionPreviewSection();
        DrawVersionIncrementOptionsSection();
        DrawPlatformSpecificSection();
        DrawBuildButtonSection();

        // End scrollable area
        EditorGUILayout.EndScrollView();
    }

    private void DrawCurrentVersionSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        {
            GUILayout.Label("Current Version", headerStyle);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Version:", currentFullVersion);
                EditorGUILayout.LabelField("iOS Build:", iosBuildNumber.ToString());
                EditorGUILayout.LabelField("Android Bundle Version:", androidBundleVersionCode.ToString());
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawVersionIncrementToggleSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        {
            GUILayout.Label("Version Settings", headerStyle);

            // Put the toggle in a nice box
            enableVersionIncrement = EditorGUILayout.ToggleLeft(
                " Enable Version Increment",
                enableVersionIncrement,
                EditorStyles.boldLabel
            );
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawNextVersionPreviewSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        {
            GUILayout.Label("Next Version Preview", headerStyle);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField(GetNextVersionPreview(), EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawVersionIncrementOptionsSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        {
            GUILayout.Label("Version Configuration", headerStyle);

            GUI.enabled = enableVersionIncrement;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Increment Type", subHeaderStyle);
                versionIncrement = (VersionIncrement)EditorGUILayout.EnumPopup("Increment:", versionIncrement);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Version Numbers", subHeaderStyle);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Major:", GUILayout.Width(60));
                    major = EditorGUILayout.IntField(major, GUILayout.Width(50));
                    EditorGUILayout.LabelField("Minor:", GUILayout.Width(60));
                    minor = EditorGUILayout.IntField(minor, GUILayout.Width(50));
                    EditorGUILayout.LabelField("Build:", GUILayout.Width(60));
                    build = EditorGUILayout.IntField(build, GUILayout.Width(50));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Development Stage", subHeaderStyle);
                selectedDevelopmentStageIndex = EditorGUILayout.Popup("Stage:", selectedDevelopmentStageIndex, developmentStages);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Custom Version (Optional)", subHeaderStyle);
                customVersion = EditorGUILayout.TextField("Custom Version:", customVersion);
            }
            EditorGUILayout.EndVertical();

            GUI.enabled = true;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPlatformSpecificSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        {
            GUILayout.Label("Platform-Specific Settings", headerStyle);

            DrawAndroidSection();
            DrawIOSSection();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAndroidSection()
    {
        // Determine if this section should be enabled
        bool isEnabled = enableVersionIncrement;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            GUILayout.Label("Android Settings", subHeaderStyle);

            GUI.enabled = isEnabled;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Bundle Version Code:", GUILayout.Width(150));
                androidBundleVersionCode = EditorGUILayout.IntField(androidBundleVersionCode);
            }
            EditorGUILayout.EndHorizontal();

            incrementAndroidBundleVersion = EditorGUILayout.ToggleLeft(" Increment Android Bundle Version", incrementAndroidBundleVersion);

            GUI.enabled = true;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawIOSSection()
    {
        // Determine if this section should be enabled
        bool isEnabled = enableVersionIncrement;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            GUILayout.Label("iOS Settings", subHeaderStyle);

            GUI.enabled = isEnabled;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("iOS Build Number:", GUILayout.Width(150));
                iosBuildNumber = EditorGUILayout.IntField(iosBuildNumber);
            }
            EditorGUILayout.EndHorizontal();

            incrementIOSBuildNumber = EditorGUILayout.ToggleLeft(" Increment iOS Build Number", incrementIOSBuildNumber);

            GUI.enabled = true;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawBuildButtonSection()
    {
        EditorGUILayout.Space(10);

        // Make a nice big build button
        GUI.backgroundColor = new Color(0.6f, 0.8f, 0.6f, 1.0f);
        if (GUILayout.Button("Build", GUILayout.Height(40)))
        {
            // 1. Update the main version string for the chosen target (if enabled)
            if (enableVersionIncrement)
            {
                UpdateVersion();

                // 2. If the active build target is Android, also update the Android bundle version if toggled
                if (cachedBuildPlayerOptions.target == BuildTarget.Android && incrementAndroidBundleVersion)
                {
                    IncrementAndroidBundleVersion();
                }

                // 3. If the active build target is iOS, also update the iOS build number if toggled
                if (cachedBuildPlayerOptions.target == BuildTarget.iOS && incrementIOSBuildNumber)
                {
                    IncrementIOSBuildNumber();
                }
            }
            else
            {
                Debug.Log("Version increment disabled. Building with current version: " + currentFullVersion);
            }

            // 4. Build
            BuildProject();
            Close();
        }
        GUI.backgroundColor = Color.white;
    }

    /// <summary>
    /// Loads the current version info (bundleVersion, iOS buildNumber, Android bundleVersionCode).
    /// </summary>
    private void LoadCurrentVersion()
    {
        // 1. Load the current bundleVersion (e.g., "beta-0.8.12")
        string currentVersion = PlayerSettings.bundleVersion;
        currentFullVersion = currentVersion;

        // 2. Strip development stage if present
        string versionWithoutStage = currentVersion;
        for (int i = 0; i < developmentStages.Length; i++)
        {
            string stage = developmentStages[i];
            string prefix = stage.ToLower().Replace(" ", "-") + "-";
            if (currentVersion.StartsWith(prefix))
            {
                versionWithoutStage = currentVersion.Substring(prefix.Length);
                selectedDevelopmentStageIndex = i;
                break;
            }
        }

        // 3. Parse the version numbers
        string[] versionParts = versionWithoutStage.Split('.');
        if (versionParts.Length >= 3)
        {
            int.TryParse(versionParts[0], out major);
            int.TryParse(versionParts[1], out minor);
            int.TryParse(versionParts[2], out build);
        }
        else
        {
            // Fallback if the bundleVersion doesn't have 3 parts
            major = 1;
            minor = 0;
            build = 0;
        }

        // 4. Load the current Android bundle version code
        androidBundleVersionCode = PlayerSettings.Android.bundleVersionCode;

        // 5. Load the current iOS build number
        if (!int.TryParse(PlayerSettings.iOS.buildNumber, out iosBuildNumber))
        {
            iosBuildNumber = 1;
        }
    }

    /// <summary>
    /// Builds a multi-line string showing how both iOS and Android versions would change if toggles are on.
    /// Now also shows version for other platforms.
    /// </summary>
    private string GetNextVersionPreview()
    {
        // If version increment is disabled, show that versions will remain the same
        if (!enableVersionIncrement)
        {
            return "Version increment is disabled. Current versions will be maintained.";
        }

        //
        // 1. Copy the current values so we can simulate increments
        //
        int oldMajor = major;
        int oldMinor = minor;
        int oldBuild = build;

        int oldIOSBuildNumber = iosBuildNumber;
        int oldAndroidBundleVersion = androidBundleVersionCode;

        // We'll make separate preview copies
        int previewMajor = oldMajor;
        int previewMinor = oldMinor;
        int previewBuild = oldBuild;
        int previewIOSBuildNumber = oldIOSBuildNumber;
        int previewAndroidBundleVersion = oldAndroidBundleVersion;

        //
        // 2. Apply the chosen version increment (Build/Minor/Major) to our preview
        //
        switch (versionIncrement)
        {
            case VersionIncrement.Build:
                previewBuild++;
                break;
            case VersionIncrement.Minor:
                previewMinor++;
                previewBuild = 0;
                break;
            case VersionIncrement.Major:
                previewMajor++;
                previewMinor = 0;
                previewBuild = 0;
                break;
        }

        //
        // 3. If user typed a custom version, parse that instead
        //
        if (!string.IsNullOrEmpty(customVersion))
        {
            string[] versionParts = customVersion.Split('.');
            if (versionParts.Length >= 3)
            {
                int.TryParse(versionParts[0], out previewMajor);
                int.TryParse(versionParts[1], out previewMinor);
                int.TryParse(versionParts[2], out previewBuild);
            }
        }

        //
        // 4. If toggles are on, simulate increments
        //
        if (incrementIOSBuildNumber)
        {
            previewIOSBuildNumber++;
        }
        if (incrementAndroidBundleVersion)
        {
            previewAndroidBundleVersion++;
        }

        //
        // 5. Create the "old" vs "new" version strings for iOS, Android, and other platforms
        //
        string oldStage = developmentStages[selectedDevelopmentStageIndex].ToLower().Replace(" ", "-");
        string newStage = developmentStages[selectedDevelopmentStageIndex].ToLower().Replace(" ", "-");

        // Old iOS version is always stage-less
        string oldIOSVersion = $"{oldMajor}.{oldMinor}.{oldBuild}";

        // New iOS version is also stage-less
        string newIOSVersion = $"{previewMajor}.{previewMinor}.{previewBuild}";

        // Old Android version includes stage
        string oldAndroidVersion = $"{oldStage}-{oldMajor}.{oldMinor}.{oldBuild}";

        // New Android version includes stage
        string newAndroidVersion = $"{newStage}-{previewMajor}.{previewMinor}.{previewBuild}";

        // Other platforms (like Windows, WebGL, etc.)
        string oldOtherVersion = $"{oldStage}-{oldMajor}.{oldMinor}.{oldBuild}";
        string newOtherVersion = $"{newStage}-{previewMajor}.{previewMinor}.{previewBuild}";

        //
        // 6. Build the multi-line preview
        //
        StringBuilder preview = new StringBuilder();

        // Get current build target to highlight active platform
        BuildTarget currentTarget = cachedBuildPlayerOptions.target;

        // iOS Preview
        preview.AppendLine("--- iOS ---");
        if (currentTarget == BuildTarget.iOS)
            preview.AppendLine("(ACTIVE BUILD TARGET)");
        preview.AppendLine($"Version: {oldIOSVersion} → {newIOSVersion}");
        preview.AppendLine($"Build Number: {oldIOSBuildNumber} → {previewIOSBuildNumber}");
        preview.AppendLine();

        // Android Preview
        preview.AppendLine("--- Android ---");
        if (currentTarget == BuildTarget.Android)
            preview.AppendLine("(ACTIVE BUILD TARGET)");
        preview.AppendLine($"Version: {oldAndroidVersion} → {newAndroidVersion}");
        preview.AppendLine($"Bundle Version Code: {oldAndroidBundleVersion} → {previewAndroidBundleVersion}");
        preview.AppendLine();

        // Other Platforms Preview (Windows, MacOS, WebGL, etc.)
        preview.AppendLine("--- Other Platforms ---");
        if (currentTarget != BuildTarget.iOS && currentTarget != BuildTarget.Android)
            preview.AppendLine("(ACTIVE BUILD TARGET)");
        preview.AppendLine($"Version: {oldOtherVersion} → {newOtherVersion}");

        return preview.ToString();
    }

    /// <summary>
    /// Updates the main PlayerSettings.bundleVersion based on the current target.
    /// iOS gets stage-less version; other platforms get stage prefix.
    /// </summary>
    private void UpdateVersion()
    {
        // 1. Increment version parts
        switch (versionIncrement)
        {
            case VersionIncrement.Build:
                build++;
                break;
            case VersionIncrement.Minor:
                minor++;
                build = 0;
                break;
            case VersionIncrement.Major:
                major++;
                minor = 0;
                build = 0;
                break;
        }

        // 2. If custom version is set, override
        if (!string.IsNullOrEmpty(customVersion))
        {
            string[] versionParts = customVersion.Split('.');
            if (versionParts.Length >= 3)
            {
                int.TryParse(versionParts[0], out major);
                int.TryParse(versionParts[1], out minor);
                int.TryParse(versionParts[2], out build);
            }
            else
            {
                Debug.LogWarning("Custom version format is incorrect. Please use 'Major.Minor.Build'");
            }
        }

        // 3. Determine if the *actual build target* is iOS or something else
        bool isIOS = (cachedBuildPlayerOptions.target == BuildTarget.iOS);

        // iOS: no stage prefix
        // Others (Android, Windows, etc.): stage prefix
        string newVersion;
        if (isIOS)
        {
            newVersion = $"{major}.{minor}.{build}";
        }
        else
        {
            string stage = developmentStages[selectedDevelopmentStageIndex].ToLower().Replace(" ", "-");
            newVersion = $"{stage}-{major}.{minor}.{build}";
        }

        PlayerSettings.bundleVersion = newVersion;
        Debug.Log($"Updated bundleVersion to: {newVersion}");
    }

    private void IncrementAndroidBundleVersion()
    {
        androidBundleVersionCode++;
        PlayerSettings.Android.bundleVersionCode = androidBundleVersionCode;
        Debug.Log($"Updated Android bundle version code to: {androidBundleVersionCode}");
    }

    private void IncrementIOSBuildNumber()
    {
        iosBuildNumber++;
        PlayerSettings.iOS.buildNumber = iosBuildNumber.ToString();
        Debug.Log($"Updated iOS build number to: {iosBuildNumber}");
    }

    private void BuildProject()
    {
        if (string.IsNullOrEmpty(cachedBuildPlayerOptions.locationPathName))
        {
            string defaultName = PlayerSettings.productName;
            string extension = GetBuildExtension(cachedBuildPlayerOptions.target);
            string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Build canceled: No location selected.");
                return;
            }
            cachedBuildPlayerOptions.locationPathName = $"{path}/{defaultName}{extension}";
        }

        BuildReport report = BuildPipeline.BuildPlayer(cachedBuildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed");
        }
    }

    private string GetBuildExtension(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return ".exe";
            case BuildTarget.Android:
                return ".apk";
            case BuildTarget.iOS:
                return "";
            case BuildTarget.WebGL:
                return "";
            default:
                return "";
        }
    }
}