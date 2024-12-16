using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class PackageManagerSetup
{
    private class Package
    {
        public string Category;    // New: Category for organizational purposes
        public string DisplayName; // UI display name
        public string PackageName; // Manifest dependency name
        public string PackageUrl;  // Git or URL path

        public Package(string category, string displayName, string packageName, string packageUrl)
        {
            Category = category;
            DisplayName = displayName;
            PackageName = packageName;
            PackageUrl = packageUrl;
        }
    }

    // You can group packages by category for better organization:
    // For example: "Core", "Editor", "Utilities", "Third Party"
    private List<Package> packages = new List<Package>
    {
        new Package("Core", "JG/Unity Essentials", "com.jg.unityessentials", "git+https://github.com/yourname/pooling-system.git#1.0.0"),
        new Package("Core", "JG/Scene Manager", "com.jg.scenemanager", "git+https://github.com/yourname/scene-manager.git#1.0.0"),

        new Package("Third Party", "DOTween", "com.demigiant.dotween", "git+https://github.com/Demigiant/dotween.git"),
        new Package("Third Party", "Odin Inspector", "com.odininspector", "https://odininspector.com/package-url"),
        new Package("Third Party", "Editor Toolbox", "com.editortoolbox", "git+https://github.com/arimger/Unity-Editor-Toolbox.git#upm"),
        new Package("Third Party", "Eflatun/Scene Reference", "com.eflatun.scenereference", "git+https://github.com/starikcetin/Eflatun.SceneReference.git#4.1.1"),
        new Package("Third Party", "Naughty Attributes", "com.naughtyattributes", "git+https://github.com/dbrizov/NaughtyAttributes.git"),
        new Package("Third Party", "Folder Icons", "com.foldericons", "git+https://github.com/WooshiiDev/Unity-Folder-Icons.git"),

        new Package("Custom", "Hex Grid Framework", "com.hexgridframework", "git+https://github.com/yourname/hex-grid-framework.git#1.0.0")
    };

    private Dictionary<string, bool> selectedPackages = new Dictionary<string, bool>();

    public PackageManagerSetup()
    {
        foreach (var package in packages)
        {
            selectedPackages[package.PackageName] = false;
        }
    }

    public void DrawPackageSelection()
    {
        GUILayout.Label("Select Packages to Download", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Group packages by category to display separate headers
        var packagesByCategory = new Dictionary<string, List<Package>>();
        foreach (var package in packages)
        {
            if (!packagesByCategory.ContainsKey(package.Category))
            {
                packagesByCategory[package.Category] = new List<Package>();
            }
            packagesByCategory[package.Category].Add(package);
        }

        // Display packages by category
        foreach (var kvp in packagesByCategory)
        {
            GUILayout.Label(kvp.Key, EditorStyles.boldLabel);
            foreach (var package in kvp.Value)
            {
                selectedPackages[package.PackageName] = EditorGUILayout.Toggle("  " + package.DisplayName, selectedPackages[package.PackageName]);
            }
            GUILayout.Space(10);
        }

        if (GUILayout.Button("Download Selected Packages"))
        {
            DownloadSelectedPackages();
        }
    }

    private void DownloadSelectedPackages()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

        if (!File.Exists(manifestPath))
        {
            Debug.LogError("Could not find manifest.json file in the expected location.");
            return;
        }

        string manifestContent = File.ReadAllText(manifestPath);
        int dependenciesIndex = manifestContent.IndexOf("\"dependencies\": {");

        if (dependenciesIndex == -1)
        {
            Debug.LogError("Invalid manifest.json: Missing \"dependencies\" section.");
            return;
        }

        bool modified = false;

        // We'll rebuild the dependencies section carefully.
        // Extract the dependencies block
        int dependenciesStart = manifestContent.IndexOf("{", dependenciesIndex) + 1;
        int dependenciesEnd = manifestContent.IndexOf("}", dependenciesStart);

        if (dependenciesEnd == -1)
        {
            Debug.LogError("Invalid manifest.json: could not find closing '}' for dependencies.");
            return;
        }

        // Current dependencies content (everything inside the {})
        string dependenciesContent = manifestContent.Substring(dependenciesStart, dependenciesEnd - dependenciesStart);
        dependenciesContent = dependenciesContent.Trim(); // Remove leading/trailing whitespace

        // We'll collect lines and ensure correct commas.
        List<string> dependencyLines = new List<string>();
        if (!string.IsNullOrEmpty(dependenciesContent))
        {
            // Split existing dependencies by line
            using (StringReader reader = new StringReader(dependenciesContent))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!string.IsNullOrEmpty(line))
                        dependencyLines.Add(line.TrimEnd(','));
                }
            }
        }

        // Add new dependencies if not already present
        foreach (var package in packages)
        {
            if (selectedPackages[package.PackageName])
            {
                bool alreadyInManifest = false;
                foreach (var line in dependencyLines)
                {
                    if (line.StartsWith($"\"{package.PackageName}\""))
                    {
                        alreadyInManifest = true;
                        Debug.Log($"Package {package.DisplayName} is already in the manifest.");
                        break;
                    }
                }

                if (!alreadyInManifest)
                {
                    // Add new dependency line
                    string newDependency = $"\"{package.PackageName}\": \"{package.PackageUrl}\"";
                    dependencyLines.Add(newDependency);
                    modified = true;
                    Debug.Log($"Added package: {package.DisplayName}");
                }
            }
        }

        if (modified)
        {
            // Rebuild the dependencies section ensuring commas
            for (int i = 0; i < dependencyLines.Count; i++)
            {
                // Add a comma at the end of all but the last line
                if (i < dependencyLines.Count - 1)
                {
                    dependencyLines[i] = dependencyLines[i] + ",";
                }
            }

            string newDependenciesBlock = string.Join("\n    ", dependencyLines.ToArray());

            // Reinsert into manifest
            string beforeDependencies = manifestContent.Substring(0, dependenciesStart);
            string afterDependencies = manifestContent.Substring(dependenciesEnd);

            string updatedManifest = beforeDependencies
                                     + "\n    " + newDependenciesBlock + "\n"
                                     + afterDependencies;

            File.WriteAllText(manifestPath, updatedManifest);
            AssetDatabase.Refresh();
            Debug.Log("Selected packages have been added.");
        }
        else
        {
            Debug.Log("No packages were selected for download or all selected packages are already installed.");
        }
    }
}
