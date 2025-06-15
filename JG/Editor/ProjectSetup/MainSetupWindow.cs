using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MainPackageInstallerWindow : EditorWindow
{
    private List<IModule> modules = new List<IModule>();
    private int currentModuleIndex = 0;
    private readonly string[] moduleNames = { "Folder Setup", "Project Setup Tool" };

    [MenuItem("Tools/Project Setup Tool", false, 2507)]
    public static void ShowWindow()
    {
        GetWindow<MainPackageInstallerWindow>("Project Setup Tool");
    }

    private void OnEnable()
    {
        modules.Clear();
        modules.Add(new FolderSetupModule());
        modules.Add(new PackageInstallerModule());

        foreach (var module in modules)
        {
            module.OnEnable();
        }
    }

    private void OnDisable()
    {
        foreach (var module in modules)
        {
            module.OnDisable();
        }

        modules.Clear();
    }

    private void OnGUI()
    {
        if (modules.Count == 0)
            return;

        currentModuleIndex = GUILayout.Toolbar(currentModuleIndex, moduleNames);
        modules[currentModuleIndex].OnGUI();
    }
}

public interface IModule
{
    void OnEnable();
    void OnDisable();
    void OnGUI();
}
