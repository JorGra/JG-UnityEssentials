using UnityEditor;
using UnityEngine;

//[CustomPropertyDrawer(typeof(ScriptableObject), true)]
public class InlineScriptableObjectDrawer : PropertyDrawer
{
    private bool foldout;
    private Editor editorInstance;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue != null)
        {
            // Foldout Pfeil + Object Field
            Rect foldoutRect = new Rect(position.x, position.y, 15, EditorGUIUtility.singleLineHeight);
            Rect objectFieldRect = new Rect(position.x + 15, position.y, position.width - 15, EditorGUIUtility.singleLineHeight);

            foldout = EditorGUI.Foldout(foldoutRect, foldout, GUIContent.none);
            EditorGUI.PropertyField(objectFieldRect, property, label, false);

            if (foldout)
            {
                EditorGUI.indentLevel++;
                if (editorInstance == null || editorInstance.target != property.objectReferenceValue)
                {
                    editorInstance = Editor.CreateEditor(property.objectReferenceValue);
                }

                if (editorInstance != null)
                {
                    EditorGUI.BeginChangeCheck();
                    editorInstance.serializedObject.Update();

                    // Alle Properties bis auf "m_Script" anzeigen
                    var so = editorInstance.serializedObject;
                    var prop = so.GetIterator();
                    var yOffset = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    bool enterChildren = true;
                    while (prop.NextVisible(enterChildren))
                    {
                        if (prop.name == "m_Script")
                        {
                            enterChildren = false;
                            continue;
                        }

                        float propHeight = EditorGUI.GetPropertyHeight(prop, null, true);
                        Rect propRect = new Rect(position.x, yOffset, position.width, propHeight);
                        EditorGUI.PropertyField(propRect, prop, true);
                        yOffset += propHeight + EditorGUIUtility.standardVerticalSpacing;
                        enterChildren = false;
                    }

                    so.ApplyModifiedProperties();
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUI.PropertyField(position, property, label, false);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (property.objectReferenceValue != null && foldout && editorInstance != null)
        {
            var so = editorInstance.serializedObject;
            so.Update();
            var prop = so.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script") continue;
                height += EditorGUI.GetPropertyHeight(prop, null, true) + EditorGUIUtility.standardVerticalSpacing;
            }
        }
        return height;
    }
}
