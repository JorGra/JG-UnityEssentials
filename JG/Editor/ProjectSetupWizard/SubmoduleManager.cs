using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using System.Linq;

using Debug = UnityEngine.Debug;
public class SubmoduleManager
{
    // Hardcoded private repositories
    private Dictionary<string, bool> submoduleRepos = new Dictionary<string, bool>()
    {
        { "JG-Pooling", false },
        { "JG-GenericStructures", false },
        { "JG-SceneLoader", false },
        { "JG-Utils", false }
    };

    private Dictionary<string, string> repoUrls = new Dictionary<string, string>()
    {
        { "JG-Pooling", "https://github.com/yourusername/JG-Pooling.git" },
        { "JG-GenericStructures", "https://github.com/yourusername/JG-GenericStructures.git" },
        { "JG-SceneLoader", "https://github.com/yourusername/JG-SceneLoader.git" },
        { "JG-Utils", "https://github.com/yourusername/JG-Utils.git" }
    };

    private string token = ""; // Personal Access Token (PAT) for private repo access
    private bool isJGSceneLoaderInstalled = false;

    public void DrawSubmoduleSetup()
    {
        GUILayout.Label("Install Private Repositories as Submodules", EditorStyles.boldLabel);

        // Draw checkboxes for each repository
        foreach (var repo in submoduleRepos.Keys.ToList())
        {
            submoduleRepos[repo] = EditorGUILayout.Toggle(repo, submoduleRepos[repo]);
        }

        // Token field for authentication
        token = EditorGUILayout.PasswordField("Personal Access Token (PAT)", token);

        if (GUILayout.Button("Install Selected Repositories"))
        {
            InstallSelectedSubmodules();
        }

        // Only show scene structure creation option if JG-SceneLoader is installed
        if (isJGSceneLoaderInstalled)
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Create Scene Structure"))
            {
                CreateSceneStructure();
            }
        }
    }

    private void InstallSelectedSubmodules()
    {
        // Loop through the selected repositories and install them as submodules
        foreach (var repo in submoduleRepos)
        {
            if (repo.Value)
            {
                AddPrivateSubmodule(repo.Key, repoUrls[repo.Key]);
            }
        }
    }

    private void AddPrivateSubmodule(string repoName, string repoUrl)
    {
        // Modify the repo URL to include the token for HTTPS authentication
        string authenticatedRepoUrl = repoUrl.Insert(8, token + "@");

        // Determine the submodule path in the "Packages/" directory
        string submodulePath = $"Packages/{repoName}";

        // Git command to add the submodule
        string gitCommand = $"submodule add {authenticatedRepoUrl} {submodulePath}";

        ExecuteGitCommand(gitCommand);

        // Check if package.json exists and refresh the package list
        string packageJsonPath = Path.Combine(Application.dataPath, "../", submodulePath, "package.json");
        if (File.Exists(packageJsonPath))
        {
            Debug.Log($"{repoName} added as a package with package.json found.");

            // If JG-SceneLoader is installed, flag it
            if (repoName == "JG-SceneLoader")
            {
                isJGSceneLoaderInstalled = true;
            }
        }
        else
        {
            Debug.LogError($"{repoName} submodule added, but no package.json found. Please check the repository structure.");
        }

        // Refresh the package manager to recognize the new packages
        AssetDatabase.Refresh();
    }

    private void ExecuteGitCommand(string gitCommand)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = "git",
            Arguments = gitCommand,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process())
        {
            process.StartInfo = startInfo;
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Debug.Log($"Successfully added submodule: {result}");
            }
            else
            {
                Debug.LogError($"Error adding submodule: {error}");
            }
        }
    }

    // Method to create the scene structure
    private void CreateSceneStructure()
    {
        string scenesFolderPath = Path.Combine(Application.dataPath, "5. Scenes");
        if (!Directory.Exists(scenesFolderPath))
        {
            Directory.CreateDirectory(scenesFolderPath);
        }

        // Scene files to be created
        string[] sceneNames = { "Bootstrapper", "Main", "Gameplay" };

        foreach (string sceneName in sceneNames)
        {
            string scenePath = Path.Combine(scenesFolderPath, $"{sceneName}.unity");

            // Only create the scene if it doesn't exist
            if (!File.Exists(scenePath))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                EditorSceneManager.SaveScene(newScene, $"Assets/5. Scenes/{sceneName}.unity");
                Debug.Log($"Created scene: {sceneName}");
            }
        }

        // Refresh the asset database to make the new scenes visible
        AssetDatabase.Refresh();
        Debug.Log("Scene structure creation complete.");
    }
}
