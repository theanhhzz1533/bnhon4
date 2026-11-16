using System;
using System.Collections.Generic;
using Bnhon.Core.Crafting;

namespace Bnhon.Core.Story
{
    /// <summary>Names of the story flags the rules themselves look at.</summary>
    public static class StoryIds
    {
        /// <summary>The opening in the hospital is over and the player lives at the base.</summary>
        public const string IntroDone = "intro_done";

        /// <summary>The player has picked up the dagger during the opening.</summary>
        public const string HasKnife = "has_knife";

        /// <summary>The player has reached the hospital canteen, where the horde appears.</summary>
        public const string ReachedCanteen = "intro_canteen";

        /// <summary>The player has come back alive from an ordinary raid at least once.</summary>
        public const string Extracted = "extracted_once";
    }

    /// <summary>One objective of a storyline: what to tell the player, how to know it is done, and what it gives.</summary>
    public sealed class StoryStep
    {
        private readonly List<ItemAmount> _rewardItems = new List<ItemAmount>();

        public string Id { get; }
        public string Text { get; }
        public Func<GameSession, bool> IsDone { get; }
        public int RewardMoney { get; private set; }
        public IReadOnlyList<ItemAmount> RewardItems => _rewardItems;

        /// <summary>Shown when the step is completed. May be null.</summary>
        public string DoneMessage { get; private set; }

        public StoryStep(string id, string text, Func<GameSession, bool> isDone)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Step id is required.", nameof(id));
            Id = id;
            Text = text;
            IsDone = isDone ?? throw new ArgumentNullException(nameof(isDone));
        }

        public string Flag => "step_" + Id;

        public StoryStep Gives(int money, params ItemAmount[] items)
        {
            RewardMoney = money;
            _rewardItems.AddRange(items);
            return this;
        }

        public StoryStep Says(string doneMessage)
        {
            DoneMessage = doneMessage;
            return this;
        }
    }

    /// <summary>
    /// An ordered list of objectives. Only the first unfinished one is active; a finished
    /// step stays finished even if its condition later stops being true.
    /// </summary>
    public sealed class Storyline
    {
        private readonly List<StoryStep> _steps = new List<StoryStep>();

        public IReadOnlyList<StoryStep> Steps => _steps;

        public Storyline Add(StoryStep step)
        {
            _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            return this;
        }

        /// <summary>The objective the player is on, or null when the storyline is finished.</summary>
        public StoryStep Current(GameSession session)
        {
            foreach (StoryStep step in _steps)
            {
                if (!session.Story.Has(step.Flag)) return step;
            }
            return null;
        }

        /// <summary>
        /// Completes the current objective if its condition holds: marks it, pays its reward
        /// (items go to the stash, or the backpack if the stash is full) and returns it.
        /// Returns null if nothing was completed. Completes at most one step per call.
        /// </summary>
        public StoryStep Advance(GameSession session)
        {
            StoryStep step = Current(session);
            if (step == null || !step.IsDone(session)) return null;

            session.Story.Set(step.Flag);
            if (step.RewardMoney > 0) session.Wallet.Add(step.RewardMoney);
            foreach (ItemAmount reward in step.RewardItems)
            {
                int leftover = session.Stash.Add(reward.Item, reward.Count);
                if (leftover > 0) session.Backpack.Add(reward.Item, leftover);
            }
            return step;
        }

        /// <summary>Marks every step finished without rewards. For saves made before the storyline existed.</summary>
        public void SkipAll(GameSession session)
        {
            foreach (StoryStep step in _steps) session.Story.Set(step.Flag);
        }
    }
}
