#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SelectionHistorySystem
{
    public class SelectionHistoryCore
    {
        private const int MaxHistoryCount = 20;

        // ScriptableSingleton
        private SelectionHistoryPersistent Persistent => SelectionHistoryPersistent.instance;
        private List<SelectionRecord> History => Persistent.History;
        private List<SelectionRecord> Favorites => Persistent.Favorites;

        private bool _ignoreNextSelectionChange;

        private int CurrentIndex
        {
            get => Persistent.SavedCurrentIndex;
            set
            {
                Persistent.SavedCurrentIndex = value;
                Persistent.SaveData();
            }
        }

        public int HistoryCount => History.Count;
        public int FavoritesCount => Favorites.Count;

        public int GetCurrentIndex() => CurrentIndex;

        public SelectionHistoryCore()
        {
            if (History.Count == 0)
            {
                CurrentIndex = -1;
            }
            else
            {
                if (CurrentIndex < 0 || CurrentIndex >= History.Count)
                    CurrentIndex = 0;
            }
        }

        public void OnEditorSelectionChanged(Object[] currentSelection)
        {
            if (_ignoreNextSelectionChange)
            {
                _ignoreNextSelectionChange = false;
                return;
            }

            if (currentSelection == null || currentSelection.Length == 0) return;

            var list = new List<Object>(currentSelection);
            list.RemoveAll(obj => obj == null);
            if (list.Count == 0) return;

            // If matches topmost, do nothing
            if (History.Count > 0 && History[0].Matches(list.ToArray()))
                return;

            // Insert front
            History.Insert(0, new SelectionRecord(list.ToArray()));
            CurrentIndex = 0;

            if (History.Count > MaxHistoryCount)
                History.RemoveRange(MaxHistoryCount, History.Count - MaxHistoryCount);

            Persistent.SaveData();
        }

        public void ApplySelectionFromHistory(int index)
        {
            if (index < 0 || index >= History.Count) return;
            var objects = History[index].ToObjectArray();
            _ignoreNextSelectionChange = true;
            Selection.objects = objects;
        }

        public void JumpToHistoryIndex(int index)
        {
            if (index < 0 || index >= History.Count) return;
            CurrentIndex = index;
            ApplySelectionFromHistory(CurrentIndex);
        }

        public void GoBackwardInHistory()
        {
            if (CurrentIndex < History.Count - 1)
            {
                CurrentIndex++;
                ApplySelectionFromHistory(CurrentIndex);
            }
        }

        public void GoForwardInHistory()
        {
            if (CurrentIndex > 0)
            {
                CurrentIndex--;
                ApplySelectionFromHistory(CurrentIndex);
            }
        }

        public Object[] GetHistoryItem(int index)
        {
            if (index < 0 || index >= History.Count) return null;
            return History[index].ToObjectArray();
        }

        #region Favorites
        public void ToggleFavorite(Object[] selection)
        {
            if (selection == null || selection.Length == 0) return;

            int idx = FindFavorite(selection);
            if (idx >= 0)
            {
                Favorites.RemoveAt(idx);
            }
            else
            {
                Favorites.Add(new SelectionRecord(selection));
            }
            Persistent.SaveData();
        }


        public bool IsFavorite(Object[] selection)
        {
            return FindFavorite(selection) >= 0;
        }

        public Object[] GetFavoriteItem(int index)
        {
            if (index < 0 || index >= Favorites.Count) return null;
            return Favorites[index].ToObjectArray();
        }

        private int FindFavorite(Object[] selection)
        {
            for (int i = 0; i < Favorites.Count; i++)
            {
                if (Favorites[i].Matches(selection))
                    return i;
            }
            return -1;
        }
        #endregion
    }
}
#endif
