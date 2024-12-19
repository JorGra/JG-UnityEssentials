using System.IO;
using UnityEditor;
using UnityEngine;

public class FolderSetupModule : IModule
{
    public void OnEnable()
    {
        // Do any setup required when the module is loaded
    }

    public void OnDisable()
    {
        // Do any cleanup required when the module is unloaded
    }

    public void OnGUI()
    {
        GUILayout.Label("Create Standard Folder Structure", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Folder Structure"))
        {
            CreateFolderStructure();
        }
    }

    private void CreateFolderStructure()
    {
        ProjectSetup.CreateFolders();
    }
}
