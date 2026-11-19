using Bnhon.Core.Content;
using Bnhon.Core.Maps;
using Bnhon.Core.Npcs;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using Bnhon.Core.Trading;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class VyTests
    {
        private static GameSession NewSession()
        {
            return PrototypeContent.CreateNewSession(Gender.Male, 1);
        }

        private static GameSession WithVy()
        {
            GameSession session = NewSession();
            Residents.MoveIn(session, PrototypeContent.NpcVy);
            return session;
        }

        [Test]
        public void VysRescue_IsInTheGarage_FromDayFive_AndNeedsPower()
        {
            RescueDefinition rescue = PrototypeContent.FindRescue(PrototypeContent.AreaOutskirtsId, 1);
            Assert.That(rescue.NpcId, Is.EqualTo(PrototypeContent.NpcVy));
            Assert.That(rescue.NeedsPower, Is.True);
            Assert.That(rescue.WorkSeconds > 0f, Is.True);

            GameSession session = NewSession();
            session.Story.Set(Story.StoryIds.IntroDone);
            Assert.That(rescue.IsAvailable(session), Is.False);
            for (int i = 0; i < 4; i++) session.Sleep();
            Assert.That(rescue.IsAvailable(session), Is.True);
        }

        [Test]
        public void RideLevel_GrowsWithHerTasks()
        {
            GameSession session = WithVy();
            Assert.That(PrototypeContent.RideLevel(session), Is.EqualTo(0));

            session.Stash.Add(PrototypeContent.FuelCan, 1);
            session.Stash.Add(PrototypeContent.ScrapMetal, 4);
            session.Stash.Add(PrototypeContent.CircuitBoard, 1);
            Assert.That(PrototypeContent.CompleteTask(session, PrototypeContent.NpcVy) != null, Is.True);
            Assert.That(PrototypeContent.RideLevel(session), Is.EqualTo(1));

            session.Story.Set(PrototypeContent.FlagVyShop);
            session.Story.Set(PrototypeContent.FlagRideChooseTwo);
            Assert.That(PrototypeContent.RideLevel(session), Is.EqualTo(2));
        }

        [Test]
        public void TowerTask_WaitsForTheElectricianToFall_ThenOpensActTwo()
        {
            GameSession session = WithVy();
            session.Story.Set(PrototypeContent.FlagRide);
            session.Story.Set(PrototypeContent.FlagVyShop);
            session.Story.Set(PrototypeContent.FlagRideChooseTwo);
            session.Stash.Add(PrototypeContent.CircuitBoard, 3);
            session.Stash.Add(PrototypeContent.ScrapMetal, 6);

            QuestChain chain = PrototypeContent.FindChain(PrototypeContent.NpcVy);
            Assert.That(chain.Check(session), Is.EqualTo(ChainBlock.Flag));

            session.Story.Set(PrototypeContent.FlagTowerBossDead);
            Assert.That(PrototypeContent.CompleteTask(session, PrototypeContent.NpcVy) != null, Is.True);
            Assert.That(PrototypeContent.RideLevel(session), Is.EqualTo(3));
            Assert.That(session.Story.Has(PrototypeContent.FlagActTwo), Is.True);
        }

        [Test]
        public void VysShop_IsHerOwn_AndSellsGadgetsOnlyAfterHerBench()
        {
            GameSession session = WithVy();
            Shop shop = session.ShopOf(PrototypeContent.NpcVy);

            Assert.That(shop != session.Shop, Is.True);
            foreach (ShopEntry entry in shop.Entries)
            {
                Assert.That(entry.RequiredFlag != null, Is.True);
            }
        }

        [Test]
        public void Sleeping_RestocksEveryShop()
        {
            GameSession session = WithVy();
            Shop shop = session.ShopOf(PrototypeContent.NpcVy);
            ShopEntry decoys = shop.Entries[0];
            session.Wallet.Add(1000);
            Assert.That(shop.Buy(decoys, session.Wallet, session.Backpack, session.Stash), Is.True);
            Assert.That(decoys.Stock, Is.EqualTo(decoys.MaxStock - 1));

            session.Sleep();
            Assert.That(decoys.Stock, Is.EqualTo(decoys.MaxStock));
        }

        [Test]
        public void Save_KeepsEachShopsStockApart()
        {
            GameSession session = WithVy();
            Shop vy = session.ShopOf(PrototypeContent.NpcVy);
            session.Wallet.Add(1000);
            vy.Buy(vy.Entries[0], session.Wallet, session.Backpack, session.Stash);

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(SaveMapper.Capture(session, false), 2, out report);

            Assert.That(loaded.ShopOf(PrototypeContent.NpcVy).Entries[0].Stock, Is.EqualTo(vy.Entries[0].Stock));
            Assert.That(loaded.Shop.Entries[0].Stock, Is.EqualTo(session.Shop.Entries[0].Stock));
        }

        [Test]
        public void Maps_HaveThreeNamedDropOffPoints()
        {
            foreach (string name in new[] { "outskirts", "town" })
            {
                string path = System.IO.Path.Combine(TestPaths.Maps, name + ".txt");
                RaidMap map = RaidMap.Parse(System.IO.File.ReadAllText(path));
                Assert.That(map.InsertionPoints.Count, Is.EqualTo(3));
            }
            Assert.That(PrototypeContent.AreaOutskirts.InsertionNames.Count, Is.EqualTo(3));
            Assert.That(PrototypeContent.AreaTown.InsertionNames.Count, Is.EqualTo(3));
        }
    }
}
