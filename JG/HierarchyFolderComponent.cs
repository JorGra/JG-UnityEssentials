// HierarchyFolderComponent.cs (Place in runtime code folder, e.g. Assets/Scripts)
using UnityEngine;

/// <summary>
/// Marker for grouping items in the Hierarchy as a folder in the Editor.
/// At runtime, this GameObject is removed, and its children are reparented, so no transform overhead remains.
/// </summary>
[ExecuteInEditMode]
[DisallowMultipleComponent]
public class HierarchyFolderComponent : MonoBehaviour
{
    /// <summary>
    /// Display name for the folder in the Hierarchy.
    /// </summary>
    [SerializeField]
    private string folderName = "New Folder";

    /// <summary>
    /// Background gradient color (alpha at the right edge).
    /// </summary>
    [SerializeField]
    private Color folderColor = new Color(0.45f, 0.66f, 0.91f, 0.7f);

    /// <summary>
    /// Should an underline be drawn below the folder name?
    /// </summary>
    [SerializeField]
    private bool underline = true;

    /// <summary>
    /// Expose folder properties to the Editor.
    /// </summary>
    public string FolderName => folderName;
    public Color FolderColor => folderColor;
    public bool UnderlineEnabled => underline;

    private void OnValidate()
    {
        // Sync GameObject name with folderName
        if (name != folderName)
            name = folderName;
    }
}
