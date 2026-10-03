using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FarmBeware.Data.Runtime;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "FarmBeware/Database/Item Database")]
public class ItemDatabase : ScriptableObject, IItemCatalog
{
    private static IItemCatalog _customCatalog;

    /// <summary>
    /// Injects an active catalog implementation (e.g. ItemRegistrySO from CoreBootstrapper/ServiceLocator).
    /// </summary>
    public static void SetCatalog(IItemCatalog catalog)
    {
        _customCatalog = catalog;
    }

    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemDatabase>("Database/ItemDatabase");
            }
            return _instance;
        }
    }

    [Header("Item Registry")]
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();

    private readonly Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>(System.StringComparer.OrdinalIgnoreCase);
    private bool isInitialized = false;

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        itemLookup.Clear();
        if (allItems == null) return;

        foreach (var item in allItems)
        {
            if (item == null) continue;

            // 1. Index by itemId
            if (!string.IsNullOrEmpty(item.itemId))
            {
                if (!itemLookup.ContainsKey(item.itemId))
                {
                    itemLookup.Add(item.itemId, item);
                }
            }

            // 2. Index by asset name
            if (!string.IsNullOrEmpty(item.name))
            {
                if (!itemLookup.ContainsKey(item.name))
                {
                    itemLookup.Add(item.name, item);
                }
            }

            // 3. Index normalized variants (without underscores or spaces)
            if (!string.IsNullOrEmpty(item.itemId))
            {
                string normId = item.itemId.Replace("_", "").Replace(" ", "");
                if (!itemLookup.ContainsKey(normId))
                {
                    itemLookup.Add(normId, item);
                }
            }

            if (!string.IsNullOrEmpty(item.name))
            {
                string normName = item.name.Replace("_", "").Replace(" ", "");
                if (!itemLookup.ContainsKey(normName))
                {
                    itemLookup.Add(normName, item);
                }
            }
        }
        isInitialized = true;
    }

    public ItemData GetItem(string itemId)
    {
        if (_customCatalog != null)
        {
            return _customCatalog.GetItem(itemId);
        }

        if (string.IsNullOrEmpty(itemId)) return null;
        if (!isInitialized || itemLookup.Count == 0) Initialize();

        // 1. Direct dictionary lookup (case-insensitive)
        if (itemLookup.TryGetValue(itemId, out ItemData item))
        {
            return item;
        }

        // 2. Normalized lookup (ignoring underscores and spaces)
        string normalizedTarget = itemId.Replace("_", "").Replace(" ", "");
        if (itemLookup.TryGetValue(normalizedTarget, out item))
        {
            return item;
        }

        // 3. Fallback linear search across allItems
        if (allItems != null)
        {
            foreach (var it in allItems)
            {
                if (it == null) continue;
                if (string.Equals(it.itemId, itemId, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(it.name, itemId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return it;
                }

                if (string.Equals((it.itemId ?? "").Replace("_", "").Replace(" ", ""), normalizedTarget, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(it.name.Replace("_", "").Replace(" ", ""), normalizedTarget, System.StringComparison.OrdinalIgnoreCase))
                {
                    return it;
                }
            }
        }

        Debug.LogWarning($"[ItemDatabase] Item dengan ID '{itemId}' tidak ditemukan di database!");
        return null;
    }

    public bool TryGetItem(string itemId, out ItemData item)
    {
        if (_customCatalog != null)
        {
            return _customCatalog.TryGetItem(itemId, out item);
        }

        item = GetItem(itemId);
        return item != null;
    }

    public List<ItemData> GetItemsByCategory(ItemCategory category)
    {
        if (_customCatalog != null)
        {
            return _customCatalog.GetItemsByCategory(category);
        }

        List<ItemData> result = new List<ItemData>();
        if (allItems == null) return result;

        foreach (var item in allItems)
        {
            if (item != null && item.category == category)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public IReadOnlyList<ItemData> GetAllItems()
    {
        if (_customCatalog != null)
        {
            return _customCatalog.GetAllItems();
        }

        return allItems;
    }

    public void SetItemsList(List<ItemData> items)
    {
        allItems = items;
        Initialize();
    }
}
