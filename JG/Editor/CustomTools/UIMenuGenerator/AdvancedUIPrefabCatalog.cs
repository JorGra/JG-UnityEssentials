// Assets/Scripts/UI/AdvancedUIPrefabCatalog.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AdvancedUIPrefabCatalog",
    menuName = "UI/Advanced Prefab Catalog",
    order = 0)]
public class AdvancedUIPrefabCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string category;      // e.g. "Forms" or "Charts"
        public string displayName;   // e.g. "Login Panel"
        public GameObject prefab;      // a prefab in your project
    }

    public List<Entry> entries = new();
}
