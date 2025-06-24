// Assets/Scripts/UI/AdvancedUIPrefabCatalog.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AdvancedUIPrefabCatalog",
                 menuName = "UI/Advanced Prefab Catalog")]
public class AdvancedUIPrefabCatalog : ScriptableObject
{
    // ========== ITEM DATA ==========
    [Serializable]
    public class Entry
    {
        public string category;           // e.g. "Charts"
        public string displayName;        // e.g. "Pie Chart"
        public GameObject prefab;

        [Tooltip("Lower numbers appear higher inside the category.")]
        public int orderInCategory = 0;
    }
    public List<Entry> entries = new();

    // ========== CATEGORY ORDER ==========
    [Serializable]
    public class CategoryOrder
    {
        public string category;               // must match Entry.category
        [Tooltip("Lower numbers appear first in the GameObject menu.")]
        public int order = 0;
    }
    [Tooltip("Leave empty to fall back to alphabetical order.")]
    public List<CategoryOrder> categoryOrders = new();
}
