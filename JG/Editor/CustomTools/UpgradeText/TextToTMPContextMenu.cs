// File: Assets/Editor/TextToTMPContextMenu.cs
// Place this script anywhere inside an “Editor” folder.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;          // legacy UI Text
using TMPro;                  // TextMeshProUGUI

/// Adds “Upgrade to TextMeshPro” under the gear menu of UI.Text.
internal static class TextToTMPContextMenu
{
    // ---------- Context-menu entry ----------
    [MenuItem("CONTEXT/Text/Upgrade to TextMeshPro", false, 2000)]
    private static void Upgrade(MenuCommand cmd)
    {
        var src = (Text)cmd.context;                 // the Text we clicked on
        var go = src.gameObject;

        // ----- record one undo group so Ctrl+Z puts everything back -----
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        // ----- cache values we can copy straight across -----
        string txt = src.text;
        Color color = src.color;
        int fontSize = src.fontSize;
        FontStyle fontStyle = src.fontStyle;
        TextAnchor anchor = src.alignment;
        bool rich = src.supportRichText;
        bool raycast = src.raycastTarget;
        HorizontalWrapMode hWrap = src.horizontalOverflow;
        VerticalWrapMode vWrap = src.verticalOverflow;

        // ----- delete old component -----
        Undo.DestroyObjectImmediate(src);

        // ----- add TextMeshProUGUI -----
        var tmp = Undo.AddComponent<TextMeshProUGUI>(go);

        tmp.text = txt;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.raycastTarget = raycast;
        tmp.richText = rich;
        tmp.fontStyle = ConvertFontStyle(fontStyle);
        tmp.alignment = ConvertAnchor(anchor);
        tmp.enableAutoSizing = false;

        // wrapping equivalents
        tmp.enableWordWrapping = hWrap == HorizontalWrapMode.Wrap;
        if (vWrap == VerticalWrapMode.Truncate)
            tmp.overflowMode = TextOverflowModes.Truncate;
        else
            tmp.overflowMode = TextOverflowModes.Overflow;

        // mark scene dirty so the change is saved
        EditorUtility.SetDirty(tmp);
        Undo.CollapseUndoOperations(group);
    }

    // ---------- validate so the item only shows once ----------
    [MenuItem("CONTEXT/Text/Upgrade to TextMeshPro", true)]
    private static bool UpgradeValidate(MenuCommand cmd) => cmd.context is Text;

    // ---------- helper conversions ----------
    private static FontStyles ConvertFontStyle(FontStyle s) => s switch
    {
        FontStyle.Bold => FontStyles.Bold,
        FontStyle.Italic => FontStyles.Italic,
        FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
        _ => FontStyles.Normal
    };

    private static TextAlignmentOptions ConvertAnchor(TextAnchor a) => a switch
    {
        TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
        TextAnchor.UpperCenter => TextAlignmentOptions.Top,
        TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
        TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
        TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
        TextAnchor.MiddleRight => TextAlignmentOptions.Right,
        TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
        TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
        TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
        _ => TextAlignmentOptions.Center
    };
}
#endif
