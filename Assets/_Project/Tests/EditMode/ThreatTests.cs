using Bnhon.Core.Combat;
using Bnhon.Core.Content;
using Bnhon.Core.Maps;
using Bnhon.Core.Raid;
using Bnhon.Core.Relationships;
using Bnhon.Core.Story;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class ThreatTests
    {
        [Test]
        public void Threat_StartsAtZero_RisesEveryTwoDays_AndIsCapped()
        {
            Assert.That(Threat.Level(0), Is.EqualTo(0));
            Assert.That(Threat.Level(1), Is.EqualTo(0));
            Assert.That(Threat.Level(2), Is.EqualTo(1));
            Assert.That(Threat.Level(9), Is.EqualTo(4));
            Assert.That(Threat.Level(500), Is.EqualTo(Threat.MaxLevel));
        }

        [Test]
        public void AtLevelZero_EveryWalkerStaysAWalker()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 200; i++)
            {
                Assert.That(Threat.Roll(0, rng), Is.EqualTo(ThreatUpgrade.None));
            }
        }

        [Test]
        public void ArmouredZombies_OnlyAppearFromLevelTwo()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 300; i++)
            {
                Assert.That(Threat.Roll(1, rng) == ThreatUpgrade.Armored, Is.False);
            }
            Assert.That(Threat.ArmoredChance(2) > 0.0, Is.True);
        }

        [Test]
        public void HigherLevels_UpgradeMoreWalkers_ButMostStayWalkers()
        {
            int low = CountUpgrades(2);
            int high = CountUpgrades(Threat.MaxLevel);

            Assert.That(high > low, Is.True);
            Assert.That(high < 600, Is.True);
        }

        private static int CountUpgrades(int level)
        {
            var rng = new System.Random(11);
            int upgraded = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (Threat.Roll(level, rng) != ThreatUpgrade.None) upgraded++;
            }
            return upgraded;
        }

        [Test]
        public void Armor_StopsHitsFromTheFront_NotFromTheSideOrBehind()
        {
            // Facing right (+x).
            Assert.That(Armor.Apply(100f, 0.75f, 1f, 0f, 5f, 0f), Is.EqualTo(25f));
            Assert.That(Armor.Apply(100f, 0.75f, 1f, 0f, 3f, 2f), Is.EqualTo(25f));
            Assert.That(Armor.Apply(100f, 0.75f, 1f, 0f, 0f, 4f), Is.EqualTo(100f));
            Assert.That(Armor.Apply(100f, 0.75f, 1f, 0f, -5f, 0f), Is.EqualTo(100f));
        }

        [Test]
        public void Shield_WearsDownUnderFrontalHits_ThenStopsNothing()
        {
            var shield = new Shield(0.75f, 100f);

            // Facing right; three 34-damage shots from the front.
            Assert.That(shield.Absorb(34f, 1f, 0f, 5f, 0f), Is.EqualTo(8.5f));
            Assert.That(shield.Absorb(34f, 1f, 0f, 5f, 0f), Is.EqualTo(8.5f));
            Assert.That(shield.IsBroken, Is.False);
            Assert.That(shield.Absorb(34f, 1f, 0f, 5f, 0f), Is.EqualTo(8.5f));
            Assert.That(shield.IsBroken, Is.True);

            Assert.That(shield.Absorb(34f, 1f, 0f, 5f, 0f), Is.EqualTo(34f));
        }

        [Test]
        public void Shield_IsNotWornByHitsFromBehind()
        {
            var shield = new Shield(0.75f, 100f);

            Assert.That(shield.Absorb(40f, 1f, 0f, -5f, 0f), Is.EqualTo(40f));
            Assert.That(shield.Durability, Is.EqualTo(100f));
        }

        [Test]
        public void TownCentre_IsLockedUntilTheWorkbenchStep_AndItsBossStaysDead()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            RaidArea town = PrototypeContent.AreaTown;

            Assert.That(PrototypeContent.AreaOutskirts.IsUnlocked(session), Is.True);
            Assert.That(town.IsUnlocked(session), Is.False);

            session.Story.Set("step_workbench");
            Assert.That(town.IsUnlocked(session), Is.True);

            Assert.That(town.BossDefeated(session), Is.False);
            session.Story.Set(PrototypeContent.FlagButcherDead);
            Assert.That(town.BossDefeated(session), Is.True);
        }

        [Test]
        public void UnlockFlagOfTheTown_IsARealStoryStep()
        {
            bool found = false;
            foreach (StoryStep step in PrototypeContent.OpeningStory.Steps)
            {
                if (step.Flag == PrototypeContent.AreaTown.UnlockFlag) found = true;
            }
            Assert.That(found, Is.True);
        }

        [Test]
        public void Map_AcceptsSafesArmouredZombiesAndABoss()
        {
            RaidMap map = RaidMap.Parse("#######\n#P.V.E#\n#.A.B.#\n#######\n");
            Assert.That(map.Markers.Count, Is.EqualTo(4));
        }
    }
}
