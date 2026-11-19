using Bnhon.Core.Content;
using Bnhon.Core.Npcs;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using Bnhon.Core.Trading;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class ResidentTests
    {
        private static GameSession NewSession()
        {
            return PrototypeContent.CreateNewSession(Gender.Male, 1);
        }

        private static QuestChain SauChain => PrototypeContent.FindChain(PrototypeContent.NpcChuSau);

        [Test]
        public void NewGame_HasTheFoundersAtTheBase_ButNotChuSau()
        {
            GameSession session = NewSession();

            foreach (string founder in PrototypeContent.Founders)
            {
                Assert.That(Residents.LivesAtBase(session, founder), Is.True);
            }
            Assert.That(Residents.LivesAtBase(session, PrototypeContent.NpcChuSau), Is.False);
            Assert.That(PrototypeContent.ShopOpen(session), Is.False);
        }

        [Test]
        public void ChuSausRescue_IsInTheOutskirts_AfterTheIntro_AndNeedsABandage()
        {
            RescueDefinition rescue = PrototypeContent.FindRescue(PrototypeContent.AreaOutskirtsId, 1);
            Assert.That(rescue.NpcId, Is.EqualTo(PrototypeContent.NpcChuSau));
            Assert.That(rescue.Needs.Value.Item, Is.EqualTo(PrototypeContent.Bandage));

            GameSession inIntro = PrototypeContent.CreateNewGame(Gender.Male, 1);
            Assert.That(rescue.IsAvailable(inIntro), Is.False);
            GameSession afterIntro = NewSession();
            afterIntro.Story.Set(Bnhon.Core.Story.StoryIds.IntroDone);
            Assert.That(rescue.IsAvailable(afterIntro), Is.True);
            Assert.That(rescue.Arrival.Count > 0, Is.True);
        }

        [Test]
        public void Chain_IsClosedUntilTheResidentMovesIn()
        {
            GameSession session = NewSession();
            Assert.That(SauChain.Check(session), Is.EqualTo(ChainBlock.NotResident));

            Residents.MoveIn(session, PrototypeContent.NpcChuSau);
            Assert.That(SauChain.Check(session), Is.EqualTo(ChainBlock.Items));
        }

        [Test]
        public void FirstTask_TakesTheItems_AndOpensTheShop()
        {
            GameSession session = NewSession();
            Residents.MoveIn(session, PrototypeContent.NpcChuSau);
            session.Stash.Add(PrototypeContent.CannedFood, 5);
            session.Backpack.Add(PrototypeContent.WaterBottle, 2);
            int foodBefore = session.Stash.CountOf(PrototypeContent.CannedFood.Id);

            ChainStep step = PrototypeContent.CompleteTask(session, PrototypeContent.NpcChuSau);

            Assert.That(step.UnlockFlag, Is.EqualTo(PrototypeContent.FlagShopOpen));
            Assert.That(PrototypeContent.ShopOpen(session), Is.True);
            Assert.That(session.Stash.CountOf(PrototypeContent.CannedFood.Id), Is.EqualTo(foodBefore - 2));
            Assert.That(SauChain.Current(session).UnlockFlag, Is.EqualTo(PrototypeContent.FlagShopStock));
        }

        [Test]
        public void Task_WithoutEverything_TakesNothing()
        {
            GameSession session = NewSession();
            Residents.MoveIn(session, PrototypeContent.NpcChuSau);
            int food = session.Stash.CountOf(PrototypeContent.CannedFood.Id) + session.Backpack.CountOf(PrototypeContent.CannedFood.Id);
            session.Stash.TryConsume(PrototypeContent.WaterBottle.Id, session.Stash.CountOf(PrototypeContent.WaterBottle.Id));
            session.Backpack.TryConsume(PrototypeContent.WaterBottle.Id, session.Backpack.CountOf(PrototypeContent.WaterBottle.Id));

            Assert.That(PrototypeContent.CompleteTask(session, PrototypeContent.NpcChuSau) == null, Is.True);
            Assert.That(session.Stash.CountOf(PrototypeContent.CannedFood.Id) + session.Backpack.CountOf(PrototypeContent.CannedFood.Id), Is.EqualTo(food));
            Assert.That(PrototypeContent.ShopOpen(session), Is.False);
        }

        [Test]
        public void HeartStep_WaitsForTheBond()
        {
            GameSession session = NewSession();
            Residents.MoveIn(session, PrototypeContent.NpcChuSau);
            session.Story.Set(PrototypeContent.FlagShopOpen);
            session.Story.Set(PrototypeContent.FlagShopStock);
            session.Story.Set(PrototypeContent.FlagShopRare);
            session.Stash.Add(PrototypeContent.OldCassette, 1);

            Assert.That(SauChain.Check(session), Is.EqualTo(ChainBlock.Hearts));

            session.Relationships.AddPoints(PrototypeContent.NpcChuSau, RelationshipTracker.PointsPerHeart * 3);
            Assert.That(SauChain.Check(session), Is.EqualTo(ChainBlock.None));
        }

        [Test]
        public void LaterTasks_ChangePricesAndStock()
        {
            GameSession session = NewSession();
            Shop shop = session.Shop;
            ShopEntry ammo = shop.Entries[0];
            int fullPrice = shop.PriceOf(ammo);

            session.Story.Set(PrototypeContent.FlagShopDiscount);
            session.Story.Set(PrototypeContent.FlagShopCart);
            PrototypeContent.ApplyServices(session);

            Assert.That(shop.PriceOf(ammo) < fullPrice, Is.True);
            Assert.That(shop.StockLimit(ammo), Is.EqualTo(ammo.MaxStock + 1));
            Assert.That(shop.SellPriceOf(PrototypeContent.Pistol) > Shop.SellPrice(PrototypeContent.Pistol), Is.True);
        }

        [Test]
        public void EvenWithEveryBonus_NothingCanBeBoughtAndSoldBackAtAProfit()
        {
            GameSession session = NewSession();
            foreach (ChainStep step in SauChain.Steps) session.Story.Set(step.UnlockFlag);
            PrototypeContent.ApplyServices(session);

            foreach (ShopEntry entry in session.Shop.Entries)
            {
                Assert.That(session.Shop.SellPriceOf(entry.Item) * entry.Bundle < session.Shop.PriceOf(entry), Is.True);
            }
        }

        [Test]
        public void EveryChainStep_HasAFlagNoOtherStepUses()
        {
            var flags = new System.Collections.Generic.HashSet<string>();
            foreach (QuestChain chain in PrototypeContent.Chains)
            {
                foreach (ChainStep step in chain.Steps)
                {
                    Assert.That(flags.Add(step.UnlockFlag), Is.True);
                    Assert.That(string.IsNullOrEmpty(step.RewardText), Is.False);
                }
            }
        }

        [Test]
        public void Save_KeepsResidentsAndTasks()
        {
            GameSession session = NewSession();
            Residents.MoveIn(session, PrototypeContent.NpcChuSau);
            session.Story.Set(PrototypeContent.FlagShopOpen);
            session.Story.Set(PrototypeContent.FlagShopDiscount);

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(SaveMapper.Capture(session, false), 2, out report);

            Assert.That(Residents.LivesAtBase(loaded, PrototypeContent.NpcChuSau), Is.True);
            Assert.That(Residents.LivesAtBase(loaded, PrototypeContent.NpcLan), Is.True);
            Assert.That(PrototypeContent.ShopOpen(loaded), Is.True);
            Assert.That(loaded.Shop.PriceFactor < 1.0, Is.True);
        }

        [Test]
        public void SaveFromBeforeRescues_KeepsItsShopOpen()
        {
            GameSession old = NewSession();
            SaveData data = SaveMapper.Capture(old, false);
            data.version = 6;
            data.storyFlags.Clear();

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(data, 2, out report);

            Assert.That(PrototypeContent.ShopOpen(loaded), Is.True);
            Assert.That(Residents.LivesAtBase(loaded, PrototypeContent.NpcChuSau), Is.True);
            Assert.That(Residents.LivesAtBase(loaded, PrototypeContent.NpcBaTu), Is.True);
        }
    }
}
