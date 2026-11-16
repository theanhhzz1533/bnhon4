using System.Collections.Generic;
using Bnhon.Core.Content;
using Bnhon.Core.Inventory;
using Bnhon.Core.Npcs;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class NpcLifeTests
    {
        private static GameSession NewSession()
        {
            return PrototypeContent.CreateNewSession(Gender.Male, 1);
        }

        [Test]
        public void Schedule_DaySpecificRuleWinsOverEveryDayRule_AndDefaultFillsTheRest()
        {
            var schedule = new NpcSchedule("home")
                .On(DayPhase.Evening, "fire")
                .OnDay(6, DayPhase.Evening, "well");

            Assert.That(schedule.SpotFor(2, DayPhase.Morning), Is.EqualTo("home"));
            Assert.That(schedule.SpotFor(2, DayPhase.Evening), Is.EqualTo("fire"));
            Assert.That(schedule.SpotFor(6, DayPhase.Evening), Is.EqualTo("well"));
        }

        [Test]
        public void EveryResident_HasALife_AndOnlyGoesToSpotsTheBaseHas()
        {
            foreach (NpcProfile npc in PrototypeContent.CreateNpcs())
            {
                NpcLife life = PrototypeContent.FindNpcLife(npc.Id);
                Assert.That(life != null, Is.True);

                foreach (string spot in life.Schedule.AllSpots())
                {
                    bool known = false;
                    foreach (string baseSpot in PrototypeContent.BaseSpots)
                    {
                        if (baseSpot == spot) known = true;
                    }
                    Assert.That(known, Is.True);
                }
            }
        }

        [Test]
        public void EveryResident_HasSomethingToSay_AtEveryHeartLevelAndPhase()
        {
            foreach (NpcLife life in PrototypeContent.NpcLives)
            {
                for (int hearts = 0; hearts <= RelationshipTracker.MaxHearts; hearts++)
                {
                    foreach (DayPhase phase in new[] { DayPhase.Morning, DayPhase.Evening })
                    {
                        for (int day = 0; day < 7; day++)
                        {
                            string spot = life.Schedule.SpotFor(day, phase);
                            Assert.That(life.Dialogue.Pick(hearts, phase, spot, day) != null, Is.True);
                        }
                    }
                }
            }
        }

        [Test]
        public void Dialogue_ChangesWithHearts_AndIsTheSameAllDay()
        {
            NpcLife lan = PrototypeContent.FindNpcLife(PrototypeContent.NpcLan);
            var early = new HashSet<string>();
            var close = new HashSet<string>();
            for (int day = 0; day < 6; day++)
            {
                early.Add(lan.Dialogue.Pick(0, DayPhase.Morning, PrototypeContent.SpotClinic, day));
                close.Add(lan.Dialogue.Pick(5, DayPhase.Morning, PrototypeContent.SpotClinic, day));
            }

            Assert.That(early.Overlaps(close), Is.False);
            Assert.That(early.Count > 1, Is.True);
            Assert.That(
                lan.Dialogue.Pick(0, DayPhase.Morning, PrototypeContent.SpotClinic, 3),
                Is.EqualTo(lan.Dialogue.Pick(0, DayPhase.Morning, PrototypeContent.SpotClinic, 3)));
        }

        [Test]
        public void Dialogue_PrefersTheLineForTheSpot()
        {
            NpcLife lan = PrototypeContent.FindNpcLife(PrototypeContent.NpcLan);
            string atWell = lan.Dialogue.Pick(0, DayPhase.Morning, PrototypeContent.SpotWell, 6);
            string atClinic = lan.Dialogue.Pick(0, DayPhase.Morning, PrototypeContent.SpotClinic, 6);

            Assert.That(atWell.Contains("giếng"), Is.True);
            Assert.That(atClinic.Contains("giếng"), Is.False);
        }

        [Test]
        public void Format_FillsInHowThePlayerIsAddressed()
        {
            Assert.That(DialogueBook.Format("{P} về rồi à, {p} mệt không?", "anh"), Is.EqualTo("Anh về rồi à, anh mệt không?"));

            NpcLife lan = PrototypeContent.FindNpcLife(PrototypeContent.NpcLan);
            Assert.That(lan.Address(Gender.Male), Is.EqualTo("anh"));
            Assert.That(lan.Address(Gender.Female), Is.EqualTo("chị"));
        }

        [Test]
        public void HeartEvent_UnlocksAtItsHearts_InItsPhase_AndPlaysOnce()
        {
            GameSession session = NewSession();
            NpcLife lan = PrototypeContent.FindNpcLife(PrototypeContent.NpcLan);

            Assert.That(lan.NextEvent(1, DayPhase.Evening, session.Story) == null, Is.True);
            Assert.That(lan.NextEvent(2, DayPhase.Morning, session.Story) == null, Is.True);

            HeartEvent first = lan.NextEvent(2, DayPhase.Evening, session.Story);
            Assert.That(first.Id, Is.EqualTo(PrototypeContent.EventLanSupplies));

            int before = session.Relationships.Get(PrototypeContent.NpcLan).Points;
            Assert.That(session.CompleteEvent(PrototypeContent.NpcLan, first, first.Choices[0]), Is.True);
            Assert.That(session.Relationships.Get(PrototypeContent.NpcLan).Points, Is.EqualTo(before + first.Choices[0].Points));

            Assert.That(session.CompleteEvent(PrototypeContent.NpcLan, first, first.Choices[0]), Is.False);
            Assert.That(lan.NextEvent(2, DayPhase.Evening, session.Story) == null, Is.True);
        }

        [Test]
        public void HeartEvents_PlayInOrderOfHearts()
        {
            GameSession session = NewSession();
            NpcLife lan = PrototypeContent.FindNpcLife(PrototypeContent.NpcLan);

            HeartEvent next = lan.NextEvent(5, DayPhase.Evening, session.Story);
            Assert.That(next.Id, Is.EqualTo(PrototypeContent.EventLanSupplies));

            session.CompleteEvent(PrototypeContent.NpcLan, next, null);
            Assert.That(lan.NextEvent(5, DayPhase.Evening, session.Story).Id, Is.EqualTo(PrototypeContent.EventLanTea));
        }

        [Test]
        public void EveryHeartEvent_HasLinesAndTwoChoices_AndABetterAnswer()
        {
            foreach (NpcLife life in PrototypeContent.NpcLives)
            {
                foreach (HeartEvent heartEvent in life.Events)
                {
                    Assert.That(heartEvent.Lines.Count >= 3, Is.True);
                    Assert.That(heartEvent.Choices.Count, Is.EqualTo(2));
                    Assert.That(heartEvent.Choices[0].Points != heartEvent.Choices[1].Points, Is.True);
                }
            }
        }

        [Test]
        public void GiveGift_TakesTheItemOnlyWhenAccepted()
        {
            GameSession session = NewSession();
            session.Backpack.Add(PrototypeContent.TeaTin, 1);
            session.Backpack.Add(PrototypeContent.Cloth, 2);

            PlacedItem tea = Find(session.Backpack, PrototypeContent.TeaTin.Id);
            GiftTaste taste;
            Assert.That(session.GiveGift(PrototypeContent.NpcLan, session.Backpack, tea, out taste), Is.EqualTo(GiftResult.Accepted));
            Assert.That(taste, Is.EqualTo(GiftTaste.Loved));
            Assert.That(session.Backpack.CountOf(PrototypeContent.TeaTin.Id), Is.EqualTo(0));

            PlacedItem cloth = Find(session.Backpack, PrototypeContent.Cloth.Id);
            Assert.That(session.GiveGift(PrototypeContent.NpcLan, session.Backpack, cloth, out taste), Is.EqualTo(GiftResult.AlreadyGiftedToday));
            Assert.That(session.Backpack.CountOf(PrototypeContent.Cloth.Id), Is.EqualTo(2));
        }

        [Test]
        public void Phase_IsMorningUntilTheRaid_ThenEvening_AndMorningAgainAfterSleep()
        {
            GameSession session = NewSession();
            Assert.That(session.Phase, Is.EqualTo(DayPhase.Morning));

            session.TryStartRaid().Extract();
            Assert.That(session.Phase, Is.EqualTo(DayPhase.Evening));

            session.Sleep();
            Assert.That(session.Phase, Is.EqualTo(DayPhase.Morning));
        }

        [Test]
        public void Save_KeepsWhoWasMetAndWhichScenesWereSeen()
        {
            GameSession session = NewSession();
            NpcLife lan = PrototypeContent.FindNpcLife(PrototypeContent.NpcLan);
            session.Story.Set(lan.MetFlag);
            session.Story.Set(PrototypeContent.EventLanSupplies);

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(SaveMapper.Capture(session, false), 2, out report);

            Assert.That(loaded.Story.Has(lan.MetFlag), Is.True);
            Assert.That(loaded.Story.Has(PrototypeContent.EventLanSupplies), Is.True);
            Assert.That(loaded.Story.Has(PrototypeContent.EventLanTea), Is.False);
        }

        private static PlacedItem Find(GridInventory grid, string itemId)
        {
            foreach (PlacedItem placed in grid.Items)
            {
                if (placed.Item.Id == itemId) return placed;
            }
            return null;
        }
    }
}
