using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Custom property drawer for fields decorated with [ScriptableObjectCreate].
/// This adds a "Create" button next to the field, allowing you to create and assign
/// a new ScriptableObject of a derived type.
/// </summary>
//[CustomPropertyDrawer(typeof(ScriptableObjectCreateAttribute))]
//[CustomPropertyDrawer(typeof(ScriptableObject), true)]
public class ScriptableObjectCreateDrawer : PropertyDrawer
{
    private const float ButtonWidth = 60f;

    // Whether we’re currently displaying the type popup
    private bool showTypePopup = false;

    // Possible derived types for the current field
    private Type[] derivedTypes;

    // Currently selected index in the popup
    private int selectedTypeIndex = 0;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1) Draw the label
        position = EditorGUI.PrefixLabel(position, label);

        // 2) Reserve space for the object field + "Create" button
        Rect objectFieldRect = new Rect(position.x, position.y, position.width - ButtonWidth - 5, position.height);
        Rect buttonRect = new Rect(objectFieldRect.xMax + 5, position.y, ButtonWidth, position.height);

        // 3) Draw the normal object field (Unity default)
        EditorGUI.PropertyField(objectFieldRect, property, GUIContent.none);

        // 4) Draw our "Create" button
        if (GUI.Button(buttonRect, "Create"))
        {
            // Figure out if it's an array or a single object
            Type fieldType = fieldInfo.FieldType;
            bool isArray = false;

            // If it's an array (e.g. WeaponData[]), get the element type (WeaponData)
            if (fieldType.IsArray)
            {
                fieldType = fieldType.GetElementType();
                isArray = true;
            }
            // If you need to handle List<T> similarly:
            // if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            // {
            //     fieldType = fieldType.GetGenericArguments()[0];
            //     isArray = true; // We can treat it similarly
            // }

            // Gather all possible derived (non-abstract) types
            derivedTypes = GetAllDerivedTypes(fieldType).ToArray();

            if (derivedTypes.Length == 0)
            {
                Debug.LogWarning($"No valid derived (non-abstract) types found for: {fieldType.Name}");
                return;
            }
            else if (derivedTypes.Length == 1)
            {
                // Only one possible type => directly create the SO
                CreateNewScriptableObject(property, derivedTypes[0], isArray);
            }
            else
            {
                // Multiple => show a popup to pick which type
                showTypePopup = true;
                selectedTypeIndex = 0;
            }
        }

        // 5) If the user clicked "Create" and multiple derived types exist, show the popup
        if (showTypePopup && derivedTypes != null && derivedTypes.Length > 1)
        {
            // Draw an inline popup in a small box
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Select Type to Create:");

            string[] typeNames = derivedTypes.Select(t => t.Name).ToArray();
            selectedTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeNames);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("OK", GUILayout.Width(40)))
            {
                showTypePopup = false;
                CreateNewScriptableObject(property, derivedTypes[selectedTypeIndex],
                                          property.isArray);
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(60)))
            {
                showTypePopup = false;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Actually creates the new ScriptableObject instance, prompts for a save path,
    /// and assigns it to the property (or adds it to the array if isArray = true).
    /// </summary>
    private void CreateNewScriptableObject(SerializedProperty property, Type type, bool isArray)
    {
        // 1) Ask user where to save the asset
        string path = EditorUtility.SaveFilePanelInProject(
            "Save new " + type.Name,
            type.Name + ".asset",
            "asset",
            "Specify where to save the new ScriptableObject."
        );

        if (string.IsNullOrEmpty(path))
        {
            // User canceled
            return;
        }

        // 2) Create the instance
        ScriptableObject newObj = ScriptableObject.CreateInstance(type);

        // 3) Save to asset
        AssetDatabase.CreateAsset(newObj, path);
        AssetDatabase.Refresh();

        // 4) Assign it to the property
        property.serializedObject.Update();

        if (property.isArray)
        {
            // Expand array by 1
            property.arraySize++;
            // Set the last element
            SerializedProperty newElement = property.GetArrayElementAtIndex(property.arraySize - 1);
            newElement.objectReferenceValue = newObj;
        }
        else
        {
            // Single object field
            property.objectReferenceValue = newObj;
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Finds all non-abstract classes that derive from baseType.
    /// Uses reflection across all assemblies and catches ReflectionTypeLoadException.
    /// </summary>
    private IEnumerable<Type> GetAllDerivedTypes(Type baseType)
    {
        var derivedTypes = new List<Type>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var asm in assemblies)
        {
            Type[] types = null;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // Only use the valid types that were loaded
                types = e.Types.Where(t => t != null).ToArray();
            }

            if (types == null) continue;

            foreach (Type t in types)
            {
                if (!t.IsAbstract && baseType.IsAssignableFrom(t))
                {
                    derivedTypes.Add(t);
                }
            }
        }

        return derivedTypes;
    }
}
