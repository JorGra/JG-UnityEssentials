
// HierarchyFolderEditor.cs (Place in an Editor folder, e.g. Assets/Editor)
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

/// <summary>
/// Custom drawing and creation of Hierarchy Folder markers in the Unity Editor.
/// </summary>
[InitializeOnLoad]
public static class HierarchyFolderEditor
{
    private static readonly Texture2D transparentIcon;

    static HierarchyFolderEditor()
    {
        // Create a transparent texture to remove default GameObject icon
        transparentIcon = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        transparentIcon.hideFlags = HideFlags.HideAndDontSave;
        transparentIcon.SetPixel(0, 0, Color.clear);
        transparentIcon.Apply();

        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
    }

    private static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
    {
        var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;

        var comp = go.GetComponent<HierarchyFolderComponent>();
        if (comp == null) return;

        // Draw gradient background when not selected, spanning full window width
        if (!Selection.Contains(go))
        {
            float fullWidth = EditorGUIUtility.currentViewWidth;
            var bgRect = new Rect(0, selectionRect.y + 1, fullWidth, selectionRect.height - 2);

            // Generate gradient texture based on current component color
            var gradient = GenerateGradientTexture(comp.FolderColor);
            GUI.DrawTexture(bgRect, gradient, ScaleMode.StretchToFill);
        }

        // Remove default GameObject icon
        EditorGUIUtility.SetIconForObject(go, transparentIcon);

        // Centered label
        float fullW = EditorGUIUtility.currentViewWidth;
        var labelRect = new Rect(0, selectionRect.y, fullW, selectionRect.height);
        var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
        EditorGUI.LabelField(labelRect, comp.FolderName, style);

        // Draw underline if enabled
        if (comp.UnderlineEnabled)
        {
            Vector2 textSize = style.CalcSize(new GUIContent(comp.FolderName));
            float xCenter = fullW * 0.5f;
            var lineRect = new Rect(
                xCenter - textSize.x * 0.5f,
                selectionRect.y + selectionRect.height * 0.5f + textSize.y * 0.5f + 1,
                textSize.x,
                1
            );
            EditorGUI.DrawRect(lineRect, comp.FolderColor);
        }
    }

    // Generates a 2x1 horizontal gradient from transparent on left to the given color on right
    private static Texture2D GenerateGradientTexture(Color color)
    {
        var tex = new Texture2D(2, 1, TextureFormat.ARGB32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixel(0, 0, new Color(color.r, color.g, color.b, 0f));
        tex.SetPixel(1, 0, color);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Menu command to create a new Hierarchy Folder marker.
    /// </summary>
    [MenuItem("GameObject/Create Hierarchy Folder", priority = 0)]
    private static void CreateHierarchyFolder(MenuCommand command)
    {
        var parent = command.context as GameObject;
        var go = new GameObject("New Folder");
        go.AddComponent<HierarchyFolderComponent>();
        GameObjectUtility.SetParentAndAlign(go, parent);
        Undo.RegisterCreatedObjectUndo(go, "Create Hierarchy Folder");
        Selection.activeGameObject = go;
    }

    /// <summary>
    /// Before building the player, remove all folder markers and reparent their children.
    /// </summary>
    [PostProcessScene]
    private static void OnPostprocessScene()
    {
        if (!BuildPipeline.isBuildingPlayer)
            return;

        var folders = Object.FindObjectsOfType<HierarchyFolderComponent>();
        foreach (var folder in folders)
        {
            var goFolder = folder.gameObject;
            var parentT = goFolder.transform.parent;
            int index = goFolder.transform.GetSiblingIndex();

            int childCount = goFolder.transform.childCount;
            var children = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
                children[i] = goFolder.transform.GetChild(i);

            for (int i = 0; i < childCount; i++)
            {
                var child = children[i];
                child.SetParent(parentT);
                child.SetSiblingIndex(index + i);
            }

            Object.DestroyImmediate(goFolder);
        }
    }
}
