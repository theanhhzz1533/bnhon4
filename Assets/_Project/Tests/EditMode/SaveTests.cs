using Bnhon.Core.Calendar;
using Bnhon.Core.Content;
using Bnhon.Core.Farming;
using Bnhon.Core.Quests;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class SaveTests
    {
        /// <summary>A session with something changed in every system that is saved.</summary>
        private static GameSession PlayedSession()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Female, 3);

            session.Wallet.Add(125);
            session.Backpack.Clear();
            session.Backpack.TryPlace(PrototypeContent.Wood, 3, 4, 1, true);
            session.Backpack.Add(PrototypeContent.PistolAmmo, 11);
            session.Stash.Add(PrototypeContent.Shotgun, 1);
            session.SecurePouch.Add(PrototypeContent.TeaTin, 1);
            session.Loadout.Set(1, PrototypeContent.Shotgun, 1);
            session.Loadout.SetLoaded(0, 5);

            FarmPlot plot = session.Farm.Plots[2];
            plot.Till();
            plot.Plant(PrototypeContent.CornCrop, Season.Spring);
            plot.Water();
            session.Farm.Plots[4].Till();

            session.Relationships.Talk(PrototypeContent.NpcLan);
            session.Relationships.Gift(PrototypeContent.NpcLan, PrototypeContent.TeaTin.Id, out _);
            session.Quests.Accept("q_fence");

            session.Sleep();
            session.Sleep();
            plot.Water();
            session.Needs.Tick(100f);
            session.TryStartRaid().Extract();
            return session;
        }

        private static GameSession RoundTrip(GameSession session, bool inRaid, out LoadReport report)
        {
            SaveData data = SaveMapper.Capture(session, inRaid);
            return PrototypeContent.LoadSession(data, 99, out report);
        }

        [Test]
        public void RoundTrip_RestoresEverySavedSystem()
        {
            GameSession original = PlayedSession();

            GameSession loaded = RoundTrip(original, false, out LoadReport report);

            Assert.That(report.AbandonedRaid, Is.False);
            Assert.That(report.Dropped.Count, Is.EqualTo(0));

            Assert.That(loaded.Relationships.PlayerGender, Is.EqualTo(Gender.Female));
            Assert.That(loaded.Clock.TotalDays, Is.EqualTo(2));
            Assert.That(loaded.Wallet.Money, Is.EqualTo(original.Wallet.Money));
            Assert.That(loaded.Needs.Hunger, Is.EqualTo(original.Needs.Hunger));
            Assert.That(loaded.Needs.Thirst, Is.EqualTo(original.Needs.Thirst));
            Assert.That(loaded.RaidedToday, Is.True);
            Assert.That(loaded.IsInRaid, Is.False);

            var wood = loaded.Backpack.GetAt(4, 1);
            Assert.That(wood != null && wood.Item == PrototypeContent.Wood, Is.True);
            Assert.That(wood.Count, Is.EqualTo(3));
            Assert.That(wood.Rotated, Is.True);
            Assert.That(loaded.Backpack.CountOf(PrototypeContent.PistolAmmo.Id), Is.EqualTo(11));
            Assert.That(loaded.Backpack.Items.Count, Is.EqualTo(original.Backpack.Items.Count));
            Assert.That(loaded.Stash.CountOf(PrototypeContent.Shotgun.Id), Is.EqualTo(1));
            Assert.That(loaded.Stash.CountOf(PrototypeContent.SweetPotatoSeed.Id), Is.EqualTo(4));
            Assert.That(loaded.SecurePouch.CountOf(PrototypeContent.TeaTin.Id), Is.EqualTo(1));

            Assert.That(loaded.Loadout.GetWeapon(0) == PrototypeContent.Pistol, Is.True);
            Assert.That(loaded.Loadout.GetLoaded(0), Is.EqualTo(5));
            Assert.That(loaded.Loadout.GetWeapon(1) == PrototypeContent.Shotgun, Is.True);
            Assert.That(loaded.Loadout.GetLoaded(1), Is.EqualTo(1));

            FarmPlot plot = loaded.Farm.Plots[2];
            Assert.That(plot.State, Is.EqualTo(PlotState.Growing));
            Assert.That(plot.Crop == PrototypeContent.CornCrop, Is.True);
            Assert.That(plot.GrowthDays, Is.EqualTo(1));
            Assert.That(plot.DryDays, Is.EqualTo(1));
            Assert.That(plot.IsWatered, Is.True);
            Assert.That(loaded.Farm.Plots[4].State, Is.EqualTo(PlotState.Tilled));
            Assert.That(loaded.Farm.Plots[0].State, Is.EqualTo(PlotState.Untilled));

            Relationship lan = loaded.Relationships.Get(PrototypeContent.NpcLan);
            Assert.That(lan.Points, Is.EqualTo(100));
            Assert.That(lan.GiftsThisWeek, Is.EqualTo(1));
            Assert.That(lan.TalkedToday, Is.False);
            Assert.That(loaded.Quests.Get("q_fence").State, Is.EqualTo(QuestState.Active));
            Assert.That(loaded.Quests.Get("q_first_aid").State, Is.EqualTo(QuestState.Available));
        }

        [Test]
        public void LoadedSession_KeepsPlaying()
        {
            GameSession loaded = RoundTrip(PlayedSession(), false, out _);

            Assert.That(loaded.TryStartRaid() == null, Is.True);
            Assert.That(loaded.Sleep(), Is.True);
            Assert.That(loaded.Clock.TotalDays, Is.EqualTo(3));
            Assert.That(loaded.Farm.Plots[2].GrowthDays, Is.EqualTo(2));
            Assert.That(loaded.TryStartRaid() != null, Is.True);
        }

        [Test]
        public void SaveWrittenDuringARaid_LoadsAsADeath()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.SecurePouch.Add(PrototypeContent.TeaTin, 1);
            session.TryStartRaid();
            session.Backpack.Add(PrototypeContent.Wood, 2);

            GameSession loaded = RoundTrip(session, true, out LoadReport report);

            Assert.That(report.AbandonedRaid, Is.True);
            Assert.That(loaded.Backpack.Items.Count, Is.EqualTo(0));
            Assert.That(loaded.Loadout.HasAny, Is.False);
            Assert.That(loaded.SecurePouch.CountOf(PrototypeContent.TeaTin.Id), Is.EqualTo(1));
            Assert.That(loaded.RaidedToday, Is.True);
            Assert.That(loaded.IsInRaid, Is.False);
            // Not left with only a knife: the fallback pistol is in the stash.
            Assert.That(loaded.Stash.CountOf(PrototypeContent.Pistol.Id), Is.EqualTo(1));
        }

        [Test]
        public void UnknownIds_AreDroppedAndReported_NotFatal()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            SaveData data = SaveMapper.Capture(session, false);
            data.stash.items.Add(new ItemSave { id = "removed_item", count = 2, x = 5, y = 5 });
            data.loadout[1].id = "removed_gun";
            data.quests.Add(new QuestSave { id = "removed_quest", state = 1 });
            data.farm[0].state = (int)PlotState.Growing;
            data.farm[0].cropId = "removed_crop";

            GameSession loaded = PrototypeContent.LoadSession(data, 1, out LoadReport report);

            Assert.That(report.Dropped.Count, Is.EqualTo(4));
            Assert.That(loaded.Farm.Plots[0].State, Is.EqualTo(PlotState.Tilled));
            Assert.That(loaded.Loadout.GetWeapon(1) == null, Is.True);
            Assert.That(loaded.Stash.CountOf(PrototypeContent.SweetPotatoSeed.Id), Is.EqualTo(4));
        }

        [Test]
        public void NewerSaveVersion_IsRejected()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            SaveData data = SaveMapper.Capture(session, false);
            data.version = SaveData.CurrentVersion + 1;

            bool threw = false;
            try
            {
                PrototypeContent.LoadSession(data, 1, out _);
            }
            catch (System.FormatException)
            {
                threw = true;
            }
            Assert.That(threw, Is.True);
        }
    }
}
