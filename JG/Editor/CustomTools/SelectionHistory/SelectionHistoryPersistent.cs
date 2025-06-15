using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SelectionHistorySystem
{
    [FilePath("UserSettings/SelectionHistoryPersistent.asset", FilePathAttribute.Location.ProjectFolder)]
    public class SelectionHistoryPersistent : ScriptableSingleton<SelectionHistoryPersistent>
    {
        [SerializeField] private List<SelectionRecord> _history = new List<SelectionRecord>();
        [SerializeField] private List<SelectionRecord> _favorites = new List<SelectionRecord>();
        [SerializeField] private int _savedCurrentIndex = -1;

        public List<SelectionRecord> History => _history;
        public List<SelectionRecord> Favorites => _favorites;

        public int SavedCurrentIndex
        {
            get => _savedCurrentIndex;
            set => _savedCurrentIndex = value;
        }

        /// <summary>
        /// Persist changes to disk in UserSettings.
        /// </summary>
        public void SaveData()
        {
            // If your Unity version complains, remove the '(true)' parameter.
            Save(true);
        }
    }

    /// <summary>
    /// Each selection record can contain multiple objects (multi-selection).
    /// We store them in a list of AssetReference.
    /// </summary>
    [Serializable]
    public class SelectionRecord
    {
        public List<AssetReference> references = new List<AssetReference>();

        public SelectionRecord() { }

        public SelectionRecord(Object[] objects)
        {
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (obj != null)
                        references.Add(new AssetReference(obj));
                }
            }
        }

        public Object[] ToObjectArray()
        {
            var objs = new List<Object>();
            foreach (var ar in references)
            {
                var o = ar?.ToObject();
                if (o != null)
                    objs.Add(o);
            }
            return objs.ToArray();
        }

        public bool Matches(Object[] objects)
        {
            if (objects == null || objects.Length != references.Count)
                return false;

            for (int i = 0; i < objects.Length; i++)
            {
                if (!references[i].IsSameObject(objects[i]))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Stores exactly one object, either:
    ///  - A persistent asset/folder/scene-file (GUID + localFileID), or
    ///  - A scene object (GlobalObjectId) if it's not a persistent asset.
    /// </summary>
    [Serializable]
    public class AssetReference
    {
        [SerializeField] private bool isPersistent;
        [SerializeField] private string guid;   // For persistent project assets (incl. folders)
        [SerializeField] private long localId;  // sub-asset ID or 0 for main
        [SerializeField] private bool isSceneObject;
        [SerializeField] private GlobalObjectId globalId; // For scene objects

        public AssetReference() { }

        public AssetReference(Object obj)
        {
            if (EditorUtility.IsPersistent(obj))
            {
                // It's a project asset (folder or file in the Project window)
                isPersistent = true;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var g, out long lid))
                {
                    guid = g;
                    localId = lid;
                }
            }
            else
            {
                // It's a scene object => use GlobalObjectId
                isSceneObject = true;
                globalId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            }
        }

        public Object ToObject()
        {
            if (isPersistent && !string.IsNullOrEmpty(guid))
            {
                // Normal asset/folder/scene-file
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    return null;

                // If it's actually a .unity scene file, we load it as SceneAsset
                // to avoid enumerating sub-objects inside the scene file
                if (path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Return the main SceneAsset
                    return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                }

                // Otherwise, load normal asset(s) or folder
                Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in all)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var g2, out long lid2))
                    {
                        if (g2 == guid && lid2 == localId)
                            return a;
                    }
                }
                return null; // If no match found
            }
            else if (isSceneObject)
            {
                // Re-hydrate a scene object
                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
            }

            // If neither, return null
            return null;
        }

        public bool IsSameObject(Object obj)
        {
            if (obj == null)
                return false;

            if (isPersistent)
            {
                // Compare GUID + local ID
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var g, out long lid))
                {
                    return (g == guid && lid == localId);
                }
                return false;
            }
            else if (isSceneObject)
            {
                // Compare GlobalObjectId
                var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                return gid.Equals(globalId);
            }

            return false;
        }
    }
}
