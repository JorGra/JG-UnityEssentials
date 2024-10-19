using UnityEditor;
using UnityEngine;

public class ProjectSetupMainWindow : EditorWindow
{
    private PackageManagerSetup packageManagerSetup = new PackageManagerSetup();
    private FolderStructureSetup folderStructureSetup = new FolderStructureSetup();
    private InputSystemSetup inputSystemSetup = new InputSystemSetup();
    private SubmoduleManager submoduleManager = new SubmoduleManager();

    [MenuItem("Tools/Project Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<ProjectSetupMainWindow>("Project Setup Wizard");
    }

    private void OnGUI()
    {
        //GUILayout.Label("Project Setup Wizard", EditorStyles.boldLabel);

        //GUILayout.Space(20);
        packageManagerSetup.DrawPackageSelection();

        GUILayout.Space(20);
        folderStructureSetup.DrawFolderSetup();

        GUILayout.Space(20);
        inputSystemSetup.DrawInputSystemSetup();

        //GUILayout.Space(20);
        //submoduleManager.DrawSubmoduleSetup(); // Draw the submodule installation UI with scene creation support
    }
}
