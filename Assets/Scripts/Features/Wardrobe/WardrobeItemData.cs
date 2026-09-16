using UnityEngine;

namespace FarmBeware.Logic
{
    /// <summary>
    /// ScriptableObject representing a single wardrobe item with 3D model reference.
    /// Used for dynamic inventory population in the Wardrobe UI grid.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWardrobeItem", menuName = "Wardrobe/Wardrobe Item Data")]
    public class WardrobeItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        public Sprite icon;

        [Header("3D Model Reference")]
        [Tooltip("The 3D prefab/avatar part that this item represents (e.g., cloth2, shoes2_left).")]
        public GameObject previewPrefab;

        [Header("Category Classification")]
        [Tooltip("Which category this item belongs to (Upper Body, Lower Body, Accessories).")]
        public OutfitPartResolver.Category category = OutfitPartResolver.Category.Top;

        [Header("Optional Metadata")]
        [TextArea] public string description;

        /// <summary>
        /// Returns a clone of this item data. Useful for UI preview state without affecting the original.
        /// </summary>
        public WardrobeItemData Clone()
        {
            var clone = ScriptableObject.CreateInstance<WardrobeItemData>();
            clone.itemId = itemId;
            clone.displayName = displayName;
            clone.icon = icon;
            clone.previewPrefab = previewPrefab;
            clone.category = category;
            clone.description = description;
            return clone;
        }
    }
}