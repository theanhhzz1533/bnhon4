using System;
using System.Collections.Generic;
using Bnhon.Core.Items;

namespace Bnhon.Core.Loot
{
    public struct LootDrop
    {
        public ItemDefinition Item;
        public int Count;

        public LootDrop(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
        }
    }

    /// <summary>Weighted loot table. Each roll picks one entry by weight.</summary>
    public sealed class LootTable
    {
        private sealed class Entry
        {
            public ItemDefinition Item;
            public int Weight;
            public int MinCount;
            public int MaxCount;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private int _totalWeight;

        public string Id { get; }
        public int EntryCount => _entries.Count;

        public LootTable(string id)
        {
            Id = id;
        }

        public LootTable Add(ItemDefinition item, int weight, int minCount = 1, int maxCount = 1)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight));
            if (minCount < 1 || maxCount < minCount) throw new ArgumentOutOfRangeException(nameof(minCount));

            _entries.Add(new Entry { Item = item, Weight = weight, MinCount = minCount, MaxCount = maxCount });
            _totalWeight += weight;
            return this;
        }

        public List<LootDrop> Roll(Random rng, int rolls)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            var drops = new List<LootDrop>();
            if (_entries.Count == 0) return drops;

            for (int i = 0; i < rolls; i++)
            {
                int pick = rng.Next(_totalWeight);
                foreach (var entry in _entries)
                {
                    if (pick < entry.Weight)
                    {
                        drops.Add(new LootDrop(entry.Item, rng.Next(entry.MinCount, entry.MaxCount + 1)));
                        break;
                    }
                    pick -= entry.Weight;
                }
            }
            return drops;
        }
    }
}
