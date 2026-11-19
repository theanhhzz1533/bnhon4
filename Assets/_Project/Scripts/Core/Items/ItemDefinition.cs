using System;

namespace Bnhon.Core.Items
{
    public enum ItemCategory
    {
        Material,
        Food,
        Medicine,
        Weapon,
        Ammo,
        Seed,
        Produce,
        Gift,
        Key,
        Quest,
        Blueprint,

        /// <summary>Used in a raid for an effect in the world (a decoy, a flare).</summary>
        Gadget
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Static description of an item type. Width and Height are the cells it
    /// occupies in a <see cref="Bnhon.Core.Inventory.GridInventory"/>.
    /// </summary>
    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public ItemCategory Category { get; }
        public ItemRarity Rarity { get; }
        public int Width { get; }
        public int Height { get; }
        public int MaxStack { get; }
        public int BaseValue { get; }

        public ItemDefinition(
            string id,
            string displayName,
            ItemCategory category,
            ItemRarity rarity = ItemRarity.Common,
            int width = 1,
            int height = 1,
            int maxStack = 1,
            int baseValue = 0)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Item id is required.", nameof(id));
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width), "Item size must be at least 1x1.");
            if (maxStack < 1) throw new ArgumentOutOfRangeException(nameof(maxStack));

            Id = id;
            DisplayName = displayName ?? id;
            Category = category;
            Rarity = rarity;
            Width = width;
            Height = height;
            MaxStack = maxStack;
            BaseValue = baseValue;
        }

        public override string ToString() => DisplayName;
    }
}
