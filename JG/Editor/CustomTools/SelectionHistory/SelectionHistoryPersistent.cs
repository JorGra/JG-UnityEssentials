// SelectionHistoryPersistent.cs
// Improved selection history with safer scene-object resolution, settings, and pruning.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SelectionHistorySystem
{
    // ----------------------------- SETTINGS ---------------------------------

    [FilePath("UserSettings/SelectionHistorySettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class SelectionHistorySettings : ScriptableSingleton<SelectionHistorySettings>
    {
        [Header("Resolution")]
        [Tooltip("When true, scene objects are only rehydrated if exactly one scene is loaded.")]
        public bool resolveSceneObjectsOnlyWhenSingleSceneLoaded = true;

        [Tooltip("When true, scene objects are only rehydrated if their owning scene is already loaded.")]
        public bool resolveOnlyIfOwningSceneLoaded = true;

        [Tooltip("When true and the owning scene is not loaded, the editor will load it additively to resolve a reference.")]
        public bool autoLoadOwningSceneIfMissing = false;

        [Header("History")]
        [Tooltip("Maximum number of history items to keep (older entries are pruned).")]
        public int maxHistory = 300;

        [Tooltip("If true, attempts to deduplicate consecutive identical selections.")]
        public bool dedupeConsecutive = true;

        [Header("Diagnostics")]
        public bool enableDebugLogging = false;

        [SerializeField] private int dataVersion = CURRENT_VERSION;
        public const int CURRENT_VERSION = 1;

        public void SaveSettings()
        {
            Save(true);
        }
    }

    // ----------------------------- PERSISTENT SINGLETON ---------------------------------

    [FilePath("UserSettings/SelectionHistoryPersistent.asset", FilePathAttribute.Location.ProjectFolder)]
    public class SelectionHistoryPersistent : ScriptableSingleton<SelectionHistoryPersistent>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<SelectionRecord> _history = new List<SelectionRecord>();
        [SerializeField] private List<SelectionRecord> _favorites = new List<SelectionRecord>();
        [SerializeField] private int _savedCurrentIndex = -1;

        [SerializeField] private int _dataVersion = CURRENT_VERSION;
        private const int CURRENT_VERSION = 2;

        private static double _lastSaveTime;
        private const double SAVE_THROTTLE_SECONDS = 0.5;

        public List<SelectionRecord> History => _history;
        public List<SelectionRecord> Favorites => _favorites;

        public int SavedCurrentIndex
        {
            get => _savedCurrentIndex;
            set => _savedCurrentIndex = value;
        }

        /// <summary>Add a new selection (utility for callers). Applies dedupe and size limit.</summary>
        public void AddSelection(Object[] objects)
        {
            var settings = SelectionHistorySettings.instance;

            var rec = new SelectionRecord(objects);
            if (settings.dedupeConsecutive && _history.Count > 0 && _history[_history.Count - 1].Matches(objects))
                return;

            _history.Add(rec);
            PruneBrokenAndTrim();
            ThrottledSave();
        }

        /// <summary>Delete broken entries and enforce max size.</summary>
        public void PruneBrokenAndTrim()
        {
            var settings = SelectionHistorySettings.instance;
            _history.RemoveAll(r => r == null || r.IsCompletelyBroken());
            _favorites.RemoveAll(r => r == null || r.IsCompletelyBroken());

            int max = Mathf.Max(1, settings.maxHistory);
            if (_history.Count > max)
                _history.RemoveRange(0, _history.Count - max);
        }

        /// <summary>Persist changes to disk in UserSettings (throttled).</summary>
        public void SaveData()
        {
            // Immediate save (explicit call)
            Save(true);
            _lastSaveTime = EditorApplication.timeSinceStartup;
        }

        private void ThrottledSave()
        {
            // Reduce asset churn if many updates happen quickly
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastSaveTime >= SAVE_THROTTLE_SECONDS)
            {
                Save(true);
                _lastSaveTime = now;
            }
            else
            {
                EditorApplication.delayCall -= DelayedSaveOnce;
                EditorApplication.delayCall += DelayedSaveOnce;
            }
        }

        private void DelayedSaveOnce()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastSaveTime >= SAVE_THROTTLE_SECONDS)
            {
                Save(true);
                _lastSaveTime = now;
            }
        }

        // ----- Versioning hooks -----
        public void OnBeforeSerialize() { /* future migrations could write */ }

        public void OnAfterDeserialize()
        {
            if (_dataVersion < CURRENT_VERSION)
            {
                // Example migration point if the data format changes later.
                _dataVersion = CURRENT_VERSION;
            }
        }
    }

    // ----------------------------- SELECTION RECORD ---------------------------------

    /// <summary>
    /// Each selection record can contain multiple objects (multi-selection).
    /// </summary>
    [Serializable]
    public class SelectionRecord
    {
        public List<AssetReference> references = new List<AssetReference>();

        public SelectionRecord() { }

        public SelectionRecord(Object[] objects)
        {
            if (objects == null) return;
            foreach (var obj in objects)
            {
                if (obj != null)
                    references.Add(new AssetReference(obj));
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

        public bool IsCompletelyBroken()
        {
            // if none of the references can be resolved, consider it broken
            foreach (var ar in references)
            {
                if (ar != null && ar.CanPotentiallyResolve())
                    return false;
            }
            return true;
        }
    }

    // ----------------------------- ASSET REFERENCE ---------------------------------

    /// <summary>
    /// Stores exactly one object, either:
    ///  - A persistent asset/folder/scene-file (GUID + localFileID), or
    ///  - A scene object (GlobalObjectId + sceneGuid + hierarchyPath + componentType).
    ///  The extra metadata lets us fallback gracefully without causing editor warnings.
    /// </summary>
    [Serializable]
    public class AssetReference
    {
        [SerializeField] private bool isPersistent;
        [SerializeField] private string guid;            // For project assets (incl. folders & scene assets)
        [SerializeField] private long localId;           // Sub-asset ID or 0 for main

        [SerializeField] private bool isSceneObject;
        [SerializeField] private GlobalObjectId globalId; // For scene objects (fast path)

        // Extra info for robust scene rehydration
        [SerializeField] private string sceneGuid;        // Owning scene GUID
        [SerializeField] private string hierarchyPath;    // "Root/Child/SubChild" (GameObject path)
        [SerializeField] private string componentTypeAQN; // AssemblyQualifiedName for Component, null for GameObject

        public AssetReference() { }

        public AssetReference(Object obj)
        {
            if (EditorUtility.IsPersistent(obj))
            {
                isPersistent = true;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var g, out long lid))
                {
                    guid = g;
                    localId = lid;
                }
            }
            else
            {
                isSceneObject = true;
                globalId = GlobalObjectId.GetGlobalObjectIdSlow(obj);

                // Capture scene GUID and hierarchy info for fallback
                var go = GetGameObjectFromObject(obj, out var compType);
                componentTypeAQN = compType;
                if (go != null)
                {
                    var scn = go.scene;
                    if (scn.IsValid() && !string.IsNullOrEmpty(scn.path))
                        sceneGuid = AssetDatabase.AssetPathToGUID(scn.path);

                    hierarchyPath = BuildHierarchyPath(go.transform);
                }
            }
        }

        public Object ToObject()
        {
            var settings = SelectionHistorySettings.instance;

            if (isPersistent && !string.IsNullOrEmpty(guid))
            {
                // Normal asset/folder/scene-file
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    return null;

                // If it's a .unity scene file, return SceneAsset (don't enumerate sub-assets)
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

                // Otherwise, load the specific sub-asset via localId
                Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in all)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var g2, out long lid2))
                        if (g2 == guid && lid2 == localId) return a;
                }
                return null;
            }

            if (isSceneObject)
            {
                // Guard: avoid cross-scene warnings when many scenes are open.
                if (settings.resolveSceneObjectsOnlyWhenSingleSceneLoaded && SceneManager.loadedSceneCount > 1)
                    return null;

                // If we know the owning scene, optionally require it to be loaded or auto-load it.
                string scenePath = !string.IsNullOrEmpty(sceneGuid) ? AssetDatabase.GUIDToAssetPath(sceneGuid) : null;
                bool sceneLoaded = IsScenePathLoaded(scenePath);

                if (settings.resolveOnlyIfOwningSceneLoaded && !sceneLoaded)
                {
                    if (settings.autoLoadOwningSceneIfMissing && !string.IsNullOrEmpty(scenePath))
                    {
                        try
                        {
                            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                            sceneLoaded = true;
                        }
                        catch (Exception e)
                        {
                            if (settings.enableDebugLogging)
                                Debug.LogWarning($"SelectionHistory: Failed to auto-load scene '{scenePath}': {e.Message}");
                            sceneLoaded = false;
                        }
                    }
                    if (!sceneLoaded)
                        return null;
                }

                // Fast path: use GlobalObjectId
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                if (obj != null)
                    return obj;

                // Fallback: resolve by hierarchy path + component type within the owning scene
                if (!string.IsNullOrEmpty(scenePath))
                {
                    var o2 = ResolveByHierarchy(scenePath, hierarchyPath, componentTypeAQN);
                    if (o2 != null)
                        return o2;
                }

                // As a last resort, try across all loaded scenes (only if single scene rule already passed)
                if (!string.IsNullOrEmpty(hierarchyPath))
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        var scn = SceneManager.GetSceneAt(i);
                        if (!scn.IsValid() || !scn.isLoaded) continue;
                        var o3 = ResolveInSceneByHierarchy(scn, hierarchyPath, componentTypeAQN);
                        if (o3 != null)
                            return o3;
                    }
                }

                return null;
            }

            return null;
        }

        public bool IsSameObject(Object obj)
        {
            if (obj == null) return false;

            if (isPersistent)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var g, out long lid))
                    return (g == guid && lid == localId);
                return false;
            }

            if (isSceneObject)
            {
                var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                if (gid.Equals(globalId))
                    return true;

                // Fallback equality: same scene + same hierarchy + same component type
                var go = GetGameObjectFromObject(obj, out var compType);
                string objSceneGuid = null;
                if (go != null && go.scene.IsValid() && !string.IsNullOrEmpty(go.scene.path))
                    objSceneGuid = AssetDatabase.AssetPathToGUID(go.scene.path);

                return objSceneGuid == sceneGuid
                       && BuildHierarchyPath(go?.transform) == hierarchyPath
                       && compType == componentTypeAQN;
            }

            return false;
        }

        public bool CanPotentiallyResolve()
        {
            if (isPersistent)
                return !string.IsNullOrEmpty(guid);
            if (isSceneObject)
                return globalId.identifierType != 0 || !string.IsNullOrEmpty(hierarchyPath) || !string.IsNullOrEmpty(sceneGuid);
            return false;
        }

        // ---------------------- Helpers ----------------------

        private static string BuildHierarchyPath(Transform t)
        {
            if (t == null) return null;
            // Build "Root/Child/SubChild"
            var stack = new System.Collections.Generic.Stack<string>();
            while (t != null && t.parent == null && t.gameObject.scene.IsValid() && t.gameObject.scene.isLoaded)
            {
                // if already at root, break after adding
                break;
            }
            // Walk up
            var cur = t;
            while (cur != null)
            {
                stack.Push(cur.name);
                cur = cur.parent;
            }
            return string.Join("/", stack.ToArray());
        }

        private static GameObject FindByHierarchyPathInScene(Scene scn, string path)
        {
            if (!scn.IsValid() || !scn.isLoaded || string.IsNullOrEmpty(path)) return null;

            var segments = path.Split('/');
            if (segments.Length == 0) return null;

            GameObject current = null;
            // root
            var roots = scn.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == segments[0])
                {
                    current = roots[i];
                    break;
                }
            }
            if (current == null) return null;

            // children
            for (int i = 1; i < segments.Length; i++)
            {
                var tr = current.transform;
                Transform found = null;
                for (int c = 0; c < tr.childCount; c++)
                {
                    var ch = tr.GetChild(c);
                    if (ch.name == segments[i])
                    {
                        found = ch;
                        break;
                    }
                }
                if (found == null) return null;
                current = found.gameObject;
            }

            return current;
        }

        private static bool IsScenePathLoaded(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == scenePath && s.isLoaded) return true;
            }
            return false;
        }

        private static Object ResolveByHierarchy(string scenePath, string path, string componentTypeAQN)
        {
            var scn = SceneManager.GetSceneByPath(scenePath);
            if (!scn.IsValid() || !scn.isLoaded) return null;
            return ResolveInSceneByHierarchy(scn, path, componentTypeAQN);
        }

        private static Object ResolveInSceneByHierarchy(Scene scn, string path, string componentTypeAQN)
        {
            var go = FindByHierarchyPathInScene(scn, path);
            if (go == null) return null;

            if (string.IsNullOrEmpty(componentTypeAQN))
                return go;

            var type = Type.GetType(componentTypeAQN);
            if (type == null) return null;

            var comp = go.GetComponent(type);
            return comp != null ? (Object)comp : null;
        }

        private static GameObject GetGameObjectFromObject(Object obj, out string componentTypeAQNOut)
        {
            componentTypeAQNOut = null;

            if (obj is GameObject go)
                return go;

            if (obj is Component comp)
            {
                componentTypeAQNOut = comp.GetType().AssemblyQualifiedName;
                return comp.gameObject;
            }

            return null;
        }
    }
}
