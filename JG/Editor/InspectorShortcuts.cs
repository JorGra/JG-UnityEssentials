using UnityEditor;

public static class InspectorShortcuts
{
    // taken from: http://answers.unity3d.com/questions/282959/set-inspector-lock-by-code.html
    [MenuItem("Tools/Toggle Inspector Lock %q")] // Ctrl + Q
    static void ToggleInspectorLock()
    {
        ActiveEditorTracker.sharedTracker.isLocked = !ActiveEditorTracker.sharedTracker.isLocked;
        ActiveEditorTracker.sharedTracker.ForceRebuild();
    }

}
