using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SelectionHistory
{
    // Holds arrays of objects because Unity.Selection can have multiple items at once.
    // Index 0 = most recent selection; Count-1 = oldest.
    private static List<Object[]> _selectionHistory = new List<Object[]>();

    // Points to which selection is currently active in the history.
    // 0 = newest; larger index = older.
    private static int _currentIndex = -1;

    // Prevents multiple triggers of the same back/forward event in one frame.
    private static int _lastFrameUsed = -1;

    // When we explicitly set Selection.objects, we don't want that to cause another "new entry."
    private static bool _ignoreNextSelectionChange;

    static SelectionHistory()
    {
        // Called after the editor reloads/compiles. Subscribe to relevant events.
        Selection.selectionChanged += OnSelectionChanged;
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowGUI;
    }

    /// <summary>
    /// Called whenever the user changes selection in the Editor.
    /// </summary>
    private static void OnSelectionChanged()
    {
        // If this was triggered by our own ApplySelectionFromHistory, skip adding it.
        if (_ignoreNextSelectionChange)
        {
            _ignoreNextSelectionChange = false;
            return;
        }

        // If no valid objects are selected, do nothing.
        if (Selection.objects == null || Selection.objects.Length == 0) return;

        // Filter out null references.
        List<Object> nonNull = new List<Object>(Selection.objects);
        nonNull.RemoveAll(obj => obj == null);
        if (nonNull.Count == 0) return;

        // Check if it's already at the top (i.e., same as _selectionHistory[0]).
        if (_selectionHistory.Count > 0 && AreSelectionsEqual(_selectionHistory[0], nonNull.ToArray()))
        {
            // Already the top entry, do not re-add.
            return;
        }

        // Insert the new selection at index 0 (newest).
        _selectionHistory.Insert(0, nonNull.ToArray());
        _currentIndex = 0;

        // Limit to last 20 items.
        if (_selectionHistory.Count > 20)
        {
            _selectionHistory.RemoveRange(20, _selectionHistory.Count - 20);
        }

        // Repaint the History Window so we see the new entry immediately.
        SelectionHistoryWindow.RepaintIfOpen();
    }

    /// <summary>
    /// Utility: compares two Object[] arrays for identical references.
    /// </summary>
    private static bool AreSelectionsEqual(Object[] a, Object[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    /// <summary>
    /// Called for every GUI update in the Scene View. Check for mouse back/forward clicks here.
    /// </summary>
    private static void OnSceneGUI(SceneView sceneView)
    {
        HandleMouseNavigation();
    }

    /// <summary>
    /// Called for every item drawn in the Project Window. Check for mouse back/forward clicks here too.
    /// </summary>
    private static void OnProjectWindowGUI(string guid, Rect selectionRect)
    {
        HandleMouseNavigation();
    }

    /// <summary>
    /// Checks the current event for back/forward mouse buttons (3 & 4).
    /// Increments or decrements _currentIndex to move older/newer in the history.
    /// </summary>
    private static void HandleMouseNavigation()
    {
        Event e = Event.current;
        if (e == null) return;

        if (e.type == EventType.MouseDown && Time.frameCount != _lastFrameUsed)
        {
            // Mouse button 3 = "Back" => older => index++ if possible
            if (e.button == 3)
            {
                GoBackwardInHistory();
                e.Use();
                _lastFrameUsed = Time.frameCount;
            }
            // Mouse button 4 = "Forward" => newer => index-- if possible
            else if (e.button == 4)
            {
                GoForwardInHistory();
                e.Use();
                _lastFrameUsed = Time.frameCount;
            }
        }
    }

    /// <summary>
    /// Go "back" in history => older => increase index if we can.
    /// </summary>
    public static void GoBackwardInHistory()
    {
        if (_currentIndex < _selectionHistory.Count - 1)
        {
            _currentIndex++;
            ApplySelectionFromHistory(_currentIndex);
        }
    }

    /// <summary>
    /// Go "forward" in history => newer => decrease index if we can.
    /// </summary>
    public static void GoForwardInHistory()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            ApplySelectionFromHistory(_currentIndex);
        }
    }

    /// <summary>
    /// Sets the editor's Selection.objects to the specified history index,
    /// ignoring the next selection changed event to prevent duplicates.
    /// </summary>
    private static void ApplySelectionFromHistory(int index)
    {
        if (index < 0 || index >= _selectionHistory.Count) return;

        _ignoreNextSelectionChange = true;
        Selection.objects = _selectionHistory[index];

        // Repaint in case the user is viewing the window; helps reflect the new "current" highlight.
        SelectionHistoryWindow.RepaintIfOpen();
    }

    // Expose for the Editor Window:
    public static int HistoryCount => _selectionHistory.Count;
    public static int CurrentIndex => _currentIndex;

    public static Object[] GetHistoryItem(int index)
    {
        if (index < 0 || index >= _selectionHistory.Count) return null;
        return _selectionHistory[index];
    }

    /// <summary>
    /// If user clicks an entry in the window, jump directly to that index.
    /// </summary>
    public static void JumpToHistoryIndex(int index)
    {
        if (index < 0 || index >= _selectionHistory.Count) return;
        _currentIndex = index;
        ApplySelectionFromHistory(_currentIndex);
    }
}

public class SelectionHistoryWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    private static SelectionHistoryWindow _instance;

    [MenuItem("Tools/Selection History")]
    public static void ShowWindow()
    {
        GetWindow<SelectionHistoryWindow>("Selection History");
    }

    private void OnEnable()
    {
        _instance = this;
    }

    private void OnDisable()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>
    /// Lets the SelectionHistory script repaint this window immediately if it's open.
    /// </summary>
    public static void RepaintIfOpen()
    {
        if (_instance != null)
        {
            _instance.Repaint();
        }
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        int count = SelectionHistory.HistoryCount;
        for (int i = 0; i < count; i++)
        {
            Object[] objs = SelectionHistory.GetHistoryItem(i);
            if (objs == null || objs.Length == 0)
                continue;

            // Prepare small icon + text (similar to hierarchy).
            GUIContent content = CreateHierarchyStyleLabel(objs);

            // Reserve a row ~18 px high for mini icons
            Rect rowRect = EditorGUILayout.GetControlRect(false, 18f);

            // Highlight if this is the currently 'active' index
            bool isCurrent = (i == SelectionHistory.CurrentIndex);
            if (isCurrent)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.20f));
            }

            // Render clickable row
            GUIStyle rowStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                imagePosition = ImagePosition.ImageLeft,
                fixedHeight = 16f
            };

            if (GUI.Button(rowRect, content, rowStyle))
            {
                SelectionHistory.JumpToHistoryIndex(i);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        //DrawNavButtons();
    }

    /// <summary>
    /// Create a label with small icon (mini thumbnail) plus text.
    /// If multiple objects are in the same history record, show the first plus "(+X)".
    /// </summary>
    private GUIContent CreateHierarchyStyleLabel(Object[] objs)
    {
        GUIContent content = new GUIContent();

        // Use the first object’s mini thumbnail
        if (objs[0] != null)
        {
            content.image = AssetPreview.GetMiniThumbnail(objs[0]);
        }

        if (objs.Length == 1)
        {
            content.text = objs[0].name;
        }
        else
        {
            content.text = $"{objs[0].name} (+{objs.Length - 1})";
        }

        return content;
    }

    /// <summary>
    /// Optional older/newer nav buttons at the bottom.
    /// </summary>
    private void DrawNavButtons()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = (SelectionHistory.CurrentIndex < SelectionHistory.HistoryCount - 1);
        if (GUILayout.Button("Older <<"))
        {
            SelectionHistory.GoBackwardInHistory();
        }

        GUI.enabled = (SelectionHistory.CurrentIndex > 0);
        if (GUILayout.Button(">> Newer"))
        {
            SelectionHistory.GoForwardInHistory();
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }
}
