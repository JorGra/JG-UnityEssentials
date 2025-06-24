// Assets/Editor/CustomFileCreator.cs
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds “Markdown”, “Text”, and “JSON” file templates to  
/// <c>Assets ▸ Create ▸ Custom Files ▸ …</c> in the Project window.
/// </summary>
public static class CustomFileCreator
{
    // ----------- Menu Items -------------------------------------------------

    [MenuItem("Assets/Create/Custom Files/Markdown File", false, 100)]
    private static void CreateMarkdown()
    {
        CreateFile("NewMarkdown", "md", "# New Document\n");
    }

    [MenuItem("Assets/Create/Custom Files/Text File", false, 101)]
    private static void CreateText()
    {
        CreateFile("NewText", "txt", ""); // empty stub
    }

    [MenuItem("Assets/Create/Custom Files/JSON File", false, 102)]
    private static void CreateJson()
    {
        CreateFile("NewJSON", "json", "{}");
    }

    // ----------- Core Logic --------------------------------------------------

    /// <summary>
    /// Generates a new file with boilerplate, ensuring the filename is unique.
    /// </summary>
    /// <param name="baseName">Base filename without extension.</param>
    /// <param name="extension">File extension without dot.</param>
    /// <param name="contents">Initial file contents.</param>
    private static void CreateFile(string baseName, string extension, string contents)
    {
        string folderPath = GetSelectedFolderPath();
        string filePath = GenerateUniquePath(folderPath, baseName, extension);

        try
        {
            File.WriteAllText(filePath, contents);
            AssetDatabase.Refresh();

            // Select & ping the newly created asset
            var asset = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
        catch (IOException ex)
        {
            Debug.LogError($"CustomFileCreator: Could not create file.\n{ex}");
        }
    }

    /// <summary>
    /// Returns the path of the folder currently selected in the Project window,
    /// or the Assets root if no folder is selected.
    /// </summary>
    private static string GetSelectedFolderPath()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(path))
            return Application.dataPath;

        if (File.Exists(path))
            path = Path.GetDirectoryName(path);

        return path;
    }

    /// <summary>
    /// Appends an incrementing number to the filename if a clash is detected.
    /// </summary>
    private static string GenerateUniquePath(string folderPath, string baseName, string extension)
    {
        string filePath = Path.Combine(folderPath, $"{baseName}.{extension}");
        int counter = 1;

        while (File.Exists(filePath))
        {
            filePath = Path.Combine(folderPath, $"{baseName}{counter}.{extension}");
            counter++;
        }

        // Convert absolute path to “Assets/…” form for Unity
        return filePath.Replace(Application.dataPath, "Assets");
    }
}
#endif