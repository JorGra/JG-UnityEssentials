using System.IO;
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
        window.Show();
    }

    private void OnEnable()
    {
        LoadCurrentVersion();
    }

    private void OnGUI()
    {
        //
        // CURRENT VERSION DISPLAY
        //
        GUILayout.Label("Current Version", EditorStyles.boldLabel);
        GUILayout.Label($"Version: {currentFullVersion}", EditorStyles.wordWrappedLabel);
        GUILayout.Label($"iOS Build: {iosBuildNumber}");
        GUILayout.Label($"Android Bundle Version: {androidBundleVersionCode}");

        //
        // NEXT VERSION PREVIEW
        //
        GUILayout.Space(8);
        GUILayout.Label("Next Version Preview", EditorStyles.boldLabel);
        GUILayout.Label(GetNextVersionPreview(), EditorStyles.wordWrappedLabel);

        GUILayout.Space(15);

        //
        // VERSION INCREMENT OPTIONS
        //
        GUILayout.Label("Version Increment Options", EditorStyles.boldLabel);
        versionIncrement = (VersionIncrement)EditorGUILayout.EnumPopup("Increment:", versionIncrement);

        GUILayout.Space(10);

        GUILayout.Label("Current Version Numbers", EditorStyles.boldLabel);
        major = EditorGUILayout.IntField("Major:", major);
        minor = EditorGUILayout.IntField("Minor:", minor);
        build = EditorGUILayout.IntField("Build:", build);

        GUILayout.Space(10);

        GUILayout.Label("Development Stage", EditorStyles.boldLabel);
        selectedDevelopmentStageIndex = EditorGUILayout.Popup("Stage:", selectedDevelopmentStageIndex, developmentStages);

        GUILayout.Space(10);

        GUILayout.Label("Custom Version (Optional)", EditorStyles.boldLabel);
        customVersion = EditorGUILayout.TextField("Custom Version:", customVersion);

        GUILayout.Space(10);

        //
        // ANDROID BUNDLE VERSION CODE
        //
        GUILayout.Label("Android Bundle Version Code", EditorStyles.boldLabel);
        androidBundleVersionCode = EditorGUILayout.IntField("Bundle Version Code:", androidBundleVersionCode);
        incrementAndroidBundleVersion = EditorGUILayout.Toggle("Increment Android Bundle Version?", incrementAndroidBundleVersion);

        GUILayout.Space(10);

        //
        // IOS BUILD NUMBER
        //
        GUILayout.Label("iOS Build Number", EditorStyles.boldLabel);
        iosBuildNumber = EditorGUILayout.IntField("iOS Build Number:", iosBuildNumber);
        incrementIOSBuildNumber = EditorGUILayout.Toggle("Increment iOS Build Number?", incrementIOSBuildNumber);

        GUILayout.Space(20);

        //
        // BUILD BUTTON
        //
        if (GUILayout.Button("Build"))
        {
            // 1. Update the main version string for the chosen target
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

            // 4. Build
            BuildProject();
            Close();
        }
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
    /// </summary>
    private string GetNextVersionPreview()
    {
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
        //    (We always show them now, no matter the build target)
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
        // 5. Create the "old" vs "new" version strings for iOS (no stage) and Android (with stage)
        //
        string oldStage = developmentStages[selectedDevelopmentStageIndex].ToLower().Replace(" ", "-");

        // Old iOS version is always stage-less
        string oldIOSVersion = $"{oldMajor}.{oldMinor}.{oldBuild}";

        // New iOS version is also stage-less
        string newIOSVersion = $"{previewMajor}.{previewMinor}.{previewBuild}";

        // Old Android version includes stage
        string oldAndroidVersion = $"{oldStage}-{oldMajor}.{oldMinor}.{oldBuild}";

        // New Android version includes stage
        string newStage = developmentStages[selectedDevelopmentStageIndex].ToLower().Replace(" ", "-");
        string newAndroidVersion = $"{newStage}-{previewMajor}.{previewMinor}.{previewBuild}";

        //
        // 6. Build the multi-line preview
        //
        // iOS Preview
        string iOSPreview =
            $"iOS Version: {oldIOSVersion} -> {newIOSVersion}\n" +
            $"iOS Build: {oldIOSBuildNumber} -> {previewIOSBuildNumber}";

        // Android Preview
        string androidPreview =
            $"Android Version: {oldAndroidVersion} -> {newAndroidVersion}\n" +
            $"Android Bundle Version: {oldAndroidBundleVersion} -> {previewAndroidBundleVersion}";

        // Combine them
        // For clarity, we can separate them with some dashes or blank lines
        string previewText =
            $"--- iOS ---\n{iOSPreview}\n\n" +
            $"--- Android ---\n{androidPreview}";

        return previewText;
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
