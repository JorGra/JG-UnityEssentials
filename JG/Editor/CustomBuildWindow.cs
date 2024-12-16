using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class CustomBuildWindow : EditorWindow
{
    private enum VersionIncrement
    {
        Build,
        Minor,
        Major
    }

    private VersionIncrement versionIncrement = VersionIncrement.Build;
    private string[] developmentStages = { "Pre-Alpha", "Alpha", "Beta", "Release" };
    private int selectedDevelopmentStageIndex = 0;

    private int major = 0;
    private int minor = 0;
    private int build = 0;

    private string customVersion = "";

    private static BuildPlayerOptions cachedBuildPlayerOptions;

    private string currentFullVersion = "";

    // Use InitializeOnLoadMethod to register the build handler
    [InitializeOnLoadMethod]
    private static void Init()
    {
        // Register the build handler
        BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
    }

    private static void BuildPlayerHandler(BuildPlayerOptions options)
    {
        cachedBuildPlayerOptions = options;
        CustomBuildWindow window = GetWindow<CustomBuildWindow>("Build Version");
        window.Show();
    }

    private void OnEnable()
    {
        // Load current version
        LoadCurrentVersion();
    }

    private void OnGUI()
    {
        GUILayout.Label("Current Build Version", EditorStyles.boldLabel);
        GUILayout.Label(currentFullVersion, EditorStyles.wordWrappedLabel);

        GUILayout.Space(10);

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

        GUILayout.Space(20);

        if (GUILayout.Button("Build"))
        {
            UpdateVersion();
            BuildProject();
            Close();
        }
    }

    private void LoadCurrentVersion()
    {
        string currentVersion = PlayerSettings.bundleVersion;
        currentFullVersion = currentVersion; // Store the full current version to display in the GUI

        // Remove development stage prefix
        string versionWithoutStage = currentVersion;
        foreach (var stage in developmentStages)
        {
            string prefix = stage.ToLower().Replace(" ", "-") + "-";
            if (currentVersion.StartsWith(prefix))
            {
                versionWithoutStage = currentVersion.Substring(prefix.Length);
                selectedDevelopmentStageIndex = System.Array.IndexOf(developmentStages, stage);
                break;
            }
        }

        string[] versionParts = versionWithoutStage.Split('.');
        if (versionParts.Length >= 3)
        {
            int.TryParse(versionParts[0], out major);
            int.TryParse(versionParts[1], out minor);
            int.TryParse(versionParts[2], out build);
        }
        else
        {
            // Default version numbers
            major = 1;
            minor = 0;
            build = 0;
        }
    }

    private void UpdateVersion()
    {
        // Increment version based on selection
        switch (versionIncrement)
        {
            case VersionIncrement.Build:
                build++;
                break;
            case VersionIncrement.Minor:
                minor++;
                build = 0; // Reset build number
                break;
            case VersionIncrement.Major:
                major++;
                minor = 0; // Reset minor
                build = 0; // Reset build
                break;
        }

        // If custom version is set, override
        if (!string.IsNullOrEmpty(customVersion))
        {
            // Assume customVersion is in the format major.minor.build
            string[] versionParts = customVersion.Split('.');
            if (versionParts.Length >= 3)
            {
                int.TryParse(versionParts[0], out major);
                int.TryParse(versionParts[1], out minor);
                int.TryParse(versionParts[2], out build);
            }
            else
            {
                Debug.LogWarning("Custom version format is incorrect. Please use format 'Major.Minor.Build'");
            }
        }

        string newVersion = $"{major}.{minor}.{build}";

        // Add development stage
        string developmentStage = developmentStages[selectedDevelopmentStageIndex].ToLower().Replace(" ", "-");
        newVersion = $"{developmentStage}-{newVersion}";

        // Update PlayerSettings
        PlayerSettings.bundleVersion = newVersion;

        Debug.Log("Updated version to: " + newVersion);
    }

    private void BuildProject()
    {
        // Use the cached BuildPlayerOptions
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

        // Build the project
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
