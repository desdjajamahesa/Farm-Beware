using System.Collections.Generic;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Abstraction contract for item catalogs and databases.
    /// Decouples systems from concrete Resources paths or ScriptableObject assets.
    /// </summary>
    public interface IItemCatalog
    {
        ItemData GetItem(string itemId);
        bool TryGetItem(string itemId, out ItemData item);
        IReadOnlyList<ItemData> GetAllItems();
        List<ItemData> GetItemsByCategory(ItemCategory category);
    }
}
