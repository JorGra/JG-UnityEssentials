using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ReflectionAnalyzerWindow : EditorWindow
{
    private string className = "UnityEngine.Transform"; // Default example class
    private Type selectedType = null;
    private MethodInfo[] methods = new MethodInfo[0];
    private PropertyInfo[] properties = new PropertyInfo[0];
    private Vector2 methodScrollPos;
    private Vector2 propertyScrollPos;

    [MenuItem("Tools/Reflection Analyzer", false, 2505)]
    public static void ShowWindow()
    {
        GetWindow<ReflectionAnalyzerWindow>("Reflection Analyzer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Reflection Analyzer", EditorStyles.boldLabel);

        // Input field for class name
        className = EditorGUILayout.TextField("Class Name (Namespace.ClassName)", className);

        // Button to load class details
        if (GUILayout.Button("Get Methods and Properties"))
        {
            if (!string.IsNullOrWhiteSpace(className))
            {
                try
                {
                    LoadClassData(className);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError("Please enter a valid class name.");
            }
        }

        if (selectedType != null)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Class: {selectedType.FullName}", EditorStyles.boldLabel);

            // Display Methods
            GUILayout.Label("Methods", EditorStyles.boldLabel);
            methodScrollPos = EditorGUILayout.BeginScrollView(methodScrollPos, GUILayout.Height(200));
            foreach (var method in methods)
            {
                string methodSignature = GetMethodSignature(method);
                EditorGUILayout.SelectableLabel(methodSignature, EditorStyles.textField, GUILayout.Height(20));
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            // Display Properties
            GUILayout.Label("Properties", EditorStyles.boldLabel);
            propertyScrollPos = EditorGUILayout.BeginScrollView(propertyScrollPos, GUILayout.Height(200));
            foreach (var property in properties)
            {
                string propertyInfo = GetPropertyInfo(property);
                EditorGUILayout.SelectableLabel(propertyInfo, EditorStyles.textField, GUILayout.Height(20));
            }
            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// Loads the methods and properties of the specified class.
    /// </summary>
    private void LoadClassData(string className)
    {
        selectedType = GetTypeByName(className);

        if (selectedType == null)
        {
            Debug.LogError($"Class '{className}' not found. Ensure the name includes the full namespace, e.g., 'UnityEngine.Transform'.");
            methods = new MethodInfo[0];
            properties = new PropertyInfo[0];
            return;
        }

        methods = selectedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        properties = selectedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
    }

    /// <summary>
    /// Searches for a type in all loaded assemblies.
    /// </summary>
    private static Type GetTypeByName(string className)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(type => type.FullName == className);
    }

    /// <summary>
    /// Generates a readable method signature for display.
    /// </summary>
    private static string GetMethodSignature(MethodInfo method)
    {
        string accessModifiers = GetAccessModifiers(method);
        string returnType = method.ReturnType.Name;
        string parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        return $"{accessModifiers} {returnType} {method.Name}({parameters})";
    }

    /// <summary>
    /// Generates a readable property info for display.
    /// </summary>
    private static string GetPropertyInfo(PropertyInfo property)
    {
        string accessModifiers = GetAccessModifiers(property);
        string propertyType = property.PropertyType.Name;
        string getter = property.CanRead ? "get;" : "";
        string setter = property.CanWrite ? "set;" : "";
        return $"{accessModifiers} {propertyType} {property.Name} {{ {getter} {setter} }}";
    }

    /// <summary>
    /// Extracts access modifiers from a MethodInfo or PropertyInfo.
    /// </summary>
    private static string GetAccessModifiers(MemberInfo member)
    {
        string access = "private";
        if (member is MethodInfo method)
        {
            if (method.IsPublic) access = "public";
            else if (method.IsPrivate) access = "private";
            else if (method.IsFamily) access = "protected";
            else if (method.IsAssembly) access = "internal";
            if (method.IsStatic) access += " static";
            if (method.IsVirtual && !method.IsAbstract) access += " virtual";
            if (method.IsAbstract) access += " abstract";
        }
        else if (member is PropertyInfo property)
        {
            MethodInfo getMethod = property.GetGetMethod(true);
            if (getMethod != null)
            {
                if (getMethod.IsPublic) access = "public";
                else if (getMethod.IsPrivate) access = "private";
                else if (getMethod.IsFamily) access = "protected";
                else if (getMethod.IsAssembly) access = "internal";
                if (getMethod.IsStatic) access += " static";
            }
        }
        return access;
    }
}
