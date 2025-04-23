
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[InitializeOnLoad]
public static class HierarchyFolderEditor
{
    static HierarchyFolderEditor()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
    }

    private static void OnHierarchyWindowItemGUI(int instanceID, Rect rowRect)
    {
        var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;

        var comp = go.GetComponent<HierarchyFolder>();
        if (comp == null) return;

        float fullWidth = EditorGUIUtility.currentViewWidth;
        float arrowWidth = EditorStyles.foldout.CalcSize(GUIContent.none).x - 14f;

        // Back up from the label area to include the fold arrow
        float startX = rowRect.x - arrowWidth;
        var gradRect = new Rect(0, rowRect.y + 1, fullWidth, rowRect.height - 2);
        // Only draw on this folder's row
        var boxRect = new Rect(
            startX,
            rowRect.y,
            fullWidth - startX,
            rowRect.height
        );


        var evt = Event.current;
        Vector2 mousePos = evt.mousePosition;

        // 2) Only consider hover during Repaint
        bool isHover = evt.type == EventType.Repaint && rowRect.Contains(mousePos);

        // base background
        if (!Selection.Contains(go) && !isHover)
            EditorGUI.DrawRect(boxRect, new Color(0.22f, 0.22f, 0.22f, 1f));
        else if (Selection.Contains(go))
            EditorGUI.DrawRect(boxRect, new Color(0.17f, 0.36f, 0.53f, 1f));
        else
            EditorGUI.DrawRect(boxRect, new Color(0.27f, 0.27f, 0.27f, 1f));

        // 2) gradient overlay
        if (!isHover && !Selection.Contains(go))
        {
            if (comp.UseGradient)
            {

                var gradientTex = GenerateGradientTexture(comp.FolderColor);
                GUI.DrawTexture(gradRect, gradientTex, ScaleMode.StretchToFill);
            }
            else
            {

                var gradientTex = GenerateFullColorTexture(comp.FolderColor);
                GUI.DrawTexture(gradRect, gradientTex, ScaleMode.StretchToFill);
            }
        }

        // 3) Centered label + underline
        var content = new GUIContent(comp.FolderName);
        var labelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = comp.FolderColor.grayscale > 0.5f ? Color.black : Color.white }
        };
        EditorGUI.LabelField(rowRect, content, labelStyle);

        if (comp.UnderlineEnabled)
        {
            Vector2 size = labelStyle.CalcSize(content);
            float x0 = boxRect.x + (boxRect.width - size.x) * 0.5f;
            float y0 = rowRect.y + rowRect.height * 0.5f + size.y * 0.5f + 1f;
            EditorGUI.DrawRect(new Rect(x0, y0, size.x, 1f), comp.FolderColor);
        }
    }

    private static Texture2D GenerateGradientTexture(Color color)
    {
        var tex = new Texture2D(2, 1, TextureFormat.ARGB32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixel(0, 0, new Color(color.r, color.g, color.b, 0f));
        tex.SetPixel(1, 0, color);
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateFullColorTexture(Color color)
    {
        var tex = new Texture2D(2, 1, TextureFormat.ARGB32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixel(0, 0, color);
        tex.SetPixel(1, 0, color);
        tex.Apply();
        return tex;
    }

    [PostProcessScene]
    private static void OnPostprocessScene()
    {
        if (!BuildPipeline.isBuildingPlayer)
            return;

        // 1) Gather all folder components, record their initial depth and index
        var folderInfos = Object
            .FindObjectsOfType<HierarchyFolder>()
            .Select(f => new
            {
                Folder = f,
                Depth = GetDepth(f.transform),
                Sibling = f.transform.GetSiblingIndex()
            })
            // 2) Process deepest folders first, then left-to-right
            .OrderByDescending(i => i.Depth)
            .ThenBy(i => i.Sibling)
            .ToArray();

        foreach (var info in folderInfos)
        {
            var goFolder = info.Folder.gameObject;
            var parentT = goFolder.transform.parent;
            int index = goFolder.transform.GetSiblingIndex();

            // Cache children so we can re-parent after
            int childCount = goFolder.transform.childCount;
            var children = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
                children[i] = goFolder.transform.GetChild(i);

            // Reparent each child into the folder's parent,
            // preserving their order starting at `index`
            for (int i = 0; i < childCount; i++)
            {
                children[i].SetParent(parentT);
                children[i].SetSiblingIndex(index + i);
            }

            // Destroy the now-empty folder GameObject
            Object.DestroyImmediate(goFolder);
        }
    }

    // Utility to compute nesting depth (root objects have depth 0)
    private static int GetDepth(Transform t)
    {
        int depth = 0;
        while (t.parent != null)
        {
            depth++;
            t = t.parent;
        }
        return depth;
    }
}
