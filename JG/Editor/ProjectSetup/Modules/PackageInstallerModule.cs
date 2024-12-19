using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
public class PackageInstallerModule : EditorWindow, IModule
{
    // Example preset data structure
    private class PackageInfo
    {
        public string Url;    // The package URL (e.g. "com.unity.postprocessing")
        public string Alias;  // A user-friendly alias/name for the package
        public bool IsInstalled; // Whether the package is installed or not
        public bool IsSelected;  // Whether the user has selected this package for installation

        public PackageInfo(string url, string alias, bool isInstalled = false)
        {
            Url = url;
            Alias = alias;
            IsInstalled = isInstalled;
            IsSelected = false;
        }
    }

    private class Preset
    {
        public string Name;
        public string Description;
        public List<PackageInfo> Packages;

        public Preset(string name, string description, List<PackageInfo> packages)
        {
            Name = name;
            Description = description;
            Packages = packages;
        }
    }

    // Sample data
    private List<Preset> presets;

    private Vector2 scrollPos;

    private ListRequest listRequest;
    private bool listingPackages = false;
    private HashSet<string> installedPackages = new HashSet<string>();


    public static void ShowWindow()
    {
        GetWindow<PackageInstallerModule>("Package Installer");
    }

    public void OnEnable()
    {
        listRequest = Client.List(true);
        listingPackages = true;
        EditorApplication.update += OnEditorUpdate;

        // Initialize presets and packages here
        presets = new List<Preset>()
        {
            new Preset(
                "Unity Essentials",
                "This preset installs common editor extensions and utilities that are useful for any project.",
                new List<PackageInfo>()
                {
                    new PackageInfo("git+https://github.com/JorGra/JG-UnityEssentials.git", "JG/Unity Essentials", CheckIfInstalled("com.unity.test-framework")),
                    new PackageInfo("git+https://github.com/JorGra/JG-UnityEditor-GameViewFullscreen.git", "JG/Gameview Fullscreen", CheckIfInstalled("com.unity.collab-proxy")),
                    new PackageInfo("git+https://github.com/WooshiiDev/Unity-Folder-Icons.git", "Folder Icons", CheckIfInstalled("com.unity.collab-proxy")),
                    new PackageInfo("git+https://github.com/dbrizov/NaughtyAttributes.git", "NaughtyAttributes", CheckIfInstalled("com.unity.collab-proxy")),
                    new PackageInfo("git+https://github.com/KyryloKuzyk/PrimeTween.git", "PrimeTween", CheckIfInstalled("com.unity.collab-proxy")),
                }
            ),
            new Preset(
                "Gameplay",
                "This preset contains packages that supply tools that are needed in any game.",
                new List<PackageInfo>()
                {
                    new PackageInfo("git+https://github.com/starikcetin/Eflatun.SceneReference.git#4.1.1", "Eflatun/SceneReference", CheckIfInstalled("com.unity.visualeffectgraph")),
                    new PackageInfo("git+https://github.com/KyryloKuzyk/PrimeTween.git", "PrimeTween", CheckIfInstalled("com.unity.collab-proxy")),
                    new PackageInfo("git+https://github.com/JorGra/JG-UnityEssentials.git", "JG/Unity Essentials", CheckIfInstalled("com.unity.test-framework")),
                    new PackageInfo("git+https://github.com/JorGra/JGameFramework", "JGameFramework", CheckIfInstalled("com.unity.shadergraph"))
                }
            ),
        };
    }

    public void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (listingPackages && listRequest.IsCompleted)
        {
            listingPackages = false;
            EditorApplication.update -= OnEditorUpdate;

            if (listRequest.Status == StatusCode.Success)
            {
                // Populate the set of installed packages
                installedPackages = new HashSet<string>(
                    listRequest.Result.Select(p => p.name),
                    StringComparer.OrdinalIgnoreCase
                );
            }
            else
            {
                Debug.LogError("Failed to list installed packages: " + listRequest.Error.message);
            }

            // Update each package's installed status
            foreach (var preset in presets)
            {
                foreach (var pkg in preset.Packages)
                {
                    pkg.IsInstalled = CheckIfInstalled(pkg.Url);
                }
            }

            Repaint();
        }
    }

    public void OnGUI()
    {
        EditorGUILayout.LabelField("Package Installer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (listingPackages)
        {
            EditorGUILayout.LabelField("Loading installed packages...");
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var preset in presets)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(preset.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(preset.Description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            foreach (var package in preset.Packages)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = !package.IsInstalled;
                package.IsSelected = EditorGUILayout.ToggleLeft(package.Alias + (package.IsInstalled ? " (Installed)" : ""), package.IsSelected);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Button to install selected packages
            GUI.enabled = preset.Packages.Any(p => p.IsSelected && !p.IsInstalled);
            if (GUILayout.Button("Install Selected Packages"))
            {
                InstallSelectedPackages(preset);
            }
            GUI.enabled = true;

            // Button to install the entire preset
            // This will ignore current selection state and install all not-installed packages
            var anyNotInstalled = preset.Packages.Any(p => !p.IsInstalled);
            GUI.enabled = anyNotInstalled;
            if (GUILayout.Button("Install Entire Preset"))
            {
                InstallEntirePreset(preset);
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }

    private bool CheckIfInstalled(string packageUrl)
    {
        return installedPackages.Contains(packageUrl);
    }

    private void InstallSelectedPackages(Preset preset)
    {
        var packagesToInstall = preset.Packages
            .Where(p => p.IsSelected && !p.IsInstalled)
            .Select(p => p.Url)
            .ToArray();

        if (packagesToInstall.Length > 0)
        {
            Debug.Log("Installing packages: " + string.Join(", ", packagesToInstall));
            ProjectSetup.InstallPackages(packagesToInstall);

            foreach (var p in preset.Packages)
            {
                if (p.IsSelected)
                {
                    p.IsInstalled = true;
                }
            }

            Debug.Log("Packages installed.");
        }
        else
        {
            Debug.Log("No packages selected for installation.");
        }
    }

    private void InstallEntirePreset(Preset preset)
    {
        var packagesToInstall = preset.Packages
            .Where(p => !p.IsInstalled)
            .Select(p => p.Url)
            .ToArray();

        if (packagesToInstall.Length > 0)
        {
            Debug.Log("Installing entire preset: " + preset.Name + "\nPackages: " + string.Join(", ", packagesToInstall));
            ProjectSetup.InstallPackages(packagesToInstall);

            // Mark as installed
            foreach (var p in preset.Packages)
            {
                p.IsInstalled = true;
            }

            Debug.Log("Entire preset installed.");
        }
        else
        {
            Debug.Log("All packages in this preset are already installed.");
        }
    }
}