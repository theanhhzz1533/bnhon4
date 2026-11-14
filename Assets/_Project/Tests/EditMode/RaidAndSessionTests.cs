using System;
using Bnhon.Core.Content;
using Bnhon.Core.Items;
using Bnhon.Core.Loot;
using Bnhon.Core.Raid;
using Bnhon.Core.Relationships;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class RaidAndSessionTests
    {
        [Test]
        public void LootTable_RollsOnlyItsEntriesWithinCountRange()
        {
            var a = new ItemDefinition("a", "A", ItemCategory.Material);
            var b = new ItemDefinition("b", "B", ItemCategory.Material);
            var table = new LootTable("t").Add(a, 1, 2, 4).Add(b, 3);

            var drops = table.Roll(new Random(42), 200);

            Assert.That(drops.Count, Is.EqualTo(200));
            int countA = 0;
            foreach (var drop in drops)
            {
                if (drop.Item == a)
                {
                    countA++;
                    Assert.That(drop.Count >= 2 && drop.Count <= 4, Is.True);
                }
                else
                {
                    Assert.That(drop.Item == b, Is.True);
                    Assert.That(drop.Count, Is.EqualTo(1));
                }
            }
            // Weight 1 of 4: expect about 50 of 200.
            Assert.That(countA > 25 && countA < 80, Is.True);
        }

        [Test]
        public void Session_AllowsOneRaidPerDay()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);

            var raid = session.TryStartRaid();
            Assert.That(raid != null, Is.True);
            Assert.That(session.Sleep(), Is.False);

            raid.Extract();
            Assert.That(session.TryStartRaid() == null, Is.True);

            Assert.That(session.Sleep(), Is.True);
            Assert.That(session.Clock.TotalDays, Is.EqualTo(1));
            Assert.That(session.TryStartRaid() != null, Is.True);
        }

        [Test]
        public void Raid_DeathLosesBackpackButKeepsSecurePouchAndStash()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Female, 1);
            session.Backpack.Clear();
            int stashSeeds = session.Stash.CountOf(PrototypeContent.SweetPotatoSeed.Id);

            var raid = session.TryStartRaid();
            session.Backpack.Add(PrototypeContent.Wood, 2);
            session.SecurePouch.Add(PrototypeContent.TeaTin, 1);

            var lost = raid.Die();

            Assert.That(raid.State, Is.EqualTo(RaidState.Died));
            Assert.That(lost.Count, Is.EqualTo(1));
            Assert.That(lost[0].Count, Is.EqualTo(2));
            Assert.That(session.Backpack.Items.Count, Is.EqualTo(0));
            Assert.That(session.SecurePouch.CountOf(PrototypeContent.TeaTin.Id), Is.EqualTo(1));
            Assert.That(session.Stash.CountOf(PrototypeContent.SweetPotatoSeed.Id), Is.EqualTo(stashSeeds));
        }

        [Test]
        public void Raid_ExtractKeepsBackpack()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            var raid = session.TryStartRaid();
            session.Backpack.Add(PrototypeContent.Wood, 2);

            Assert.That(raid.Extract(), Is.True);
            Assert.That(raid.Extract(), Is.False);
            Assert.That(session.Backpack.CountOf(PrototypeContent.Wood.Id), Is.EqualTo(2));
        }

        [Test]
        public void Session_FullDayLoop_PlantWaterSleepHarvestDeliver()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 7);
            var plot = session.Farm.Plots[0];
            var crop = PrototypeContent.SweetPotatoCrop;

            Assert.That(session.Stash.TryConsume(crop.Seed.Id, 1), Is.True);
            plot.Till();
            Assert.That(plot.Plant(crop, session.Clock.Season), Is.True);

            for (int day = 0; day < crop.DaysToMature; day++)
            {
                plot.Water();
                session.Sleep();
            }

            int harvested = plot.Harvest(session.Rng);
            Assert.That(harvested >= 2, Is.True);
            session.Backpack.Add(crop.Produce, harvested);

            session.Quests.Accept("q_first_harvest");
            Assert.That(session.Quests.TurnIn("q_first_harvest", session.Backpack, session.Wallet, session.Relationships), Is.True);
            Assert.That(session.Wallet.Money, Is.EqualTo(90));
            Assert.That(session.Relationships.Get(PrototypeContent.NpcBaTu).Points, Is.EqualTo(150));
        }
    }
}
