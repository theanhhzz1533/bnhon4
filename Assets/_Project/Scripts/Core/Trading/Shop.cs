using System;
using System.Collections.Generic;
using Bnhon.Core.Economy;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;

namespace Bnhon.Core.Trading
{
    /// <summary>One line of a shop: what it sells, in what bundle, for how much, and how many are left today.</summary>
    public sealed class ShopEntry
    {
        public ItemDefinition Item { get; }

        /// <summary>Items received per purchase (ammo is sold by the box).</summary>
        public int Bundle { get; }
        public int Price { get; }
        public int MaxStock { get; }

        /// <summary>Chance per day that this line is restocked. Below 1 for goods that are not always available.</summary>
        public double RestockChance { get; }

        public int Stock { get; internal set; }

        /// <summary>The story flag that puts this entry on sale. Null for an entry that is always sold.</summary>
        public string RequiredFlag { get; set; }

        public ShopEntry(ItemDefinition item, int bundle, int price, int maxStock, double restockChance)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Bundle = Math.Max(1, bundle);
            Price = Math.Max(0, price);
            MaxStock = Math.Max(0, maxStock);
            RestockChance = restockChance;
        }
    }

    /// <summary>
    /// The base shop, like a vendor in Escape From Duckov: a limited stock that refreshes
    /// each day, and it buys anything that has a value, at a fraction of that value.
    /// </summary>
    public sealed class Shop
    {
        /// <summary>The shop pays this share of an item's base value.</summary>
        public const double SellFactor = 0.5;

        /// <summary>What items sell for here, as a share of their value. Starts at <see cref="SellFactor"/>.</summary>
        public double SellRate { get; set; } = SellFactor;

        /// <summary>Multiplies every buying price: 0.9 is a 10% discount.</summary>
        public double PriceFactor { get; set; } = 1.0;

        /// <summary>Added to every entry's stock when it is refilled.</summary>
        public int ExtraStock { get; set; }

        public int PriceOf(ShopEntry entry)
        {
            if (entry == null) return 0;
            return Math.Max(entry.Price > 0 ? 1 : 0, (int)Math.Round(entry.Price * PriceFactor));
        }

        public int StockLimit(ShopEntry entry)
        {
            return entry == null || entry.MaxStock <= 0 ? 0 : entry.MaxStock + ExtraStock;
        }

        /// <summary>What this shop pays for one of an item.</summary>
        public int SellPriceOf(ItemDefinition item)
        {
            if (item == null || item.BaseValue <= 0) return 0;
            return Math.Max(1, (int)(item.BaseValue * SellRate));
        }

        private readonly List<ShopEntry> _entries = new List<ShopEntry>();

        public IReadOnlyList<ShopEntry> Entries => _entries;

        public ShopEntry AddEntry(ItemDefinition item, int bundle, int price, int maxStock, double restockChance = 1.0)
        {
            var entry = new ShopEntry(item, bundle, price, maxStock, restockChance);
            _entries.Add(entry);
            return entry;
        }

        /// <summary>Fills every line to its maximum (new game).</summary>
        public void FillStock()
        {
            foreach (ShopEntry entry in _entries) entry.Stock = StockLimit(entry);
        }

        /// <summary>Daily refresh. Always-available lines refill; the others refill only by chance and otherwise keep what is left.</summary>
        public void Restock(Random rng)
        {
            foreach (ShopEntry entry in _entries)
            {
                if (entry.RestockChance >= 1.0 || rng.NextDouble() < entry.RestockChance) entry.Stock = StockLimit(entry);
            }
        }

        /// <summary>What the shop pays for one of an item. 0 means it will not buy it.</summary>
        public static int SellPrice(ItemDefinition item)
        {
            if (item == null || item.BaseValue <= 0) return 0;
            return Math.Max(1, (int)(item.BaseValue * SellFactor));
        }

        /// <summary>Buys one bundle into the backpack, or the stash if the backpack is full.</summary>
        public bool Buy(ShopEntry entry, Wallet wallet, GridInventory backpack, GridInventory stash)
        {
            if (entry == null || !_entries.Contains(entry) || entry.Stock < 1 || wallet == null) return false;
            int price = PriceOf(entry);
            if (wallet.Money < price) return false;

            int toBackpack = entry.Bundle - backpack.Add(entry.Item, entry.Bundle);
            int rest = entry.Bundle - toBackpack;
            int leftover = rest > 0 && stash != null ? stash.Add(entry.Item, rest) : rest;
            if (leftover > 0)
            {
                // Could not take delivery of the whole bundle: undo it.
                if (toBackpack > 0) backpack.TryConsume(entry.Item.Id, toBackpack);
                if (rest - leftover > 0) stash.TryConsume(entry.Item.Id, rest - leftover);
                return false;
            }

            wallet.TrySpend(price);
            entry.Stock--;
            return true;
        }

        /// <summary>Sells items from one stack. Returns the money received, 0 if nothing was sold.</summary>
        public int Sell(GridInventory grid, PlacedItem item, int count, Wallet wallet)
        {
            if (grid == null || item == null || wallet == null || count < 1 || !grid.Contains(item)) return 0;

            int price = SellPriceOf(item.Item);
            if (price <= 0) return 0;

            count = Math.Min(count, item.Count);
            if (!grid.TakeFromStack(item, count)) return 0;

            int total = price * count;
            wallet.Add(total);
            return total;
        }

        /// <summary>Sets one line's remaining stock from a save. Returns false if the shop no longer sells that item.</summary>
        public bool RestoreStock(string itemId, int stock)
        {
            foreach (ShopEntry entry in _entries)
            {
                if (entry.Item.Id != itemId) continue;
                entry.Stock = Math.Max(0, Math.Min(StockLimit(entry), stock));
                return true;
            }
            return false;
        }
    }
}
