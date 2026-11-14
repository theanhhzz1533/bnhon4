using System;
using Bnhon.Core.Economy;
using Bnhon.Core.Farming;
using Bnhon.Core.Inventory;
using Bnhon.Core.Quests;
using Bnhon.Core.Raid;
using Bnhon.Core.Relationships;
using Bnhon.Core.Calendar;

namespace Bnhon.Core
{
    /// <summary>
    /// The whole state of one save and the rules of the day loop:
    /// one raid per day, sleep to advance the calendar.
    /// </summary>
    public sealed class GameSession
    {
        public GameClock Clock { get; }
        public GridInventory Stash { get; }
        public GridInventory Backpack { get; }
        public GridInventory SecurePouch { get; }
        public Wallet Wallet { get; }
        public Farm Farm { get; }
        public RelationshipTracker Relationships { get; }
        public QuestLog Quests { get; }
        public Random Rng { get; }

        public RaidSession CurrentRaid { get; private set; }
        public bool RaidedToday { get; private set; }
        public bool IsInRaid => CurrentRaid != null && CurrentRaid.State == RaidState.InProgress;

        public GameSession(Gender playerGender, int seed, int farmPlots = 9)
        {
            Clock = new GameClock();
            Stash = new GridInventory(10, 8);
            Backpack = new GridInventory(6, 5);
            SecurePouch = new GridInventory(2, 2);
            Wallet = new Wallet();
            Farm = new Farm(farmPlots);
            Relationships = new RelationshipTracker(playerGender);
            Quests = new QuestLog();
            Rng = new Random(seed);
        }

        /// <summary>Starts today's raid. Returns null if one was already started today.</summary>
        public RaidSession TryStartRaid()
        {
            if (RaidedToday || IsInRaid) return null;
            RaidedToday = true;
            CurrentRaid = new RaidSession(Backpack, SecurePouch);
            return CurrentRaid;
        }

        /// <summary>Ends the day. Returns false while a raid is still in progress.</summary>
        public bool Sleep()
        {
            if (IsInRaid) return false;

            CurrentRaid = null;
            RaidedToday = false;
            Clock.AdvanceDay();
            Farm.OnNewDay(Clock.Season);
            Relationships.OnNewDay(Clock);
            return true;
        }
    }
}
