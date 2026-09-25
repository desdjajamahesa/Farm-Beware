using System.Collections.Generic;

namespace FarmBeware.Logic
{
    /// <summary>
    /// Pure logic for resolving which character SkinnedMeshRenderer parts are active
    /// based on selected outfit variants. Zero UnityEngine dependencies — testable without Play Mode.
    /// </summary>
    public static class OutfitPartResolver
    {
        public enum Category { Top, Bottom, Shoes, Hat }

        public struct PartConfig
        {
            public string variant1Name; // e.g., "cloth1"
            public string variant2Name; // e.g., "cloth2"
            public Category category;
        }

        /// <summary>
        /// Maps each category to its two variant renderer names on the character model.
        /// </summary>
        private static readonly Dictionary<Category, PartConfig> CategoryMap = new Dictionary<Category, PartConfig>
        {
            { Category.Top,    new PartConfig { variant1Name = "cloth1", variant2Name = "cloth2", category = Category.Top } },
            { Category.Bottom, new PartConfig { variant1Name = "pants1", variant2Name = "pants2", category = Category.Bottom } },
            { Category.Shoes,  new PartConfig { variant1Name = "shoes1_left", variant2Name = "shoes2_left", category = Category.Shoes } },
            { Category.Hat,    new PartConfig { variant1Name = "hat", variant2Name = "", category = Category.Hat } }
        };

        /// <summary>
        /// Returns the renderer names that should be ENABLED for a given category and variant index.
        /// For Hat: variantIndex 0 = Unequipped (none enabled), variantIndex 1 = Hat enabled.
        /// For Top/Bottom/Shoes: variantIndex 0 = variant1, variantIndex 1 = variant2.
        /// </summary>
        public static HashSet<string> GetActiveRendererNames(Category category, int variantIndex)
        {
            var active = new HashSet<string>();
            var config = CategoryMap[category];

            if (category == Category.Hat)
            {
                // Hat: 0 = unequipped (none), 1 = hat on
                if (variantIndex == 1 && !string.IsNullOrEmpty(config.variant1Name))
                    active.Add(config.variant1Name);
            }
            else
            {
                // Top/Bottom/Shoes: 0 = variant1, 1 = variant2
                if (variantIndex == 0)
                    active.Add(config.variant1Name);
                else if (variantIndex == 1 && !string.IsNullOrEmpty(config.variant2Name))
                    active.Add(config.variant2Name);
            }

            // Shoes has left/right pair
            if (category == Category.Shoes && variantIndex >= 0)
            {
                if (variantIndex == 0)
                    active.Add("shoes1_right");
                else if (variantIndex == 1)
                    active.Add("shoes2_right");
            }

            return active;
        }

        /// <summary>
        /// Returns all renderer names that should be DISABLED for a category (the opposite variants).
        /// </summary>
        public static HashSet<string> GetInactiveRendererNames(Category category, int variantIndex)
        {
            var inactive = new HashSet<string>();
            var config = CategoryMap[category];

            if (category == Category.Hat)
            {
                if (variantIndex == 1)
                {
                    // Hat equipped: no inactive (hat was off)
                }
                else if (variantIndex == 0)
                {
                    // Hat unequipped: hat should be off
                    if (!string.IsNullOrEmpty(config.variant1Name))
                        inactive.Add(config.variant1Name);
                }
            }
            else
            {
                if (variantIndex == 0)
                {
                    if (!string.IsNullOrEmpty(config.variant2Name))
                        inactive.Add(config.variant2Name);
                    if (category == Category.Shoes)
                        inactive.Add("shoes2_right");
                }
                else if (variantIndex == 1)
                {
                    inactive.Add(config.variant1Name);
                    if (category == Category.Shoes)
                        inactive.Add("shoes1_right");
                }
            }

            return inactive;
        }

        /// <summary>
        /// Returns total variant count per category (2 for Top/Bottom/Shoes, 2 for Hat = Unequipped + Equipped).
        /// </summary>
        public static int GetVariantCount(Category category)
        {
            return 2; // All categories have 2 variants (including Hat: 0=Unequipped, 1=Equipped)
        }

        /// <summary>
        /// Returns display label for a category+variant combo.
        /// </summary>
        public static string GetVariantLabel(Category category, int variantIndex)
        {
            if (category == Category.Hat)
                return variantIndex == 0 ? "Unequipped" : "Hat";

            string[] labels = { "Variant 1", "Variant 2" };
            return labels[variantIndex];
        }
    }
}