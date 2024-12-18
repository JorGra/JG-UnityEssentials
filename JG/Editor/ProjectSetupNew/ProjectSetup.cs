using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

using static System.Environment;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;
public static class ProjectSetup
{
    [MenuItem("Tools/Setup/Import Assets")]
    static void ImportEssentials()
    {
        //Assets.ImportAssets("Odin Inspector and Serializer.unitypackage", "Sirenix/Editor ExtensionsSystem");
    }

    [MenuItem("Tools/Setup/Install ESsential Packages")]
    public static void InstallPackages()
    {
        //Packages.InstallPackages(new string[] { "com.unity.postprocessing", "com.unity.cinemachine" });

        string[] packages =
        {
            //"com.unity.postprocessing"

            //"com.unity.inputsystem", // ADD THIS LAST AS IT WILL RESTART THE EDITOR
        };

        Packages.InstallPackages(packages);
    }


    [MenuItem("Tools/Setup/Create Folders")]
    public static void CreateFolders()
    {
        //Folders.Create("_Project", "Animation", "Art", "Materials", "Prefabs", "Scripts/Tests", "Scripts/Tests/Editor", "Scripts/Tests/Runtime");
        Refresh();
        Folders.Move("_Project", "Scenes");
        Folders.Move("_Project", "Settings");
        Folders.Delete("TutorialInfo");
        Refresh();

        MoveAsset("Assets/InputSystem_Actions.inputactions", "Assets/_Project/Settings/InputSystem_Actions.inputactions");
        DeleteAsset("Assets/Readme.asset");
        Refresh();

        // Optional: Disable Domain Reload
        // EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
    }

    static class Assets
    {
        public static void ImportAssets(string asset, string folder)
        {
            string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string assetFolder = Path.Combine(basePath, "Unity/Asset Store-5.x");

            AssetDatabase.ImportPackage(Path.Combine(assetFolder, folder, asset), false);
        }
    }

    static class Packages
    {
        static AddRequest request;
        static Queue<string> packagesToInstall = new();

        static async void StartPackageInstallation()
        {
            request = Client.Add(packagesToInstall.Dequeue());

            while (!request.IsCompleted)
            {
                await Task.Delay(10);
            }

            if(request.Status == StatusCode.Success) Debug.Log("Package installed successfully");
            else if (request.Status == StatusCode.Failure) Debug.LogError("Failed to install package: " + request.Error.message);

            if (packagesToInstall.Count > 0)
            {
                await Task.Delay(1000);
                StartPackageInstallation();
            }
        }

        public static void InstallPackages(string[] packages)
        {
            foreach (var package in packages)
            {
                packagesToInstall.Enqueue(package);
            }
        }
    }
    static class Folders
    {
        public static void Create(string root, params string[] folders)
        {
            var fullpath = Combine(Application.dataPath, root);
            if (!Directory.Exists(fullpath))
            {
                Directory.CreateDirectory(fullpath);
            }

            foreach (var folder in folders)
            {
                CreateSubFolders(fullpath, folder);
            }
        }

        static void CreateSubFolders(string rootPath, string folderHierarchy)
        {
            var folders = folderHierarchy.Split('/');
            var currentPath = rootPath;

            foreach (var folder in folders)
            {
                currentPath = Combine(currentPath, folder);
                if (!Directory.Exists(currentPath))
                {
                    Directory.CreateDirectory(currentPath);
                }
            }
        }

        public static void Move(string newParent, string folderName)
        {
            var sourcePath = $"Assets/{folderName}";
            if (IsValidFolder(sourcePath))
            {
                var destinationPath = $"Assets/{newParent}/{folderName}";
                var error = MoveAsset(sourcePath, destinationPath);

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"Failed to move {folderName}: {error}");
                }
            }
        }

        public static void Delete(string folderName)
        {
            var pathToDelete = $"Assets/{folderName}";

            if (IsValidFolder(pathToDelete))
            {
                DeleteAsset(pathToDelete);
            }
        }
    }
}
