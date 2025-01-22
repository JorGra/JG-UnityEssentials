using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ScriptableObject), true)]
public class CombinedScriptableObjectDrawer : PropertyDrawer
{
    private bool foldout;
    private Editor editorInstance;

    // Dictionary to remember each property's foldout state across inspector draws
    private static Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    private const float ButtonWidth = 25f;

    private bool showTypePopup = false;
    private Type[] derivedTypes;
    private int selectedTypeIndex = 0;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1) Generate a unique key for this property to store/retrieve foldout state.
        //    We combine the current object’s InstanceID and property path.
        string key = property.serializedObject.targetObject.GetInstanceID() + "_" + property.propertyPath;

        // 2) Before drawing, see if we already have a stored foldout state. If we do,
        //    update our local 'foldout' from the dictionary. Otherwise, keep the
        //    private foldout's current value (default false).
        bool storedFoldout;
        if (foldoutStates.TryGetValue(key, out storedFoldout))
        {
            foldout = storedFoldout;
        }

        // Reserve space for the foldout, the object field, and the "+" button
        float foldoutWidth = 15f;
        Rect foldoutRect = new Rect(position.x, position.y, foldoutWidth, EditorGUIUtility.singleLineHeight);
        Rect objectFieldRect = new Rect(
            position.x + foldoutWidth,
            position.y,
            position.width - foldoutWidth - ButtonWidth - 5,
            EditorGUIUtility.singleLineHeight
        );
        Rect buttonRect = new Rect(objectFieldRect.xMax + 5, position.y, ButtonWidth, EditorGUIUtility.singleLineHeight);

        // Foldout arrow
        bool newFoldout = EditorGUI.Foldout(foldoutRect, foldout, GUIContent.none);
        if (newFoldout != foldout)
        {
            foldout = newFoldout;
            // 3) Whenever the foldout is toggled, store the new state in the dictionary
            foldoutStates[key] = foldout;
        }

        // Object field
        EditorGUI.PropertyField(objectFieldRect, property, label, false);

        // "+" button to create new ScriptableObjects
        if (GUI.Button(buttonRect, "+"))
        {
            // Determine the type (resolve array type if necessary)
            Type fieldType = fieldInfo.FieldType;
            bool isArray = false;

            if (fieldType.IsArray)
            {
                fieldType = fieldType.GetElementType();
                isArray = true;
            }
            // Optionally detect List<T> as well if needed

            derivedTypes = GetAllDerivedTypes(fieldType).ToArray();
            if (derivedTypes.Length == 0)
            {
                Debug.LogWarning($"No derived non-abstract types found for {fieldType.Name}.");
            }
            else if (derivedTypes.Length == 1)
            {
                CreateNewScriptableObject(property, derivedTypes[0], isArray);
            }
            else
            {
                showTypePopup = true;
                selectedTypeIndex = 0;
            }
        }

        // Popup to select a specific type if multiple derived types exist
        if (showTypePopup && derivedTypes != null && derivedTypes.Length > 1)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Select ScriptableObject type:");
            string[] typeNames = derivedTypes.Select(t => t.Name).ToArray();
            selectedTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeNames);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK", GUILayout.Width(40)))
            {
                showTypePopup = false;
                CreateNewScriptableObject(property, derivedTypes[selectedTypeIndex], property.isArray);
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(70)))
            {
                showTypePopup = false;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // If an object is assigned and the foldout is expanded: show inline editor
        if (property.objectReferenceValue != null && foldout)
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

                var so = editorInstance.serializedObject;
                var prop = so.GetIterator();
                bool enterChildren = true;
                float yOffset = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                while (prop.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (prop.name == "m_Script")
                        continue;

                    float propHeight = EditorGUI.GetPropertyHeight(prop, null, true);
                    Rect propRect = new Rect(position.x, yOffset, position.width, propHeight);
                    EditorGUI.PropertyField(propRect, prop, true);
                    yOffset += propHeight + EditorGUIUtility.standardVerticalSpacing;
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

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Start with the standard height for a single-line property
        float height = EditorGUIUtility.singleLineHeight;

        // 1) Generate the same key we used in OnGUI
        string key = property.serializedObject.targetObject.GetInstanceID() + "_" + property.propertyPath;

        // 2) See if we have a recorded foldout state. If yes, override the local foldout.
        bool storedFoldout;
        if (foldoutStates.TryGetValue(key, out storedFoldout))
        {
            foldout = storedFoldout;
        }

        // If the foldout is expanded, add the height of all child properties
        if (property.objectReferenceValue != null && foldout)
        {
            if (editorInstance == null || editorInstance.target != property.objectReferenceValue)
            {
                editorInstance = Editor.CreateEditor(property.objectReferenceValue);
            }
            if (editorInstance != null)
            {
                var so = editorInstance.serializedObject;
                so.Update();
                var prop = so.GetIterator();
                bool enterChildren = true;

                while (prop.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (prop.name == "m_Script")
                        continue;

                    height += EditorGUI.GetPropertyHeight(prop, null, true) + EditorGUIUtility.standardVerticalSpacing;
                }
            }
        }
        return height;
    }

    private void CreateNewScriptableObject(SerializedProperty property, Type type, bool isArray)
    {
        // Choose path
        string path = EditorUtility.SaveFilePanelInProject(
            "Save new " + type.Name,
            type.Name + ".asset",
            "asset",
            "Where should the new ScriptableObject be saved?"
        );
        if (string.IsNullOrEmpty(path))
            return; // Abort

        // Create, save, and reference the new object
        ScriptableObject newObj = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(newObj, path);
        AssetDatabase.Refresh();

        property.serializedObject.Update();

        if (isArray || property.isArray)
        {
            property.arraySize++;
            SerializedProperty newElement = property.GetArrayElementAtIndex(property.arraySize - 1);
            newElement.objectReferenceValue = newObj;
        }
        else
        {
            property.objectReferenceValue = newObj;
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    private IEnumerable<Type> GetAllDerivedTypes(Type baseType)
    {
        var derivedTypesList = new List<Type>();
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
                types = e.Types.Where(t => t != null).ToArray();
            }
            if (types == null)
                continue;

            foreach (Type t in types)
            {
                if (!t.IsAbstract && baseType.IsAssignableFrom(t))
                {
                    derivedTypesList.Add(t);
                }
            }
        }
        return derivedTypesList;
    }
}
