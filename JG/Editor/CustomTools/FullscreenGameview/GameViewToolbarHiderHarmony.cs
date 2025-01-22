#if UNITY_EDITOR
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using System.Reflection;

[InitializeOnLoad]
public static class GameViewToolbarHiderAlternative
{
    private static Harmony harmony;
    private static bool isToolbarHidden = false;
    private static float toolbarHeight = 20f; // Adjust if needed

    static GameViewToolbarHiderAlternative()
    {
        harmony = new Harmony("com.yourcompany.gameviewtoolbarhideralternative");
    }

    //[MenuItem("Tools/Toggle Alternate Toolbar Removal")]
    public static void ToggleAlternateToolbarRemoval()
    {
        isToolbarHidden = !isToolbarHidden;

        if (isToolbarHidden)
        {
            PatchMethods();
        }
        else
        {
            UnpatchMethods();
        }

        Menu.SetChecked("Tools/Toggle Alternate Toolbar Removal", isToolbarHidden);
    }

    //[MenuItem("Tools/Toggle Alternate Toolbar Removal", true)]
    public static bool ToggleAlternateToolbarRemovalValidate()
    {
        Menu.SetChecked("Tools/Toggle Alternate Toolbar Removal", isToolbarHidden);
        return true;
    }

    private static void PatchMethods()
    {
        var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
        {
            Debug.LogError("[GameViewToolbarHider] Could not find UnityEditor.GameView type.");
            return;
        }

        var doToolbarGUIMethod = gameViewType.GetMethod("DoToolbarGUI", BindingFlags.Instance | BindingFlags.NonPublic);
        if (doToolbarGUIMethod != null)
        {
            harmony.Patch(doToolbarGUIMethod,
                prefix: new HarmonyMethod(typeof(GameViewToolbarHiderAlternative), nameof(DoToolbarGUIPrefix)));
        }

        var getViewInWindowMethod = gameViewType.GetMethod("GetViewInWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        if (getViewInWindowMethod != null)
        {
            harmony.Patch(getViewInWindowMethod,
                postfix: new HarmonyMethod(typeof(GameViewToolbarHiderAlternative), nameof(GetViewInWindowPostfix)));
        }
    }

    private static void UnpatchMethods()
    {
        harmony.UnpatchAll("com.yourcompany.gameviewtoolbarhideralternative");
    }

    private static bool DoToolbarGUIPrefix()
    {
        // Skip drawing toolbar buttons
        return false;
    }

    private static void GetViewInWindowPostfix(ref Rect __result)
    {
        // Shift the result up by toolbarHeight, effectively hiding the toolbar area
        __result = new Rect(
            __result.x,
            __result.y - toolbarHeight,
            __result.width,
            __result.height + toolbarHeight
        );
    }
}
#endif
