using Bnhon.Core.Charms;
using Bnhon.Core.Combat;
using Bnhon.Core.Content;
using Bnhon.Core.Npcs;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class CharmTests
    {
        private static GameSession NewSession()
        {
            return PrototypeContent.CreateNewSession(Gender.Male, 1);
        }

        private static GameSession WithLab()
        {
            GameSession session = NewSession();
            Residents.MoveIn(session, PrototypeContent.NpcQuan);
            session.Story.Set(PrototypeContent.FlagLabOpen);
            PrototypeContent.ApplyServices(session);
            return session;
        }

        [Test]
        public void Brewing_NeedsTheLab()
        {
            GameSession session = NewSession();
            session.Stash.Add(PrototypeContent.Sample, 20);
            Assert.That(PrototypeContent.BrewCharm(session) == null, Is.True);
        }

        [Test]
        public void FirstBrew_IsFree_ThenCostsEightSamples()
        {
            GameSession session = WithLab();

            Assert.That(PrototypeContent.FirstBrewFree(session), Is.True);
            Assert.That(PrototypeContent.BrewCharm(session) != null, Is.True);
            Assert.That(PrototypeContent.FirstBrewFree(session), Is.False);

            Assert.That(PrototypeContent.BrewCharm(session) == null, Is.True);

            session.Stash.Add(PrototypeContent.Sample, 5);
            session.Backpack.Add(PrototypeContent.Sample, 5);
            Assert.That(PrototypeContent.BrewCharm(session) != null, Is.True);
            int left = session.Stash.CountOf(PrototypeContent.Sample.Id) + session.Backpack.CountOf(PrototypeContent.Sample.Id);
            Assert.That(left, Is.EqualTo(2));
        }

        [Test]
        public void Rarity_IsMostlyCommon_SometimesRare_RarelyEpic()
        {
            var rng = new System.Random(5);
            int common = 0, rare = 0, epic = 0;
            for (int i = 0; i < 4000; i++)
            {
                switch (CharmLab.RollRarity(rng))
                {
                    case CharmRarity.Common: common++; break;
                    case CharmRarity.Rare: rare++; break;
                    default: epic++; break;
                }
            }
            Assert.That(common > 2600 && common < 3000, Is.True);
            Assert.That(rare > 850 && rare < 1150, Is.True);
            Assert.That(epic > 120 && epic < 290, Is.True);
        }

        [Test]
        public void CoreBrew_IsAlwaysRareOrBetter_AndUsesTheCore()
        {
            GameSession session = WithLab();
            session.Story.Set(PrototypeContent.FlagLabCores);
            session.Stash.Add(PrototypeContent.CoreElectrician, 1);

            CharmDefinition charm = PrototypeContent.BrewFromCore(session, PrototypeContent.CoreElectrician);
            Assert.That(charm.Rarity != CharmRarity.Common, Is.True);
            Assert.That(session.Stash.CountOf(PrototypeContent.CoreElectrician.Id), Is.EqualTo(0));
        }

        [Test]
        public void GroupBrew_GivesThatGroup_AndNeedsItsUnlock()
        {
            GameSession session = WithLab();
            session.Stash.Add(PrototypeContent.Sample, 30);
            Assert.That(PrototypeContent.BrewCharm(session, CharmGroup.Combat) == null, Is.True);

            session.Story.Set(PrototypeContent.FlagLabGroups);
            Assert.That(PrototypeContent.BrewCharm(session, CharmGroup.Combat).Group, Is.EqualTo(CharmGroup.Combat));
            Assert.That(session.Stash.CountOf(PrototypeContent.Sample.Id), Is.EqualTo(18));
        }

        [Test]
        public void TwoSlots_ACharmOnlyOnce_AndWornCharmsAddUp()
        {
            GameSession session = NewSession();
            CharmCollection charms = session.Charms;
            CharmDefinition lungs = PrototypeContent.FindCharm("charm_strong_lungs");
            CharmDefinition skin = PrototypeContent.FindCharm("charm_thick_skin");
            CharmDefinition shock = PrototypeContent.FindCharm("charm_shock");
            charms.Add(lungs);
            charms.Add(skin);
            charms.Add(shock);

            Assert.That(charms.Wear(lungs, 0), Is.True);
            Assert.That(charms.Wear(lungs, 1), Is.False);
            Assert.That(charms.Wear(skin, 1), Is.True);
            Assert.That(charms.Wear(shock, 2), Is.False);
            Assert.That(charms.Total(CharmStat.MaxHealth), Is.EqualTo(25f));

            session.Story.Set(PrototypeContent.FlagLabThirdSlot);
            PrototypeContent.ApplyServices(session);
            Assert.That(charms.Wear(shock, 2), Is.True);
        }

        [Test]
        public void CharmsAreNotItems_SoDyingInARaidKeepsThem()
        {
            GameSession session = NewSession();
            CharmDefinition skin = PrototypeContent.FindCharm("charm_thick_skin");
            session.Charms.Add(skin);
            session.Charms.Wear(skin, 0);

            session.TryStartRaid().Die();

            Assert.That(session.Charms.IsWorn(skin), Is.True);
        }

        [Test]
        public void BreakingDown_OnlySpares_IntoSamples()
        {
            GameSession session = WithLab();
            session.Story.Set(PrototypeContent.FlagLabBreakdown);
            CharmDefinition rare = PrototypeContent.FindCharm("charm_reflex");
            session.Charms.Add(rare);

            Assert.That(PrototypeContent.BreakDownCharm(session, rare), Is.EqualTo(0));

            session.Charms.Add(rare);
            int before = session.Stash.CountOf(PrototypeContent.Sample.Id);
            Assert.That(PrototypeContent.BreakDownCharm(session, rare), Is.EqualTo(5));
            Assert.That(session.Stash.CountOf(PrototypeContent.Sample.Id), Is.EqualTo(before + 5));
            Assert.That(session.Charms.CountOf(rare.Id), Is.EqualTo(1));
        }

        [Test]
        public void EveryGroupAndRarity_HasCharms()
        {
            foreach (CharmGroup group in new[] { CharmGroup.Stealth, CharmGroup.Survival, CharmGroup.Combat })
            {
                int count = 0;
                foreach (CharmDefinition charm in PrototypeContent.Lab.Charms)
                {
                    if (charm.Group == group) count++;
                }
                Assert.That(count >= 3, Is.True);
            }
            Assert.That(PrototypeContent.Lab.Charms.Count, Is.EqualTo(14));
        }

        [Test]
        public void ShieldWear_CanBeRaised()
        {
            var plain = new Shield(0.75f, 100f);
            var weakened = new Shield(0.75f, 100f);
            plain.Absorb(40f, 1f, 0f, 5f, 0f);
            weakened.Absorb(40f, 1f, 0f, 5f, 0f, 1.5f);
            Assert.That(plain.Durability, Is.EqualTo(60f));
            Assert.That(weakened.Durability, Is.EqualTo(40f));
        }

        [Test]
        public void Bosses_DropTheirCoresAndSamples()
        {
            bool electrician = false, butcher = false;
            foreach (var item in PrototypeContent.AreaOutskirts.BossLoot)
            {
                if (item.Item == PrototypeContent.CoreElectrician) electrician = true;
            }
            foreach (var item in PrototypeContent.AreaTown.BossLoot)
            {
                if (item.Item == PrototypeContent.CoreButcher) butcher = true;
            }
            Assert.That(electrician && butcher, Is.True);
        }

        [Test]
        public void QuansRescue_ComesAfterTheFirstRaid_AndNeedsTheAreaClear()
        {
            RescueDefinition rescue = PrototypeContent.FindRescue(PrototypeContent.AreaOutskirtsId, 0);
            Assert.That(rescue.NpcId, Is.EqualTo(PrototypeContent.NpcQuan));
            Assert.That(rescue.ClearRadius > 0f, Is.True);

            GameSession session = NewSession();
            session.Story.Set(Story.StoryIds.IntroDone);
            Assert.That(rescue.IsAvailable(session), Is.False);
            session.Story.Set(Story.StoryIds.Extracted);
            Assert.That(rescue.IsAvailable(session), Is.True);
        }

        [Test]
        public void Save_KeepsCharmsCountsAndWhatIsWorn()
        {
            GameSession session = WithLab();
            session.Story.Set(PrototypeContent.FlagLabThirdSlot);
            PrototypeContent.ApplyServices(session);
            CharmDefinition skin = PrototypeContent.FindCharm("charm_thick_skin");
            CharmDefinition reflex = PrototypeContent.FindCharm("charm_reflex");
            session.Charms.Add(skin, 2);
            session.Charms.Add(reflex);
            session.Charms.Wear(reflex, 2);

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(SaveMapper.Capture(session, false), 2, out report);

            Assert.That(loaded.Charms.CountOf(skin.Id), Is.EqualTo(2));
            Assert.That(loaded.Charms.WornIn(2) == reflex, Is.True);
            Assert.That(loaded.Charms.WornIn(0) == null, Is.True);
        }
    }
}
