using System;
using System.Collections.Generic;
using Bnhon.Core.Crafting;

namespace Bnhon.Core.Npcs
{
    /// <summary>
    /// Someone waiting to be rescued on a raid map. They stand where the map has a rescue
    /// marker, may need an item before they can move, then follow the player; reaching an
    /// exit with them alive brings them to the base for good.
    /// </summary>
    public sealed class RescueDefinition
    {
        public string NpcId { get; }

        /// <summary>The raid area whose map holds them.</summary>
        public string AreaId { get; }

        /// <summary>Which of the map's rescue markers, counting from the top of the map, is theirs.</summary>
        public int MarkerIndex { get; }

        /// <summary>When they can be found. Before that the marker stays empty.</summary>
        public Func<GameSession, bool> IsAvailable { get; }

        /// <summary>What they need handed over before they can follow. Null for nothing.</summary>
        public ItemAmount? Needs { get; set; }

        /// <summary>What they say when found, before the player helps.</summary>
        public string Plea { get; set; } = "Cứu tôi với...";

        /// <summary>What they say when they start following.</summary>
        public string Thanks { get; set; } = "Đi thôi. Tôi theo sau.";

        /// <summary>The scene played back at the base once they are rescued.</summary>
        public IReadOnlyList<SceneLine> Arrival { get; set; }

        /// <summary>
        /// Above zero, they will not move while any zombie is within this distance of them:
        /// the player has to clear the area first.
        /// </summary>
        public float ClearRadius { get; set; }

        public float Health { get; set; } = 100f;
        public float Speed { get; set; } = 3.4f;

        public RescueDefinition(string npcId, string areaId, int markerIndex, Func<GameSession, bool> isAvailable)
        {
            if (string.IsNullOrEmpty(npcId)) throw new ArgumentException("A rescue needs an NPC.", nameof(npcId));
            NpcId = npcId;
            AreaId = areaId;
            MarkerIndex = markerIndex;
            IsAvailable = isAvailable ?? (session => true);
        }
    }

    /// <summary>Who lives at the base. Story flags hold it, so it is saved with everything else.</summary>
    public static class Residents
    {
        public static string Flag(string npcId) => "resident_" + npcId;

        public static bool LivesAtBase(GameSession session, string npcId)
        {
            return session.Story.Has(Flag(npcId));
        }

        public static void MoveIn(GameSession session, string npcId)
        {
            session.Story.Set(Flag(npcId));
        }
    }

    /// <summary>One task of a resident: things to bring, maybe a bond to reach first, and what it opens.</summary>
    public sealed class ChainStep
    {
        private readonly List<ItemAmount> _items = new List<ItemAmount>();

        public string Title { get; }
        public string Text { get; }

        /// <summary>What has to be handed over, from the stash first and then the backpack.</summary>
        public IReadOnlyList<ItemAmount> Items => _items;

        /// <summary>Hearts needed with the resident before the step can be done.</summary>
        public int MinHearts { get; private set; }

        /// <summary>The story flag the step sets. Services read these to know what is open.</summary>
        public string UnlockFlag { get; }

        /// <summary>Says what the player gets, shown before and after.</summary>
        public string RewardText { get; private set; }

        public int RewardMoney { get; private set; }

        public ChainStep(string title, string text, string unlockFlag)
        {
            if (string.IsNullOrEmpty(unlockFlag)) throw new ArgumentException("A step needs a flag.", nameof(unlockFlag));
            Title = title;
            Text = text;
            UnlockFlag = unlockFlag;
        }

        public ChainStep Bring(params ItemAmount[] items)
        {
            _items.AddRange(items);
            return this;
        }

        public ChainStep NeedsHearts(int hearts)
        {
            MinHearts = hearts;
            return this;
        }

        public ChainStep Rewards(string text, int money = 0)
        {
            RewardText = text;
            RewardMoney = money;
            return this;
        }
    }

    public enum ChainBlock
    {
        None,
        NotResident,
        Finished,
        Hearts,
        Items
    }

    /// <summary>
    /// A resident's tasks, done in order. Each opens part of their service or gives a reward.
    /// Progress is the set of step flags, so it needs no saving of its own.
    /// </summary>
    public sealed class QuestChain
    {
        private readonly List<ChainStep> _steps = new List<ChainStep>();

        public string NpcId { get; }
        public IReadOnlyList<ChainStep> Steps => _steps;

        public QuestChain(string npcId)
        {
            NpcId = npcId;
        }

        public QuestChain Add(ChainStep step)
        {
            _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            return this;
        }

        public int Completed(GameSession session)
        {
            int done = 0;
            foreach (ChainStep step in _steps)
            {
                if (!session.Story.Has(step.UnlockFlag)) break;
                done++;
            }
            return done;
        }

        /// <summary>The next task, or null when all are done.</summary>
        public ChainStep Current(GameSession session)
        {
            int done = Completed(session);
            return done < _steps.Count ? _steps[done] : null;
        }

        public ChainBlock Check(GameSession session)
        {
            if (!Residents.LivesAtBase(session, NpcId)) return ChainBlock.NotResident;
            ChainStep step = Current(session);
            if (step == null) return ChainBlock.Finished;

            Relationships.Relationship bond = session.Relationships.Get(NpcId);
            if (step.MinHearts > 0 && (bond == null || bond.Hearts < step.MinHearts)) return ChainBlock.Hearts;
            if (!Workshop.CanAfford(step.Items, session.Stash, session.Backpack)) return ChainBlock.Items;
            return ChainBlock.None;
        }

        /// <summary>Hands over the items and completes the current step. Returns the step, or null if it could not be done.</summary>
        public ChainStep Complete(GameSession session)
        {
            if (Check(session) != ChainBlock.None) return null;
            ChainStep step = Current(session);
            if (!Workshop.TrySpend(step.Items, session.Stash, session.Backpack)) return null;

            session.Story.Set(step.UnlockFlag);
            if (step.RewardMoney > 0) session.Wallet.Add(step.RewardMoney);
            return step;
        }
    }
}
