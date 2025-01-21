using UnityEditor;
using UnityEngine;

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

    // Toggle to keep scale uniform
    private static bool uniformScale;

    // A reusable style for small toggle/buttons (fixed width/height)
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
                padding = new RectOffset(0, 0, 0, 0)
            };
        }

        if (smallButtonStyle == null)
        {
            smallButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 20,
                fixedHeight = 20,
                padding = new RectOffset(0, 0, 0, 0)
            };
        }
    }

    public override void OnInspectorGUI()
    {
        // Make sure our styles are initialized
        EnsureStyles();

        Transform t = (Transform)target;

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

            // Copy (C)
            if (GUILayout.Button("C", smallButtonStyle))
            {
                positionClipboard = t.localPosition;
                positionClipboardValid = true;
            }

            // Paste (P)
            EditorGUI.BeginDisabledGroup(!positionClipboardValid);
            if (GUILayout.Button("P", smallButtonStyle))
            {
                Undo.RecordObject(t, "Paste Position");
                t.localPosition = positionClipboard;
            }
            EditorGUI.EndDisabledGroup();
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

            // Copy (C)
            if (GUILayout.Button("C", smallButtonStyle))
            {
                rotationClipboard = t.localEulerAngles;
                rotationClipboardValid = true;
            }

            // Paste (P)
            EditorGUI.BeginDisabledGroup(!rotationClipboardValid);
            if (GUILayout.Button("P", smallButtonStyle))
            {
                Undo.RecordObject(t, "Paste Rotation");
                t.localEulerAngles = rotationClipboard;
            }
            EditorGUI.EndDisabledGroup();
        }
        EditorGUILayout.EndHorizontal();

        // --- SCALE ---
        EditorGUILayout.BeginHorizontal();
        {
            // Scale field first
            Vector3 oldScale = t.localScale;
            EditorGUI.BeginChangeCheck();
            Vector3 newScale = EditorGUILayout.Vector3Field("Scale", oldScale);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Transform Scale");

                // If uniform scaling is enabled, make all axes match the one that changed most
                if (uniformScale)
                {
                    float dx = Mathf.Abs(newScale.x - oldScale.x);
                    float dy = Mathf.Abs(newScale.y - oldScale.y);
                    float dz = Mathf.Abs(newScale.z - oldScale.z);

                    if (dx >= dy && dx >= dz)
                    {
                        // x changed "the most"
                        newScale.y = newScale.x;
                        newScale.z = newScale.x;
                    }
                    else if (dy >= dx && dy >= dz)
                    {
                        // y changed "the most"
                        newScale.x = newScale.y;
                        newScale.z = newScale.y;
                    }
                    else
                    {
                        // z changed "the most"
                        newScale.x = newScale.z;
                        newScale.y = newScale.z;
                    }
                }

                t.localScale = newScale;
            }

            // Lock toggle (uniform scale) right before Reset
            GUIContent lockIcon = EditorGUIUtility.IconContent("LockIcon");
            lockIcon.tooltip = "Toggle uniform scaling";
            uniformScale = GUILayout.Toggle(uniformScale, lockIcon, smallToggleStyle);

            // Reset (R)
            if (GUILayout.Button("R", smallButtonStyle))
            {
                Undo.RecordObject(t, "Reset Scale");
                t.localScale = Vector3.one;
            }

            // Copy (C)
            if (GUILayout.Button("C", smallButtonStyle))
            {
                scaleClipboard = t.localScale;
                scaleClipboardValid = true;
            }

            // Paste (P)
            EditorGUI.BeginDisabledGroup(!scaleClipboardValid);
            if (GUILayout.Button("P", smallButtonStyle))
            {
                Undo.RecordObject(t, "Paste Scale");
                t.localScale = scaleClipboard;
            }
            EditorGUI.EndDisabledGroup();
        }
        EditorGUILayout.EndHorizontal();
    }
}
