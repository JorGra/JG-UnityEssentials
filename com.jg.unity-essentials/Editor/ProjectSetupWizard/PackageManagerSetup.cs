using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class PackageManagerSetup
{
    private Dictionary<string, bool> toolsPackages = new Dictionary<string, bool>()
    {
        { "JG/UnityEssentials", false },
        { "JG/SceneManager", false }
    };

    private Dictionary<string, bool> editorExtensionsPackages = new Dictionary<string, bool>()
    {
        { "OdinInspector", false },
        { "NaughtyAttributes", false },
        { "Folder Icons", false },
        { "Editor Toolbox", false },
        { "Eflatun/Scene Reference", false },
    };

    private Dictionary<string, bool> gameplayPackages = new Dictionary<string, bool>()
    {
        { "HexGridFramework", false }
    };

    public void DrawPackageSelection()
    {
        GUILayout.Label("Select Packages to Download", EditorStyles.boldLabel);
        GUILayout.Space(10);
        DrawPackageGroup("Tools", toolsPackages);
        GUILayout.Space(10);
        DrawPackageGroup("Editor Extensions", editorExtensionsPackages);
        GUILayout.Space(10);
        DrawPackageGroup("Gameplay", gameplayPackages);
        GUILayout.Space(10);
        if (GUILayout.Button("Download Selected Packages"))
        {
            DownloadSelectedPackages();
        }
    }

    private void DrawPackageGroup(string groupName, Dictionary<string, bool> packageGroup)
    {
        GUILayout.Label(groupName, EditorStyles.boldLabel);

        foreach (var package in packageGroup.Keys.ToList())
        {
            packageGroup[package] = EditorGUILayout.Toggle(package, packageGroup[package]);
        }
    }

    private void DownloadSelectedPackages()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

        if (File.Exists(manifestPath))
        {
            var manifest = File.ReadAllText(manifestPath);
            var modified = false;

            foreach (var package in toolsPackages.Concat(editorExtensionsPackages).Concat(gameplayPackages))
            {
                if (package.Value)
                {
                    AddPackageToManifest(ref manifest, package.Key, ref modified);
                }
            }

            if (modified)
            {
                File.WriteAllText(manifestPath, manifest);
                AssetDatabase.Refresh();
                Debug.Log("Selected packages have been added.");
            }
            else
            {
                Debug.Log("No packages were selected for download.");
            }
        }
        else
        {
            Debug.LogError("Could not find manifest.json file.");
        }
    }

    private void AddPackageToManifest(ref string manifest, string packageName, ref bool modified)
    {
        Dictionary<string, string> packageURLs = new Dictionary<string, string>()
        {
            { "JG/UnityEssentials", "https://github.com/yourname/pooling-system.git#1.0.0" },
            { "JG/SceneManager", "https://github.com/yourname/scene-manager.git#1.0.0" },
            { "OdinInspector", "https://odininspector.com/package-url" },
            { "EditorToolbox", "https://github.com/arimger/Unity-Editor-Toolbox.git#upm" },
            { "Eflatun/SceneReference", "https://github.com/starikcetin/Eflatun.SceneReference.git#upm" },
            { "NaughtyAttributes", "https://github.com/dbrizov/NaughtyAttributes.git" },
            { "Folder Icons", "https://github.com/WooshiiDev/Unity-Folder-Icons.git" },
            { "HexGridFramework", "https://github.com/yourname/hex-grid-framework.git#1.0.0" }
        };

        if (!manifest.Contains(packageURLs[packageName]))
        {
            int index = manifest.LastIndexOf("\"dependencies\": {");
            if (index != -1)
            {
                manifest = manifest.Insert(index + 17, $"\n    \"{packageURLs[packageName]}\",");
                modified = true;
                Debug.Log($"Added package: {packageName}");
            }
        }
    }
}
