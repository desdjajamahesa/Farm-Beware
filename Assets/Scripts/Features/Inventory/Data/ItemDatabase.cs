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

    private readonly Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>();
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

            string key = !string.IsNullOrEmpty(item.itemId) ? item.itemId : item.name;
            if (!itemLookup.ContainsKey(key))
            {
                itemLookup.Add(key, item);
            }
            else
            {
                Debug.LogWarning($"[ItemDatabase] ItemId duplikat terdeteksi: '{key}' pada item '{item.name}'!");
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

        if (itemLookup.TryGetValue(itemId, out ItemData item))
        {
            return item;
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
