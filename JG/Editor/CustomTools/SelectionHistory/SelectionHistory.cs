using UnityEditor;
using UnityEngine;
using SelectionHistorySystem;

[InitializeOnLoad]
public static class SelectionHistory
{
    private static readonly SelectionHistoryCore _core = new SelectionHistoryCore();
    private static int _lastFrameUsed = -1;

    static SelectionHistory()
    {
        // Editor events
        Selection.selectionChanged += OnSelectionChanged;
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowGUI;

        // After domain reload, repaint window so it shows re-hydrated references
        EditorApplication.delayCall += () => SelectionHistoryWindow.RepaintIfOpen();
    }

    private static void OnSelectionChanged()
    {
        _core.OnEditorSelectionChanged(Selection.objects);
        SelectionHistoryWindow.RepaintIfOpen();
    }

    private static void OnSceneGUI(SceneView sceneView) => HandleMouseNavigation();
    private static void OnProjectWindowGUI(string guid, Rect selectionRect) => HandleMouseNavigation();

    private static void HandleMouseNavigation()
    {
        Event e = Event.current;
        if (e == null) return;

        if (e.type == EventType.MouseDown && Time.frameCount != _lastFrameUsed)
        {
            if (e.button == 3)
            {
                _core.GoBackwardInHistory();
                e.Use();
                _lastFrameUsed = Time.frameCount;
                SelectionHistoryWindow.RepaintIfOpen();
            }
            else if (e.button == 4)
            {
                _core.GoForwardInHistory();
                e.Use();
                _lastFrameUsed = Time.frameCount;
                SelectionHistoryWindow.RepaintIfOpen();
            }
        }
    }

    #region Public API

    public static int HistoryCount => _core.HistoryCount;
    public static int CurrentIndex => _core.GetCurrentIndex();
    public static Object[] GetHistoryItem(int index) => _core.GetHistoryItem(index);

    public static void JumpToHistoryIndex(int index)
    {
        _core.JumpToHistoryIndex(index);
        SelectionHistoryWindow.RepaintIfOpen();
    }

    public static void GoBackwardInHistory()
    {
        _core.GoBackwardInHistory();
        SelectionHistoryWindow.RepaintIfOpen();
    }

    public static void GoForwardInHistory()
    {
        _core.GoForwardInHistory();
        SelectionHistoryWindow.RepaintIfOpen();
    }

    public static int FavoritesCount => _core.FavoritesCount;
    public static Object[] GetFavoriteItem(int index) => _core.GetFavoriteItem(index);

    public static void ToggleFavorite(Object[] selection)
    {
        _core.ToggleFavorite(selection);
        SelectionHistoryWindow.RepaintIfOpen();
    }

    public static bool IsFavorite(Object[] selection) => _core.IsFavorite(selection);

    #endregion
}
