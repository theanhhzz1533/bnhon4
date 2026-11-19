using System;

namespace Bnhon.Core.Raid
{
    public enum ThreatUpgrade
    {
        None,
        Runner,
        Armored
    }

    /// <summary>
    /// How the world gets more dangerous as days pass, so raids do not get easier just
    /// because the player is better armed. The level rises by one every two days, up to a
    /// cap; at each level more of a map's ordinary walkers turn up as runners or armoured.
    /// </summary>
    public static class Threat
    {
        public const int MaxLevel = 10;
        public const int DaysPerLevel = 2;

        public static int Level(int totalDays)
        {
            if (totalDays <= 0) return 0;
            return Math.Min(MaxLevel, totalDays / DaysPerLevel);
        }

        /// <summary>Chance that a walker placed by the map is a runner instead.</summary>
        public static double RunnerChance(int level)
        {
            return Math.Min(0.3, Math.Max(0, level) * 0.04);
        }

        /// <summary>Chance that a walker placed by the map is armoured instead. None before level 2.</summary>
        public static double ArmoredChance(int level)
        {
            return level < 2 ? 0.0 : Math.Min(0.2, (level - 1) * 0.03);
        }

        /// <summary>Decides what one of the map's walkers becomes at this level.</summary>
        public static ThreatUpgrade Roll(int level, Random rng)
        {
            double roll = rng.NextDouble();
            double armored = ArmoredChance(level);
            if (roll < armored) return ThreatUpgrade.Armored;
            return roll < armored + RunnerChance(level) ? ThreatUpgrade.Runner : ThreatUpgrade.None;
        }

        /// <summary>Extra zombies in each reinforcement wave.</summary>
        public static int ExtraWaveSize(int level)
        {
            return Math.Max(0, level) / 3;
        }
    }

    /// <summary>A place the player can raid.</summary>
    public sealed class RaidArea
    {
        public string Id { get; }
        public string DisplayName { get; }

        /// <summary>The map file in Resources/Maps, without extension.</summary>
        public string MapName { get; }
        public string Description { get; }

        /// <summary>The story flag that opens this area. Null if it is open from the start.</summary>
        public string UnlockFlag { get; }

        /// <summary>The story flag set when this area's boss is killed. Null if it has no boss.</summary>
        public string BossFlag { get; }

        /// <summary>Names of the map's drop-off points, from the top of the map down.</summary>
        public System.Collections.Generic.IReadOnlyList<string> InsertionNames { get; set; }

        public string InsertionName(int index)
        {
            return InsertionNames != null && index >= 0 && index < InsertionNames.Count ? InsertionNames[index] : "Điểm " + (index + 1);
        }

        /// <summary>What the boss is called in messages.</summary>
        public string BossName { get; set; } = "Trùm";

        /// <summary>The boss's health. Zero keeps the default for bosses.</summary>
        public float BossHealth { get; set; }

        /// <summary>What the boss always carries. Null for none.</summary>
        public System.Collections.Generic.IReadOnlyList<Crafting.ItemAmount> BossLoot { get; set; }

        public RaidArea(string id, string displayName, string mapName, string description, string unlockFlag, string bossFlag)
        {
            Id = id;
            DisplayName = displayName;
            MapName = mapName;
            Description = description;
            UnlockFlag = unlockFlag;
            BossFlag = bossFlag;
        }

        public bool IsUnlocked(GameSession session)
        {
            return UnlockFlag == null || session.Story.Has(UnlockFlag);
        }

        public bool BossDefeated(GameSession session)
        {
            return BossFlag != null && session.Story.Has(BossFlag);
        }
    }
}
