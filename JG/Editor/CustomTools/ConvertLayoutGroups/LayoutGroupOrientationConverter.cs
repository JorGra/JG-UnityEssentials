#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

internal static class LayoutGroupOrientationConverter
{
    private const string HToVMenu = "CONTEXT/HorizontalLayoutGroup/Convert to Vertical Layout Group";
    private const string VToHMenu = "CONTEXT/VerticalLayoutGroup/Convert to Horizontal Layout Group";

    // Context menu entries
    [MenuItem(HToVMenu)]
    private static void ConvertHToV(MenuCommand cmd)
    {
        var src = cmd.context as HorizontalLayoutGroup;
        if (src != null) Convert<HorizontalLayoutGroup, VerticalLayoutGroup>(src);
    }

    [MenuItem(VToHMenu)]
    private static void ConvertVToH(MenuCommand cmd)
    {
        var src = cmd.context as VerticalLayoutGroup;
        if (src != null) Convert<VerticalLayoutGroup, HorizontalLayoutGroup>(src);
    }

    // Keep menu items enabled only for the right component type
    [MenuItem(HToVMenu, true)]
    private static bool ValidateHToV(MenuCommand cmd) => cmd.context is HorizontalLayoutGroup;

    [MenuItem(VToHMenu, true)]
    private static bool ValidateVToH(MenuCommand cmd) => cmd.context is VerticalLayoutGroup;

    private static void Convert<TFrom, TTo>(TFrom from)
        where TFrom : HorizontalOrVerticalLayoutGroup
        where TTo : HorizontalOrVerticalLayoutGroup
    {
        var go = from.gameObject;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName($"Convert {typeof(TFrom).Name} → {typeof(TTo).Name} ({go.name})");

        // Capture state & settings BEFORE removing the old component
        int oldIndex = GetComponentIndex(go, from);
        bool wasEnabled = from.enabled;
        string json = EditorJsonUtility.ToJson(from); // contains shared fields (padding, spacing, alignment, child control/expand/scale, reverse arrangement when present)

        // Remove the old LayoutGroup FIRST to satisfy [DisallowMultipleComponent]
        Undo.DestroyObjectImmediate(from);

        // Add the destination component
        var to = Undo.AddComponent<TTo>(go);

        // Restore settings
        EditorJsonUtility.FromJsonOverwrite(json, to);
        to.enabled = wasEnabled;

        // Put it back where the original was
        MoveComponentToIndex(to, oldIndex);

        // Prefab & dirty
        PrefabUtility.RecordPrefabInstancePropertyModifications(to);
        EditorUtility.SetDirty(to);

        Debug.Log($"Converted {typeof(TFrom).Name} to {typeof(TTo).Name} on '{go.name}'.");
    }

    private static int GetComponentIndex(GameObject go, Component c)
    {
        var list = go.GetComponents<Component>().ToList();
        int i = list.IndexOf(c);
        return i >= 0 ? i : 1; // never above Transform
    }

    private static void MoveComponentToIndex(Component c, int targetIndex)
    {
        var components = c.gameObject.GetComponents<Component>().ToList();
        int current = components.IndexOf(c);
        if (current < 0) return;

        targetIndex = Mathf.Clamp(targetIndex, 1, components.Count - 1); // keep Transform at 0

        while (current > targetIndex) { ComponentUtility.MoveComponentUp(c); current--; }
        while (current < targetIndex) { ComponentUtility.MoveComponentDown(c); current++; }
    }
}
#endif
