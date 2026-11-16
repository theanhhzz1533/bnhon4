using System;
using Bnhon.Core.Combat;
using Bnhon.Core.Crafting;
using Bnhon.Core.Economy;
using Bnhon.Core.Farming;
using Bnhon.Core.Inventory;
using Bnhon.Core.Npcs;
using Bnhon.Core.Quests;
using Bnhon.Core.Raid;
using Bnhon.Core.Relationships;
using Bnhon.Core.Trading;
using Bnhon.Core.Vitals;
using Bnhon.Core.Calendar;

namespace Bnhon.Core
{
    /// <summary>
    /// The whole state of one save and the rules of the day loop:
    /// one raid per day, sleep to advance the calendar.
    /// </summary>
    public sealed class GameSession
    {
        public const float SleepMeal = 30f;

        public GameClock Clock { get; }
        public GridInventory Stash { get; }
        public GridInventory Backpack { get; }
        public GridInventory SecurePouch { get; }
        public Wallet Wallet { get; }
        public Farm Farm { get; }
        public RelationshipTracker Relationships { get; }
        public QuestLog Quests { get; }
        public Random Rng { get; }
        public Stamina Stamina { get; }
        public Loadout Loadout { get; }
        public Workshop Workshop { get; }
        public Shop Shop { get; }
        public Needs Needs { get; }
        public StoryFlags Story { get; }

        /// <summary>Morning until the day's raid has been used, evening after.</summary>
        public DayPhase Phase => RaidedToday ? DayPhase.Evening : DayPhase.Morning;

        public WateringCan WateringCan { get; }
        public BuffSet Buffs { get; }

        /// <summary>The kitchen's recipes. It is a workshop that stays at level 1 and needs no blueprints.</summary>
        public Workshop Kitchen { get; }

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
            Stamina = new Stamina();
            Loadout = new Loadout();
            Workshop = new Workshop();
            Shop = new Shop();
            Needs = new Needs();
            Story = new StoryFlags();
            WateringCan = new WateringCan();
            Buffs = new BuffSet();
            Kitchen = new Workshop();
        }

        /// <summary>Sets whether today's raid has been used, from a save. Any raid object is discarded.</summary>
        public void RestoreRaidedToday(bool raidedToday)
        {
            CurrentRaid = null;
            RaidedToday = raidedToday;
        }

        /// <summary>Starts today's raid. Returns null if one was already started today.</summary>
        public RaidSession TryStartRaid()
        {
            if (RaidedToday || IsInRaid) return null;
            RaidedToday = true;
            CurrentRaid = new RaidSession(Backpack, SecurePouch, Loadout);
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
            Shop.Restock(Rng);
            Buffs.Clear();

            // A night at the base: water is on hand, and there is a modest meal.
            Needs.Drink(Needs.Max);
            Needs.Eat(SleepMeal);
            Stamina.Refill();
            return true;
        }

        /// <summary>
        /// Gives one unit of a stack to a resident. The item is only taken if the gift is
        /// accepted (one a day, two a week).
        /// </summary>
        public GiftResult GiveGift(string npcId, GridInventory grid, PlacedItem placed, out GiftTaste taste)
        {
            taste = GiftTaste.Neutral;
            if (grid == null || placed == null || !grid.Contains(placed)) return GiftResult.UnknownNpc;

            string itemId = placed.Item.Id;
            GiftResult result = Relationships.Gift(npcId, itemId, out taste);
            if (result == GiftResult.Accepted) grid.TakeFromStack(placed, 1);
            return result;
        }

        /// <summary>Ends a heart scene on the chosen answer: it will not play again, and the answer's points are added.</summary>
        public bool CompleteEvent(string npcId, HeartEvent heartEvent, SceneChoice choice)
        {
            if (heartEvent == null || Story.Has(heartEvent.Id)) return false;
            Story.Set(heartEvent.Id);
            if (choice != null) Relationships.AddPoints(npcId, choice.Points);
            return true;
        }

        /// <summary>
        /// Uses one unit of a consumable from a stack: removes it and applies hunger and thirst.
        /// Healing is returned through <paramref name="effect"/>'s Heal for the caller to apply,
        /// because health lives with the character in the scene.
        /// </summary>
        public bool Consume(GridInventory grid, PlacedItem placed, ConsumableDefinition effect)
        {
            if (grid == null || placed == null || effect == null) return false;
            if (effect.ItemId != placed.Item.Id) return false;
            if (!grid.TakeFromStack(placed, 1)) return false;

            Needs.Eat(effect.Hunger);
            Needs.Drink(effect.Thirst);
            Buffs.Add(effect.Buff);
            return true;
        }
    }
}
