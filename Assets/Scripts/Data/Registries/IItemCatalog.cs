using System.Collections.Generic;

namespace FarmBeware.Data.Runtime
{
    /// <summary>
    /// Abstraction contract for item catalogs and databases.
    /// Allows systems to look up ItemData without binding directly to concrete Resources or ScriptableObjects.
    /// </summary>
    public interface IItemCatalog
    {
        ItemData GetItem(string itemId);
        bool TryGetItem(string itemId, out ItemData item);
        IReadOnlyList<ItemData> GetAllItems();
        List<ItemData> GetItemsByCategory(ItemCategory category);
    }
}
