using UnityEditor;
using UnityEngine;
using System.IO;

public class FolderStructureSetup
{
    private static readonly string[] FolderStructure = new string[]
    {
        "1. Import",
        "2. Materials",
        "3. Prefabs",
        "4. Scripts",
        "5. Scenes",
        "6. Particles",
        "7. Animation",
        "8. Settings"
    };

    private bool folderStructureAlreadySetUp = false;

    public FolderStructureSetup()
    {
        folderStructureAlreadySetUp = IsFolderStructureSetUp();
    }

    public void DrawFolderSetup()
    {
        if (folderStructureAlreadySetUp)
        {
            EditorGUILayout.HelpBox("Folder structure is already set up.", MessageType.Info);
        }
        else
        {
            if (GUILayout.Button("Set Up Folder Structure"))
            {
                CreateFolderStructure();
            }
        }
    }

    private void CreateFolderStructure()
    {
        foreach (var folder in FolderStructure)
        {
            string folderPath = Path.Combine(Application.dataPath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Debug.Log($"Created folder: {folder}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Folder structure set up complete.");
    }

    private bool IsFolderStructureSetUp()
    {
        foreach (var folder in FolderStructure)
        {
            string folderPath = Path.Combine(Application.dataPath, folder);
            if (!Directory.Exists(folderPath))
            {
                return false;
            }
        }
        return true;
    }
}
