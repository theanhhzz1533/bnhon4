using System;
using System.Collections.Generic;
using Bnhon.Core.Crafting;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;

namespace Bnhon.Core.Charms
{
    public enum CharmRarity
    {
        Common,
        Rare,
        Epic
    }

    /// <summary>The three kinds of charm, for brewing toward one kind.</summary>
    public enum CharmGroup
    {
        Stealth,
        Survival,
        Combat
    }

    /// <summary>What a charm changes. The amount's meaning is given per stat.</summary>
    public enum CharmStat
    {
        /// <summary>Fraction off how far zombies see the player (0.2 = they see 20% less far).</summary>
        ZombieSight,

        /// <summary>Fraction off the noise of sprinting and rolling.</summary>
        Noise,

        /// <summary>Fraction off the damage the player takes.</summary>
        DamageTaken,

        /// <summary>Fraction off how fast hunger and thirst drain.</summary>
        NeedsDrain,

        /// <summary>Fraction faster searching of containers and bodies.</summary>
        SearchSpeed,

        /// <summary>Fraction faster stamina recovery.</summary>
        StaminaRegen,

        /// <summary>Fraction faster movement while below a quarter of health.</summary>
        LowHealthSpeed,

        /// <summary>Distance at which unsearched containers glow through walls.</summary>
        LootSense,

        /// <summary>Fraction more wear on armoured zombies' shields.</summary>
        ShieldBreak,

        /// <summary>Fraction off the pause between dodge rolls.</summary>
        RollRecovery,

        /// <summary>Health restored per second after a few seconds without being hit.</summary>
        Regeneration,

        /// <summary>Extra maximum health.</summary>
        MaxHealth,

        /// <summary>Seconds a knife hit stuns a zombie.</summary>
        MeleeStun,

        /// <summary>Fraction off weapon recoil.</summary>
        Recoil
    }

    public sealed class CharmDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public CharmRarity Rarity { get; }
        public CharmGroup Group { get; }
        public CharmStat Stat { get; }
        public float Amount { get; }

        public CharmDefinition(string id, string displayName, CharmRarity rarity, CharmGroup group, CharmStat stat, float amount, string description)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A charm needs an id.", nameof(id));
            Id = id;
            DisplayName = displayName;
            Rarity = rarity;
            Group = group;
            Stat = stat;
            Amount = amount;
            Description = description;
        }
    }

    /// <summary>
    /// The charms the player owns and wears. Charms are not items: they never go in the
    /// backpack, so they are never lost on death. Owning more than one of the same charm
    /// only matters for breaking the spares down.
    /// </summary>
    public sealed class CharmCollection
    {
        public const int BaseSlots = 2;

        private readonly Dictionary<string, int> _owned = new Dictionary<string, int>();
        private readonly List<CharmDefinition> _known = new List<CharmDefinition>();
        private readonly CharmDefinition[] _worn = new CharmDefinition[3];

        /// <summary>How many slots can be used, 2 or 3.</summary>
        public int Slots { get; set; } = BaseSlots;

        /// <summary>Every charm owned at least once, in the order first found.</summary>
        public IReadOnlyList<CharmDefinition> Owned => _known;

        public int CountOf(string charmId) => _owned.TryGetValue(charmId, out int count) ? count : 0;

        public void Add(CharmDefinition charm, int count = 1)
        {
            if (charm == null || count < 1) return;
            if (!_owned.ContainsKey(charm.Id))
            {
                _owned[charm.Id] = 0;
                _known.Add(charm);
            }
            _owned[charm.Id] += count;
        }

        public CharmDefinition WornIn(int slot)
        {
            return slot >= 0 && slot < Slots ? _worn[slot] : null;
        }

        public bool IsWorn(CharmDefinition charm)
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_worn[i] == charm) return true;
            }
            return false;
        }

        /// <summary>Wears a charm in a slot. A charm can be worn only once at a time.</summary>
        public bool Wear(CharmDefinition charm, int slot)
        {
            if (charm == null || slot < 0 || slot >= Slots || CountOf(charm.Id) < 1 || IsWorn(charm)) return false;
            _worn[slot] = charm;
            return true;
        }

        public void TakeOff(int slot)
        {
            if (slot >= 0 && slot < _worn.Length) _worn[slot] = null;
        }

        /// <summary>The first free slot, or -1 when all are taken.</summary>
        public int FreeSlot()
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_worn[i] == null) return i;
            }
            return -1;
        }

        /// <summary>Removes one copy. A charm that is worn keeps at least one copy.</summary>
        public bool RemoveSpare(CharmDefinition charm)
        {
            if (charm == null || CountOf(charm.Id) < 2) return false;
            _owned[charm.Id]--;
            return true;
        }

        /// <summary>The sum of what the worn charms give for a stat.</summary>
        public float Total(CharmStat stat)
        {
            float total = 0f;
            for (int i = 0; i < Slots; i++)
            {
                if (_worn[i] != null && _worn[i].Stat == stat) total += _worn[i].Amount;
            }
            return total;
        }

        public void Clear()
        {
            _owned.Clear();
            _known.Clear();
            for (int i = 0; i < _worn.Length; i++) _worn[i] = null;
        }
    }

    public enum BrewBlock
    {
        None,
        Samples,
        Core,
        Locked
    }

    /// <summary>
    /// Brewing charms from zombie samples, like Duckov's feathers-for-a-charm: a fixed price,
    /// a random charm. Rarer charms are rarer; a boss's core always gives a good one.
    /// </summary>
    public sealed class CharmLab
    {
        public const int SamplesPerBrew = 8;
        public const int SamplesPerGroupBrew = 12;

        private readonly List<CharmDefinition> _charms = new List<CharmDefinition>();

        public ItemDefinition Sample { get; }
        public IReadOnlyList<CharmDefinition> Charms => _charms;

        public CharmLab(ItemDefinition sample)
        {
            Sample = sample ?? throw new ArgumentNullException(nameof(sample));
        }

        public CharmLab Add(CharmDefinition charm)
        {
            _charms.Add(charm);
            return this;
        }

        public CharmDefinition Find(string charmId)
        {
            foreach (CharmDefinition charm in _charms)
            {
                if (charm.Id == charmId) return charm;
            }
            return null;
        }

        /// <summary>70% common, 25% rare, 5% epic.</summary>
        public static CharmRarity RollRarity(Random rng)
        {
            double roll = rng.NextDouble();
            if (roll < 0.05) return CharmRarity.Epic;
            return roll < 0.30 ? CharmRarity.Rare : CharmRarity.Common;
        }

        /// <summary>From a boss core: 75% rare, 25% epic.</summary>
        public static CharmRarity RollCoreRarity(Random rng)
        {
            return rng.NextDouble() < 0.25 ? CharmRarity.Epic : CharmRarity.Rare;
        }

        /// <summary>A random charm of a rarity, optionally of one group. Falls back to any rarity if the group has none.</summary>
        public CharmDefinition Pick(CharmRarity rarity, CharmGroup? group, Random rng)
        {
            var pool = new List<CharmDefinition>();
            foreach (CharmDefinition charm in _charms)
            {
                if (charm.Rarity == rarity && (group == null || charm.Group == group.Value)) pool.Add(charm);
            }
            if (pool.Count == 0 && group != null)
            {
                foreach (CharmDefinition charm in _charms)
                {
                    if (charm.Group == group.Value) pool.Add(charm);
                }
            }
            if (pool.Count == 0) pool.AddRange(_charms);
            return pool[rng.Next(pool.Count)];
        }

        /// <summary>Brews one charm for samples (or free), and adds it to the collection. Returns it, or null.</summary>
        public CharmDefinition Brew(CharmCollection collection, GridInventory stash, GridInventory backpack, Random rng, bool free, CharmGroup? group = null)
        {
            int cost = group == null ? SamplesPerBrew : SamplesPerGroupBrew;
            if (!free && !Workshop.TrySpend(new[] { new ItemAmount(Sample, cost) }, stash, backpack)) return null;

            CharmDefinition charm = Pick(RollRarity(rng), group, rng);
            collection.Add(charm);
            return charm;
        }

        /// <summary>Brews from a boss core: always rare or better.</summary>
        public CharmDefinition BrewFromCore(CharmCollection collection, ItemDefinition core, GridInventory stash, GridInventory backpack, Random rng)
        {
            if (core == null || !Workshop.TrySpend(new[] { new ItemAmount(core, 1) }, stash, backpack)) return null;

            CharmDefinition charm = Pick(RollCoreRarity(rng), null, rng);
            collection.Add(charm);
            return charm;
        }

        /// <summary>Samples a spare charm breaks down into.</summary>
        public static int BreakdownValue(CharmDefinition charm)
        {
            if (charm == null) return 0;
            return charm.Rarity == CharmRarity.Epic ? 8 : charm.Rarity == CharmRarity.Rare ? 5 : 3;
        }

        /// <summary>Breaks one spare copy into samples, put in the stash. Returns how many, or 0.</summary>
        public int BreakDown(CharmCollection collection, CharmDefinition charm, GridInventory stash, GridInventory backpack)
        {
            if (!collection.RemoveSpare(charm)) return 0;
            int samples = BreakdownValue(charm);
            int leftover = stash.Add(Sample, samples);
            if (leftover > 0) backpack.Add(Sample, leftover);
            return samples;
        }
    }
}
