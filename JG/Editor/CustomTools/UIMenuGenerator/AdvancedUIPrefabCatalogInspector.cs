// Assets/Editor/AdvancedUIPrefabCatalogInspector.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AdvancedUIPrefabCatalog))]
public class AdvancedUIPrefabCatalogInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Rebuild Advanced UI Menu"))
        {
            AdvancedUIMenuGenerator.Rebuild(forceDelete: true);
        }
    }
}
#endif
