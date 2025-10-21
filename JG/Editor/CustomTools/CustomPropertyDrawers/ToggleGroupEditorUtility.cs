using System.Linq;
using System.Reflection;
using UnityEditor;

namespace UI.Tools.Editor.CustomPropertyDrawers
{
    static class ToggleGroupEditorUtility
    {

        public static bool ShouldDisplay(SerializedProperty property, FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                return true;
            }

            var attributes = fieldInfo.GetCustomAttributes(typeof(ToggleGroupMemberAttribute), true)
                                      .Cast<ToggleGroupMemberAttribute>()
                                      .ToArray();
            if (attributes.Length == 0)
            {
                return true;
            }

            foreach (var attr in attributes)
            {
                if (!CheckToggle(property, attr))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ShouldDisplay(SerializedProperty property, ToggleGroupMemberAttribute attribute)
        {
            return attribute == null || CheckToggle(property, attribute);
        }

        static bool CheckToggle(SerializedProperty property, ToggleGroupMemberAttribute attribute)
        {
            if (attribute == null || string.IsNullOrEmpty(attribute.ToggleProperty))
            {
                return true;
            }

            var toggle = FindSiblingProperty(property, attribute.ToggleProperty);
            if (toggle == null)
            {
                return true;
            }

            return toggle.propertyType != SerializedPropertyType.Boolean || toggle.boolValue;
        }

        static SerializedProperty FindSiblingProperty(SerializedProperty property, string childName)
        {
            if (property == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            string parentPath = GetParentPath(property.propertyPath);
            string childPath = string.IsNullOrEmpty(parentPath) ? childName : $"{parentPath}.{childName}";
            return property.serializedObject.FindProperty(childPath);
        }

        static string GetParentPath(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                return string.Empty;
            }

            int lastDot = propertyPath.LastIndexOf('.');
            return lastDot >= 0 ? propertyPath.Substring(0, lastDot) : string.Empty;
        }
    }
}
