using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FarmBeware.Data.Runtime
{
    /// <summary>
    /// ScriptableObject implementation of IItemCatalog.
    /// Provides central item definition registry without reliance on Resources.Load.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemRegistrySO", menuName = "FarmBeware/Registries/Item Registry")]
    public class ItemRegistrySO : ScriptableObject, IItemCatalog
    {
        [Header("Item Registry")]
        [SerializeField] private List<ItemData> items = new List<ItemData>();

        private readonly Dictionary<string, ItemData> lookup = new Dictionary<string, ItemData>();
        private bool isInitialized = false;

        private void OnEnable()
        {
            InitializeLookup();
        }

        public void InitializeLookup()
        {
            lookup.Clear();
            if (items == null) return;

            foreach (var item in items)
            {
                if (item == null) continue;
                string key = !string.IsNullOrEmpty(item.itemId) ? item.itemId : item.name;
                if (!lookup.ContainsKey(key))
                {
                    lookup.Add(key, item);
                }
            }
            isInitialized = true;
        }

        public ItemData GetItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (!isInitialized || lookup.Count == 0) InitializeLookup();

            lookup.TryGetValue(itemId, out ItemData item);
            return item;
        }

        public bool TryGetItem(string itemId, out ItemData item)
        {
            item = GetItem(itemId);
            return item != null;
        }

        public IReadOnlyList<ItemData> GetAllItems()
        {
            return items;
        }

        public List<ItemData> GetItemsByCategory(ItemCategory category)
        {
            var result = new List<ItemData>();
            if (items == null) return result;

            foreach (var item in items)
            {
                if (item != null && item.category == category)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public void SetItemsList(List<ItemData> newItems)
        {
            items = newItems;
            InitializeLookup();
        }
    }
}
