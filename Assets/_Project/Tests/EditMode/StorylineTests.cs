using Bnhon.Core.Calendar;
using Bnhon.Core.Content;
using Bnhon.Core.Maps;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using Bnhon.Core.Story;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class StorylineTests
    {
        private static GameSession NewGame()
        {
            return PrototypeContent.CreateNewGame(Gender.Male, 1);
        }

        [Test]
        public void NewGame_StartsInTheIntro_WithNothingAndNoKnife()
        {
            GameSession session = NewGame();

            Assert.That(session.InIntro, Is.True);
            Assert.That(session.HasKnife, Is.False);
            Assert.That(session.Backpack.Items.Count, Is.EqualTo(0));
            Assert.That(session.Loadout.HasAny, Is.False);
            Assert.That(PrototypeContent.OpeningStory.Current(session).Id, Is.EqualTo("wake"));
        }

        [Test]
        public void Steps_CompleteInOrder_OneAtATime()
        {
            GameSession session = NewGame();
            Storyline story = PrototypeContent.OpeningStory;

            Assert.That(story.Advance(session) == null, Is.True);

            // Reaching the canteen before finding the knife does not skip the first step.
            session.Story.Set(StoryIds.ReachedCanteen);
            Assert.That(story.Advance(session) == null, Is.True);

            session.Story.Set(StoryIds.HasKnife);
            Assert.That(story.Advance(session).Id, Is.EqualTo("wake"));
            Assert.That(story.Advance(session).Id, Is.EqualTo("escape"));
            Assert.That(story.Advance(session) == null, Is.True);
            Assert.That(story.Current(session).Id, Is.EqualTo("run"));
        }

        [Test]
        public void FinishIntro_MovesThePlayerToTheBase_WithMinhsKitInTheStash()
        {
            GameSession session = NewGame();
            session.Story.Set(StoryIds.HasKnife);
            PrototypeContent.FinishIntro(session);

            Assert.That(session.InIntro, Is.False);
            Assert.That(session.HasKnife, Is.True);
            Assert.That(session.Stash.CountOf(PrototypeContent.Pistol.Id), Is.EqualTo(1));
            Assert.That(session.Stash.CountOf(PrototypeContent.PistolAmmo.Id), Is.EqualTo(16));
            Assert.That(session.Story.Has("met_" + PrototypeContent.NpcMinh), Is.True);

            // Calling it again gives nothing more.
            PrototypeContent.FinishIntro(session);
            Assert.That(session.Stash.CountOf(PrototypeContent.Pistol.Id), Is.EqualTo(1));
        }

        [Test]
        public void WholeChain_CanBePlayedThrough_AndPaysItsRewards()
        {
            GameSession session = NewGame();
            Storyline story = PrototypeContent.OpeningStory;

            session.Story.Set(StoryIds.HasKnife);
            session.Story.Set(StoryIds.ReachedCanteen);
            PrototypeContent.FinishIntro(session);
            Assert.That(story.Advance(session).Id, Is.EqualTo("wake"));
            Assert.That(story.Advance(session).Id, Is.EqualTo("escape"));
            Assert.That(story.Advance(session).Id, Is.EqualTo("run"));

            session.Story.Set("met_" + PrototypeContent.NpcLan);
            Assert.That(story.Advance(session) == null, Is.True);
            session.Story.Set("met_" + PrototypeContent.NpcBaTu);
            Assert.That(story.Advance(session).Id, Is.EqualTo("meet"));

            session.Sleep();
            Assert.That(story.Advance(session).Id, Is.EqualTo("rest"));

            int seedsBefore = session.Stash.CountOf(PrototypeContent.MugwortSeed.Id);
            session.Farm.Plots[0].Till();
            session.Farm.Plots[0].Plant(PrototypeContent.SweetPotatoCrop, Season.Spring);
            Assert.That(story.Advance(session) == null, Is.True);
            session.WateringCan.WaterPlot(session.Farm.Plots[0]);
            Assert.That(story.Advance(session).Id, Is.EqualTo("farm"));
            Assert.That(session.Stash.CountOf(PrototypeContent.MugwortSeed.Id), Is.EqualTo(seedsBefore + 2));

            int moneyBefore = session.Wallet.Money;
            session.Story.Set(StoryIds.Extracted);
            Assert.That(story.Advance(session).Id, Is.EqualTo("first_raid"));
            Assert.That(session.Wallet.Money, Is.EqualTo(moneyBefore + 60));

            Assert.That(story.Advance(session) == null, Is.True);
            Bnhon.Core.Npcs.Residents.MoveIn(session, PrototypeContent.NpcChuSau);
            Assert.That(story.Advance(session).Id, Is.EqualTo("rescue_sau"));

            session.Stash.Add(PrototypeContent.Wood, 5);
            session.Stash.Add(PrototypeContent.Wood, 1);
            session.Stash.Add(PrototypeContent.ScrapMetal, 6);
            session.Stash.Add(PrototypeContent.Cloth, 4);
            session.Workshop.Upgrade(session.Stash, session.Backpack);
            Assert.That(story.Advance(session).Id, Is.EqualTo("workbench"));
            Assert.That(session.Stash.CountOf(PrototypeContent.BlueprintMedKit.Id), Is.EqualTo(1));

            Assert.That(story.Advance(session) == null, Is.True);
            session.Story.Set(PrototypeContent.FlagButcherDead);
            Assert.That(story.Advance(session).Id, Is.EqualTo("town"));

            Assert.That(story.Current(session) == null, Is.True);
            Assert.That(story.Advance(session) == null, Is.True);
        }

        [Test]
        public void Save_KeepsStoryProgress()
        {
            GameSession session = NewGame();
            session.Story.Set(StoryIds.HasKnife);
            PrototypeContent.OpeningStory.Advance(session);
            PrototypeContent.FinishIntro(session);

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(SaveMapper.Capture(session, false), 2, out report);

            Assert.That(loaded.InIntro, Is.False);
            Assert.That(PrototypeContent.OpeningStory.Current(loaded).Id, Is.EqualTo("escape"));
        }

        [Test]
        public void SaveFromBeforeTheStory_IsPastTheIntro_WithNoObjectivesAndNoFreeRewards()
        {
            GameSession old = PrototypeContent.CreateNewSession(Gender.Male, 1);
            SaveData data = SaveMapper.Capture(old, false);
            data.version = 4;

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(data, 2, out report);

            Assert.That(loaded.InIntro, Is.False);
            Assert.That(loaded.HasKnife, Is.True);
            Assert.That(PrototypeContent.OpeningStory.Current(loaded) == null, Is.True);
            Assert.That(loaded.Wallet.Money, Is.EqualTo(old.Wallet.Money));
        }

        [Test]
        public void StoryMap_ParsesItsSymbols_AndARescuerCountsAsTheWayOut()
        {
            RaidMap map = RaidMap.Parse("#######\n#PK.T.#\n#.H..N#\n#######\n");

            Assert.That(map.Markers.Count, Is.EqualTo(4));
            Assert.That(map.FindUnreachableMarkers().Count, Is.EqualTo(0));
        }
    }
}
