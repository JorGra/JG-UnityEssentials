using UnityEditor;
using UnityEngine;
using System.IO;

public class InputSystemSetup
{
    private bool inputSystemAlreadySetUp = false;

    public InputSystemSetup()
    {
        inputSystemAlreadySetUp = IsInputSystemSetUp();
    }

    public void DrawInputSystemSetup()
    {
        if (inputSystemAlreadySetUp)
        {
            EditorGUILayout.HelpBox("Input System is already set up.", MessageType.Info);
        }
        else
        {
            if (GUILayout.Button("Set Up New Input System"))
            {
                SetupInputSystem();
            }
        }
    }

    private void SetupInputSystem()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

        if (File.Exists(manifestPath))
        {
            var manifest = File.ReadAllText(manifestPath);
            if (!manifest.Contains("com.unity.inputsystem"))
            {
                string latestInputSystemVersion = "1.2.0";  // Latest version
                int index = manifest.LastIndexOf("\"dependencies\": {");
                if (index != -1)
                {
                    manifest = manifest.Insert(index + 17, $"\n    \"com.unity.inputsystem\": \"{latestInputSystemVersion}\",");
                    File.WriteAllText(manifestPath, manifest);
                    AssetDatabase.Refresh();
                    Debug.Log("New Input System package added.");
                }
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "ENABLE_INPUT_SYSTEM");
            PlayerSettings.SetPropertyString("ActiveInputHandler", "Input System Package");
            Debug.Log("New Input System enabled.");
        }
        else
        {
            Debug.LogError("Could not find manifest.json file.");
        }
    }

    private bool IsInputSystemSetUp()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

        if (File.Exists(manifestPath))
        {
            var manifest = File.ReadAllText(manifestPath);
            return manifest.Contains("com.unity.inputsystem");
        }

        return false;
    }
}
