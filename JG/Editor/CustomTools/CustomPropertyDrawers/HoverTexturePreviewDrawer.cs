using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Sprite))]
[CustomPropertyDrawer(typeof(Texture2D))]
public class HoverTexturePreviewDrawer : PropertyDrawer
{
    const float kPreviewSize = 96f; // square preview size (px)
    const float kPadding = 4f;  // gap between field and preview

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // ── 1. Draw the standard object field ────────────────────────────────────
        EditorGUI.PropertyField(position, property, label, true);

        // ── 2. Only continue during the Repaint pass (so we can draw *after* all
        //       other controls and safely change GUI.depth). ─────────────────────
        if (Event.current.type != EventType.Repaint) return;

        // ── 3. Early outs: not hovering, no value, no preview yet. ───────────────
        if (!position.Contains(Event.current.mousePosition)) return;
        if (property.objectReferenceValue == null) return;

        Texture preview = AssetPreview.GetAssetPreview(property.objectReferenceValue);
        if (preview == null) return;   // still generating → draw nothing

        // ── 4. Compute the desired rect (top-right of the property). ────────────
        Rect previewRect = new Rect(
            position.xMax - kPreviewSize,
            position.y - kPreviewSize - kPadding,
            kPreviewSize,
            kPreviewSize);

        // Convert to screen space to test against the Inspector window bounds
        Vector2 screenPos = GUIUtility.GUIToScreenPoint(previewRect.position);
        if (EditorWindow.focusedWindow == null)
            return; // no focused window, can't determine position

        Rect inspectorScreenRect = EditorWindow.focusedWindow.position;

        // If the rect would stick out of the top of the visible Inspector, flip
        // it below the field instead (still right-aligned).
        if (screenPos.y < inspectorScreenRect.yMin)
            previewRect.y = position.yMax + kPadding;

        // ── 5. Force the preview to be drawn on top of everything else. ─────────
        int oldDepth = GUI.depth;
        GUI.depth = int.MinValue;      // lower depth ⇒ on top

        GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox);
        GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit, true);

        GUI.depth = oldDepth;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUI.GetPropertyHeight(property, label, true);
}
