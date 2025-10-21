using UnityEditor;
using UnityEngine;

namespace UI.Tools.Editor.CustomPropertyDrawers
{
    [CustomPropertyDrawer(typeof(TogglePropertyGroupAttribute))]
    public sealed class TogglePropertyGroupDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attribute = (TogglePropertyGroupAttribute)this.attribute;
            EditorGUI.BeginProperty(position, label, property);

            var toggleLabel = string.IsNullOrEmpty(attribute.GroupLabel)
                ? label
                : new GUIContent(attribute.GroupLabel, label.tooltip);

            property.boolValue = EditorGUI.ToggleLeft(position, toggleLabel, property.boolValue);

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(ToggleGroupMemberAttribute))]
    public sealed class ToggleGroupMemberDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!ToggleGroupEditorUtility.ShouldDisplay(property, (ToggleGroupMemberAttribute)attribute))
            {
                return 0f;
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ToggleGroupMemberAttribute)attribute;
            if (!ToggleGroupEditorUtility.ShouldDisplay(property, attr))
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel += attr.IndentLevel;
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.indentLevel = prevIndent;

            EditorGUI.EndProperty();
        }
    }
}
