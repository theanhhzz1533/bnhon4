using Bnhon.Core.Calendar;
using Bnhon.Core.Economy;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;
using Bnhon.Core.Quests;
using Bnhon.Core.Relationships;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class RelationshipAndQuestTests
    {
        private static RelationshipTracker Tracker(Gender playerGender = Gender.Male)
        {
            var tracker = new RelationshipTracker(playerGender);
            tracker.Register(new NpcProfile("lan", "Lan", Gender.Female, true).Loves("tea").Dislikes("scrap"));
            tracker.Register(new NpcProfile("minh", "Minh", Gender.Male, true));
            tracker.Register(new NpcProfile("ba_tu", "Ba Tu", Gender.Female, false));
            return tracker;
        }

        [Test]
        public void Talk_CountsOncePerDay()
        {
            var tracker = Tracker();
            var clock = new GameClock();

            Assert.That(tracker.Talk("lan"), Is.True);
            Assert.That(tracker.Talk("lan"), Is.False);
            Assert.That(tracker.Get("lan").Points, Is.EqualTo(RelationshipTracker.TalkPoints));

            clock.AdvanceDay();
            tracker.OnNewDay(clock);
            Assert.That(tracker.Talk("lan"), Is.True);
        }

        [Test]
        public void Gift_UsesTasteAndDailyAndWeeklyLimits()
        {
            var tracker = Tracker();
            var clock = new GameClock();

            Assert.That(tracker.Gift("lan", "tea", out var taste), Is.EqualTo(GiftResult.Accepted));
            Assert.That(taste, Is.EqualTo(GiftTaste.Loved));
            Assert.That(tracker.Get("lan").Points, Is.EqualTo(80));
            Assert.That(tracker.Gift("lan", "tea", out taste), Is.EqualTo(GiftResult.AlreadyGiftedToday));

            clock.AdvanceDay();
            tracker.OnNewDay(clock);
            Assert.That(tracker.Gift("lan", "scrap", out taste), Is.EqualTo(GiftResult.Accepted));
            Assert.That(tracker.Get("lan").Points, Is.EqualTo(40));

            clock.AdvanceDay();
            tracker.OnNewDay(clock);
            Assert.That(tracker.Gift("lan", "tea", out taste), Is.EqualTo(GiftResult.WeeklyLimitReached));

            while (clock.DayOfWeek != 0)
            {
                clock.AdvanceDay();
                tracker.OnNewDay(clock);
            }
            Assert.That(tracker.Gift("lan", "tea", out taste), Is.EqualTo(GiftResult.Accepted));
        }

        [Test]
        public void Points_NeverGoBelowZero()
        {
            var tracker = Tracker();

            tracker.AddPoints("lan", -500);

            Assert.That(tracker.Get("lan").Points, Is.EqualTo(0));
        }

        [Test]
        public void AskOut_NeedsEightHeartsRomanceableAndOppositeGender()
        {
            var tracker = Tracker(Gender.Male);

            tracker.AddPoints("lan", 7 * RelationshipTracker.PointsPerHeart);
            Assert.That(tracker.CanAskOut("lan"), Is.False);

            tracker.AddPoints("lan", RelationshipTracker.PointsPerHeart);
            tracker.AddPoints("minh", 8 * RelationshipTracker.PointsPerHeart);
            tracker.AddPoints("ba_tu", 8 * RelationshipTracker.PointsPerHeart);

            Assert.That(tracker.CanAskOut("lan"), Is.True);
            Assert.That(tracker.CanAskOut("minh"), Is.False);
            Assert.That(tracker.CanAskOut("ba_tu"), Is.False);

            Assert.That(tracker.AskOut("lan"), Is.True);
            Assert.That(tracker.Get("lan").Status, Is.EqualTo(RelationshipStatus.Dating));
        }

        [Test]
        public void Hearts_AreCappedAtEightUntilDatingThenTen()
        {
            var tracker = Tracker();

            tracker.AddPoints("lan", 100000);
            Assert.That(tracker.Get("lan").Hearts, Is.EqualTo(8));
            Assert.That(tracker.CanMarry("lan"), Is.False);

            tracker.AskOut("lan");
            tracker.AddPoints("lan", 100000);
            Assert.That(tracker.Get("lan").Hearts, Is.EqualTo(10));
            Assert.That(tracker.Marry("lan"), Is.True);
        }

        [Test]
        public void Quest_TurnInTakesItemsAndPaysRewards()
        {
            var tracker = Tracker();
            var wallet = new Wallet();
            var inventory = new GridInventory(4, 4);
            var wood = new ItemDefinition("wood", "Wood", ItemCategory.Material, maxStack: 5);
            var log = new QuestLog();
            log.Add(new DeliveryQuest("q", "Fence", "minh", "wood", 3, 60, 100));

            inventory.Add(wood, 4);
            Assert.That(log.CanTurnIn("q", inventory), Is.False);

            Assert.That(log.Accept("q"), Is.True);
            Assert.That(log.TurnIn("q", inventory, wallet, tracker), Is.True);

            Assert.That(inventory.CountOf("wood"), Is.EqualTo(1));
            Assert.That(wallet.Money, Is.EqualTo(60));
            Assert.That(tracker.Get("minh").Points, Is.EqualTo(100));
            Assert.That(log.Get("q").State, Is.EqualTo(QuestState.Completed));
            Assert.That(log.TurnIn("q", inventory, wallet, tracker), Is.False);
        }
    }
}
