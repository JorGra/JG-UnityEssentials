using UnityEngine;
using UnityEditor;

public class SelectionHistoryWindow : EditorWindow
{
    private static SelectionHistoryWindow _instance;

    private Vector2 _scrollPositionHistory;
    private Vector2 _scrollPositionFavorites;

    private float _splitterPercent = 0.6f;
    private const float SplitterThickness = 4f;
    private bool _isResizing;
    private const string EditorPrefKey_Splitter = "SelectionHistoryWindow_SplitterPercent";

    [MenuItem("Tools/Selection History")]
    public static void ShowWindow()
    {
        GetWindow<SelectionHistoryWindow>("Selection History");
    }

    private void OnEnable()
    {
        _instance = this;
        _splitterPercent = EditorPrefs.GetFloat(EditorPrefKey_Splitter, 0.6f);
    }

    private void OnDisable()
    {
        if (_instance == this) _instance = null;
        EditorPrefs.SetFloat(EditorPrefKey_Splitter, _splitterPercent);
    }

    public static void RepaintIfOpen()
    {
        _instance?.Repaint();
    }

    private void OnGUI()
    {
        float topPanelHeight = position.height * _splitterPercent;
        float bottomPanelHeight = position.height - topPanelHeight - SplitterThickness;

        Rect historyRect = new Rect(0, 0, position.width, topPanelHeight);
        Rect splitterRect = new Rect(0, topPanelHeight, position.width, SplitterThickness);
        Rect favoritesRect = new Rect(0, topPanelHeight + SplitterThickness, position.width, bottomPanelHeight);

        GUILayout.BeginArea(historyRect);
        DrawHistoryPanel();
        GUILayout.EndArea();

        EditorGUI.DrawRect(splitterRect, new Color(0.2f, 0.2f, 0.2f, 1f));
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);
        HandleSplitter(splitterRect);

        GUILayout.BeginArea(favoritesRect);
        DrawFavoritesPanel();
        GUILayout.EndArea();
    }

    private void HandleSplitter(Rect splitterRect)
    {
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown:
                if (splitterRect.Contains(e.mousePosition))
                {
                    _isResizing = true;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (_isResizing)
                {
                    _splitterPercent += e.delta.y / position.height;
                    _splitterPercent = Mathf.Clamp(_splitterPercent, 0.1f, 0.9f);
                    Repaint();
                }
                break;
            case EventType.MouseUp:
                if (_isResizing)
                {
                    _isResizing = false;
                    e.Use();
                }
                break;
        }
    }

    private void DrawHistoryPanel()
    {
        _scrollPositionHistory = EditorGUILayout.BeginScrollView(_scrollPositionHistory);
        int count = SelectionHistory.HistoryCount;
        for (int i = 0; i < count; i++)
        {
            Object[] objs = SelectionHistory.GetHistoryItem(i);
            if (objs == null || objs.Length == 0)
                continue;

            GUIContent content = CreateHierarchyStyleLabel(objs);
            Rect rowRect = EditorGUILayout.GetControlRect(false, 18f);

            bool isCurrent = (i == SelectionHistory.CurrentIndex);
            if (isCurrent)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.20f));
            }

            // star
            Rect starRect = rowRect;
            starRect.x = rowRect.xMax - 20f;
            starRect.width = 20f;

            Rect labelRect = rowRect;
            labelRect.width -= 20f;

            if (GUI.Button(labelRect, content, EditorStyles.label))
            {
                SelectionHistory.JumpToHistoryIndex(i);
            }

            bool isFav = SelectionHistory.IsFavorite(objs);
            string starText = isFav ? "★" : "☆";
            if (GUI.Button(starRect, starText, EditorStyles.label))
            {
                SelectionHistory.ToggleFavorite(objs);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawFavoritesPanel()
    {
        EditorGUILayout.LabelField("Favorites:", EditorStyles.boldLabel);
        _scrollPositionFavorites = EditorGUILayout.BeginScrollView(_scrollPositionFavorites);

        int favCount = SelectionHistory.FavoritesCount;
        if (favCount == 0)
        {
            EditorGUILayout.HelpBox("No Favorites yet.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < favCount; i++)
            {
                Object[] objs = SelectionHistory.GetFavoriteItem(i);
                if (objs == null || objs.Length == 0)
                    continue;

                GUIContent content = CreateHierarchyStyleLabel(objs);
                Rect rowRect = EditorGUILayout.GetControlRect(false, 18f);

                Rect starRect = rowRect;
                starRect.x = rowRect.xMax - 20f;
                starRect.width = 20f;

                Rect labelRect = rowRect;
                labelRect.width -= 20f;

                if (GUI.Button(labelRect, content, EditorStyles.label))
                {
                    Selection.objects = objs;
                }

                if (GUI.Button(starRect, "★", EditorStyles.label))
                {
                    SelectionHistory.ToggleFavorite(objs);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private GUIContent CreateHierarchyStyleLabel(Object[] objs)
    {
        GUIContent content = new GUIContent();
        if (objs[0] != null)
            content.image = AssetPreview.GetMiniThumbnail(objs[0]);

        if (objs.Length == 1)
            content.text = objs[0].name;
        else
            content.text = $"{objs[0].name} (+{objs.Length - 1})";

        return content;
    }
}
