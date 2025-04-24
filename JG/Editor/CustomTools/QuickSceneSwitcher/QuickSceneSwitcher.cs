using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor window for quick switching between build-configured scenes.
/// </summary>
public class QuickSceneSwitcher : EditorWindow
{
    private Vector2 scrollPosition;
    private string filterText = "";

    private Texture2D playIcon;
    private Texture2D loadAdditiveIcon;
    private GUIContent reloadIcon;

    /// <summary>
    /// Opens the Scene Switcher window.
    /// </summary>
    [MenuItem("Tools/Open Scene Switcher")]
    public static void ShowWindow()
    {
        GetWindow<QuickSceneSwitcher>("Scene Switcher");
    }

    private void OnEnable()
    {
        // Load play & additive icons (with fallback)
        playIcon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D
                   ?? EditorGUIUtility.FindTexture("d_PlayButton");
        loadAdditiveIcon = EditorGUIUtility.IconContent("Toolbar Plus").image as Texture2D
                           ?? EditorGUIUtility.FindTexture("d_Toolbar Plus");

        // Load reload icon, fallback to Unicode
        reloadIcon = EditorGUIUtility.IconContent("Refresh");
        if (reloadIcon.image == null)
            reloadIcon = new GUIContent("\u21BB");
    }

    private void OnGUI()
    {
        DrawToolbar();
        //GUILayout.Space(4);
        DrawSceneList();
        GUILayout.Space(4);
        DrawFooter();

        // Needed so hover states update immediately
        if (Event.current.type == EventType.MouseMove)
            Repaint();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.FlexibleSpace();
            filterText = GUILayout.TextField(
                filterText,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(200)
            );
        }
    }

    private void DrawSceneList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .Where(path =>
                string.IsNullOrEmpty(filterText)
                || Path.GetFileNameWithoutExtension(path)
                    .IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0
            )
            .ToArray();

        if (scenes.Length == 0)
        {
            EditorGUILayout.LabelField("No scenes found.");
        }
        else
        {
            DrawEnhancedList(scenes);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEnhancedList(string[] scenes)
    {
        const float rowHeight = 24f;
        const float iconSize = 20f;
        const float padding = 4f;
        const float spacing = 4f;

        // Style for icon buttons (uses built-in hover)
        var iconButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedWidth = (int)iconSize,
            fixedHeight = (int)iconSize,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            alignment = TextAnchor.MiddleCenter
        };

        for (int i = 0; i < scenes.Length; i++)
        {
            string path = scenes[i];
            string sceneName = Path.GetFileNameWithoutExtension(path);

            Rect rowRect = EditorGUILayout.GetControlRect(
                false,
                rowHeight,
                GUILayout.ExpandWidth(true)
            );

            // Alternating background
            Color bgColor = (i % 2 == 0)
                ? new Color(0.18f, 0.18f, 0.18f)
                : new Color(0.22f, 0.22f, 0.22f);
            EditorGUI.DrawRect(rowRect, bgColor);

            // Row hover overlay
            if (rowRect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.1f));

            // Compute icon positions
            float iconX1 = rowRect.xMax - padding - (2 * iconSize + spacing);
            float iconX2 = rowRect.xMax - padding - iconSize;

            // Scene name spans up to first icon
            float nameWidth = iconX1 - (rowRect.x + padding);
            Rect nameRect = new Rect(
                rowRect.x + padding,
                rowRect.y + (rowHeight - EditorGUIUtility.singleLineHeight) / 2,
                nameWidth,
                EditorGUIUtility.singleLineHeight
            );
            if (GUI.Button(nameRect, sceneName, EditorStyles.label))
                LoadScene(path);

            // Play button
            Rect playRect = new Rect(
                iconX1,
                rowRect.y + (rowHeight - iconSize) / 2,
                iconSize,
                iconSize
            );
            if (GUI.Button(playRect, playIcon, iconButtonStyle))
                PlayScene(path);

            // Additive-load button
            Rect addRect = new Rect(
                iconX2,
                rowRect.y + (rowHeight - iconSize) / 2,
                iconSize,
                iconSize
            );
            if (GUI.Button(addRect, loadAdditiveIcon, iconButtonStyle))
                LoadSceneAdditively(path);
        }
    }

    private void DrawFooter()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            // Small reload icon instead of text
            if (GUILayout.Button(reloadIcon, GUIStyle.none,
                                 GUILayout.Width(20), GUILayout.Height(20)))
            {
                Repaint();
            }
        }
    }

    private void LoadScene(string scenePath)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(scenePath);
    }

    private void PlayScene(string scenePath)
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        EditorSceneManager.OpenScene(scenePath);
        EditorApplication.isPlaying = true;
    }

    private void LoadSceneAdditively(string scenePath)
    {
        if (EditorApplication.isPlaying)
            EditorSceneManager.LoadSceneAsyncInPlayMode(
                scenePath,
                new LoadSceneParameters(LoadSceneMode.Additive)
            );
        else
            Debug.LogWarning("You must be in play mode to load scenes additively.");
    }
}
