using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for Transform with reset, copy, paste, uniform scale,
/// and Copy All (CA) / Paste All (PA) buttons integrated into Position and Rotation rows.
/// </summary>
[CustomEditor(typeof(Transform))]
public class TransformResetEditor : Editor
{
    // Clipboards for individual values
    private static Vector3 positionClipboard;
    private static bool positionClipboardValid;

    private static Vector3 rotationClipboard;
    private static bool rotationClipboardValid;

    private static Vector3 scaleClipboard;
    private static bool scaleClipboardValid;

    // Toggle to keep scale proportional
    private static bool uniformScale;

    // A reusable style for small toggle/buttons
    private static GUIStyle smallToggleStyle;
    private static GUIStyle smallButtonStyle;

    private void EnsureStyles()
    {
        if (smallToggleStyle == null)
        {
            smallToggleStyle = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 20,
                fixedHeight = 20,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        if (smallButtonStyle == null)
        {
            smallButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 20,
                fixedHeight = 20,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
        }
    }

    /// <inheritdoc/>
    public override void OnInspectorGUI()
    {
        EnsureStyles();

        Transform t = (Transform)target;
        Vector3 oldScale = t.localScale;

        // --- POSITION ---
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = EditorGUILayout.Vector3Field("Position", t.localPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Transform Position");
                t.localPosition = newPosition;
            }

            // Reset (R)
            if (GUILayout.Button("R", smallButtonStyle))
            {
                Undo.RecordObject(t, "Reset Position");
                t.localPosition = Vector3.zero;
            }

            // Copy position (C)
            if (GUILayout.Button("C", smallButtonStyle))
            {
                positionClipboard = t.localPosition;
                positionClipboardValid = true;
            }

            // Paste position (P)
            EditorGUI.BeginDisabledGroup(!positionClipboardValid);
            if (GUILayout.Button("P", smallButtonStyle))
            {
                Undo.RecordObject(t, "Paste Position");
                t.localPosition = positionClipboard;
            }
            EditorGUI.EndDisabledGroup();

            // Copy All (CA)
            if (GUILayout.Button(new GUIContent("CA", "Copy All Transforms"), smallButtonStyle))
            {
                // copy pos, rot, scale
                positionClipboard = t.localPosition;
                rotationClipboard = t.localEulerAngles;
                scaleClipboard = t.localScale;
                positionClipboardValid = rotationClipboardValid = scaleClipboardValid = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        // --- ROTATION ---
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newRotation = EditorGUILayout.Vector3Field("Rotation", t.localEulerAngles);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Transform Rotation");
                t.localEulerAngles = newRotation;
            }

            // Reset (R)
            if (GUILayout.Button("R", smallButtonStyle))
            {
                Undo.RecordObject(t, "Reset Rotation");
                t.localEulerAngles = Vector3.zero;
            }

            // Copy rotation (C)
            if (GUILayout.Button("C", smallButtonStyle))
            {
                rotationClipboard = t.localEulerAngles;
                rotationClipboardValid = true;
            }

            // Paste rotation (P)
            EditorGUI.BeginDisabledGroup(!rotationClipboardValid);
            if (GUILayout.Button("P", smallButtonStyle))
            {
                Undo.RecordObject(t, "Paste Rotation");
                t.localEulerAngles = rotationClipboard;
            }
            EditorGUI.EndDisabledGroup();

            // Paste All (PA)
            bool allValid = positionClipboardValid && rotationClipboardValid && scaleClipboardValid;
            EditorGUI.BeginDisabledGroup(!allValid);
            if (GUILayout.Button(new GUIContent("PA", "Paste All Transforms"), smallButtonStyle))
            {
                Undo.RecordObject(t, "Paste All Transforms");
                t.localPosition = positionClipboard;
                t.localEulerAngles = rotationClipboard;
                t.localScale = scaleClipboard;
            }
            EditorGUI.EndDisabledGroup();
        }
        EditorGUILayout.EndHorizontal();

        // --- SCALE ---
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newScale = EditorGUILayout.Vector3Field("Scale", oldScale);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Transform Scale");

                if (uniformScale)
                {
                    // proportional scaling: apply ratio of change on primary axis
                    Vector3 delta = newScale - oldScale;
                    // detect primary axis change
                    if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && Mathf.Abs(delta.x) >= Mathf.Abs(delta.z) && oldScale.x != 0f)
                    {
                        float ratio = newScale.x / oldScale.x;
                        newScale = oldScale * ratio;
                    }
                    else if (Mathf.Abs(delta.y) >= Mathf.Abs(delta.x) && Mathf.Abs(delta.y) >= Mathf.Abs(delta.z) && oldScale.y != 0f)
                    {
                        float ratio = newScale.y / oldScale.y;
                        newScale = oldScale * ratio;
                    }
                    else if (oldScale.z != 0f)
                    {
                        float ratio = newScale.z / oldScale.z;
                        newScale = oldScale * ratio;
                    }
                }

                t.localScale = newScale;
            }


            // Reset (R)
            if (GUILayout.Button("R", smallButtonStyle))
            {
                Undo.RecordObject(t, "Reset Scale");
                t.localScale = Vector3.one;
            }

            // Copy scale (C)
            if (GUILayout.Button("C", smallButtonStyle))
            {
                scaleClipboard = t.localScale;
                scaleClipboardValid = true;
            }

            // Paste scale (P)
            EditorGUI.BeginDisabledGroup(!scaleClipboardValid);
            if (GUILayout.Button("P", smallButtonStyle))
            {
                Undo.RecordObject(t, "Paste Scale");
                t.localScale = scaleClipboard;
            }

            // Uniform scale toggle
            GUIContent lockIcon = EditorGUIUtility.IconContent("LockIcon");
            lockIcon.tooltip = "Toggle uniform scaling";
            uniformScale = GUILayout.Toggle(uniformScale, lockIcon, smallToggleStyle);
            EditorGUI.EndDisabledGroup();
        }
        EditorGUILayout.EndHorizontal();
    }
}
