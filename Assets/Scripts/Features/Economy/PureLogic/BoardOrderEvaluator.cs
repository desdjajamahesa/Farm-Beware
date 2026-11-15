using System;
using System.Collections.Generic;

namespace FeaturesEconomy.PureLogic
{
    /// <summary>
    /// Immutable pricing breakdown for a HayDay-style Board Order with the 1.2x bonus multiplier.
    /// Pure C# value type with zero Unity lifecycle dependencies and zero allocations.
    /// </summary>
    public readonly struct BoardOrderPriceBreakdown
    {
        public readonly int BaseUnitPrice;
        public readonly int BaseTotalPrice;
        public readonly int BoardUnitPrice;
        public readonly int BoardTotalReward;
        public readonly int BonusGold;

        public BoardOrderPriceBreakdown(
            int baseUnitPrice,
            int baseTotalPrice,
            int boardUnitPrice,
            int boardTotalReward,
            int bonusGold)
        {
            BaseUnitPrice = baseUnitPrice;
            BaseTotalPrice = baseTotalPrice;
            BoardUnitPrice = boardUnitPrice;
            BoardTotalReward = boardTotalReward;
            BonusGold = bonusGold;
        }
    }

    /// <summary>
    /// Serializable data model representing a single agricultural request pinned to the Farm Order Board.
    /// </summary>
    [Serializable]
    public sealed class BoardOrderSlot
    {
        public string OrderId;
        public string CustomerName;
        public string FlavorNote;
        public string ItemId;
        public int RequiredQuantity;
    }

    /// <summary>
    /// Pure C# evaluator and deterministic generator for HayDay-style Farm Board Orders.
    /// Generates washed clean-crop requests only and enforces the 1.2x sell price bonus.
    /// </summary>
    public static class BoardOrderEvaluator
    {
        public const float PriceMultiplier = 1.2f;
        public const int DefaultOrderCount = 4;

        private readonly struct OrderTemplate
        {
            public readonly string ItemId;
            public readonly int MinQty;
            public readonly int MaxQty;

            public OrderTemplate(string itemId, int minQty, int maxQty)
            {
                ItemId = itemId;
                MinQty = minQty;
                MaxQty = maxQty;
            }
        }

        private static readonly OrderTemplate[] CleanTemplates =
        {
            new OrderTemplate("crop_sweet_potato", 2, 4),
            new OrderTemplate("crop_taro", 1, 3),
            new OrderTemplate("crop_corn", 1, 3)
        };

        private static readonly string[] CustomerNames =
        {
            "Valley Bakery",
            "Riverside Inn",
            "Outpost Canteen",
            "Miller's Kitchen",
            "Town Grocer",
            "Harbor Pantry",
            "Highland Tavern",
            "Trailhead Chef"
        };

        private static readonly string[] CleanFlavorNotes =
        {
            "Washed & market-ready produce wanted urgently.",
            "Chef requested crisp, sink-washed harvest!",
            "Premium pantry delivery — +20% bonus paid.",
            "Freshly cleaned crops needed before dusk."
        };

        /// <summary>
        /// Calculates total Board Order reward at 1.2x of the normal merchant sell rate.
        /// Uses AwayFromZero rounding for consistent integer gold payouts.
        /// </summary>
        public static int CalculateBoardTotalReward(int baseUnitSellPrice, int quantity)
        {
            if (baseUnitSellPrice <= 0 || quantity <= 0)
                return 0;

            double scaled = baseUnitSellPrice * (double)quantity * PriceMultiplier;
            return Math.Max(1, (int)Math.Round(scaled, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Computes full normal vs 1.2x Board Order pricing breakdown for UI display and payout execution.
        /// </summary>
        public static BoardOrderPriceBreakdown EvaluatePrice(int baseUnitSellPrice, int quantity)
        {
            if (baseUnitSellPrice <= 0 || quantity <= 0)
                return new BoardOrderPriceBreakdown(0, 0, 0, 0, 0);

            int baseTotal = baseUnitSellPrice * quantity;
            int boardUnit = Math.Max(1, (int)Math.Round(baseUnitSellPrice * (double)PriceMultiplier, MidpointRounding.AwayFromZero));
            int boardTotal = CalculateBoardTotalReward(baseUnitSellPrice, quantity);
            int bonus = Math.Max(0, boardTotal - baseTotal);
            return new BoardOrderPriceBreakdown(baseUnitSellPrice, baseTotal, boardUnit, boardTotal, bonus);
        }

        /// <summary>
        /// Returns true if the given itemId matches a supported clean crop board template.
        /// </summary>
        public static bool IsCleanCropTemplateId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            for (int i = 0; i < CleanTemplates.Length; i++)
            {
                if (string.Equals(CleanTemplates[i].ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Generates daily pinned orders strictly using clean crop tiers (never dirty items).
        /// </summary>
        public static List<BoardOrderSlot> GenerateDailyOrders(int day, uint seed, int slotCount = DefaultOrderCount)
        {
            int safeDay = Math.Max(1, day);
            int count = Math.Max(1, slotCount);
            uint rng = seed == 0u ? (uint)(safeDay * 7919 + 104729) : seed;
            int cleanOffset = NextRange(ref rng, 0, CleanTemplates.Length);

            var orders = new List<BoardOrderSlot>(count);
            for (int i = 0; i < count; i++)
            {
                OrderTemplate template = SelectTemplateForSlot(i, cleanOffset, ref rng);
                orders.Add(BuildSlot(i, safeDay, template, ref rng));
            }

            return orders;
        }

        /// <summary>
        /// Replaces a completed or skipped slot with a fresh clean crop request.
        /// Avoids repeating the previous itemId when alternatives exist.
        /// </summary>
        public static BoardOrderSlot CreateReplacementOrder(
            int slotIndex,
            int day,
            uint rollSeed,
            string previousItemId = null)
        {
            int safeDay = Math.Max(1, day);
            uint rng = rollSeed == 0u ? (uint)(safeDay * 2654435761u + (uint)(slotIndex + 1) * 40503u) : rollSeed;

            OrderTemplate chosen = CleanTemplates[NextRange(ref rng, 0, CleanTemplates.Length)];
            if (!string.IsNullOrEmpty(previousItemId) && CleanTemplates.Length > 1 &&
                string.Equals(chosen.ItemId, previousItemId, StringComparison.OrdinalIgnoreCase))
            {
                int nextIdx = (Array.IndexOf(CleanTemplates, chosen) + 1 + NextRange(ref rng, 0, CleanTemplates.Length - 1)) % CleanTemplates.Length;
                chosen = CleanTemplates[nextIdx];
            }

            return BuildSlot(slotIndex, safeDay, chosen, ref rng);
        }

        private static OrderTemplate SelectTemplateForSlot(int slotIndex, int cleanOffset, ref uint rng)
        {
            if (slotIndex < CleanTemplates.Length)
            {
                int rotated = (cleanOffset + slotIndex) % CleanTemplates.Length;
                return CleanTemplates[rotated];
            }

            return CleanTemplates[NextRange(ref rng, 0, CleanTemplates.Length)];
        }

        private static BoardOrderSlot BuildSlot(int slotIndex, int day, OrderTemplate template, ref uint rng)
        {
            int dayCapDelta = day <= 1 ? 0 : 1;
            int minQty = template.MinQty;
            int maxQty = template.MaxQty + dayCapDelta;
            int qty = NextRange(ref rng, minQty, maxQty + 1);

            // Day 1 clean Taro/Corn stay approachable (1-2) while Sweet Potato scales slightly higher
            if (day <= 1 && template.ItemId != "crop_sweet_potato")
            {
                qty = NextRange(ref rng, 1, 3);
            }

            string customer = CustomerNames[NextRange(ref rng, 0, CustomerNames.Length)];
            string note = CleanFlavorNotes[NextRange(ref rng, 0, CleanFlavorNotes.Length)];
            uint orderHash = NextUInt(ref rng) % 9000u + 1000u;

            return new BoardOrderSlot
            {
                OrderId = $"ORD-D{day}-{slotIndex + 1}-{orderHash}",
                CustomerName = customer,
                FlavorNote = note,
                ItemId = template.ItemId,
                RequiredQuantity = qty
            };
        }

        private static uint NextUInt(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state == 0u ? 1u : state;
        }

        private static int NextRange(ref uint state, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt(ref state) % span);
        }
    }
}
