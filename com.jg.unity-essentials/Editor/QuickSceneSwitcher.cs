using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;

public class QuickSceneSwitcher : EditorWindow
{
    private Vector2 scrollPosition;
    private Texture2D playIcon;
    private Texture2D loadAdditiveIcon;

    [MenuItem("Tools/Open Scene Switcher")]
    public static void ShowWindow()
    {
        GetWindow<QuickSceneSwitcher>("Scene Switcher");
    }

    private void OnEnable()
    {
        // Load the icons
        playIcon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D;
        loadAdditiveIcon = EditorGUIUtility.IconContent("Toolbar Plus").image as Texture2D;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene Quickload", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();

        if (scenes.Length == 0)
        {
            EditorGUILayout.LabelField("No scenes found in build settings.");
        }
        else
        {
            foreach (var scene in scenes)
            {
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

                EditorGUILayout.BeginHorizontal();

                // Leftmost button that takes all available space
                if (GUILayout.Button($"{sceneName}", GUILayout.ExpandWidth(true)))
                {
                    LoadScene(scene.path);
                }

                // Create flexible space between the left and right buttons
                GUILayout.FlexibleSpace();

                // Right-aligned fixed-width buttons
                if (GUILayout.Button(new GUIContent(playIcon), GUILayout.Width(30), GUILayout.Height(20)))
                {
                    PlayScene(scene.path);
                }

                if (GUILayout.Button(new GUIContent(loadAdditiveIcon), GUILayout.Width(30), GUILayout.Height(20)))
                {
                    LoadSceneAdditively(scene.path);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Refresh Scenes"))
        {
            Repaint();
        }
    }

    private void PlayScene(string scenePath)
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        EditorSceneManager.OpenScene(scenePath);
        EditorApplication.isPlaying = true;
    }

    private void LoadSceneAdditively(string scenePath)
    {
        if (EditorApplication.isPlaying)
        {
            EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
        }
        else
        {
            Debug.LogWarning("You must be in play mode to load scenes additively.");
        }
    }

    private void LoadScene(string scenePath)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}
