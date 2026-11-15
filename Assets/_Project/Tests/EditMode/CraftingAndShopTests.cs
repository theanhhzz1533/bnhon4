using System;
using Bnhon.Core.Content;
using Bnhon.Core.Crafting;
using Bnhon.Core.Economy;
using Bnhon.Core.Inventory;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using Bnhon.Core.Trading;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class CraftingAndShopTests
    {
        private static Workshop NewWorkshop()
        {
            var workshop = new Workshop();
            PrototypeContent.SetUpWorkshop(workshop);
            return workshop;
        }

        private static Recipe Find(Workshop workshop, string id)
        {
            foreach (Recipe recipe in workshop.Recipes)
            {
                if (recipe.Id == id) return recipe;
            }
            return null;
        }

        [Test]
        public void Craft_TakesMaterialsFromStashThenBackpack_AndPutsResultInStash()
        {
            Workshop workshop = NewWorkshop();
            var stash = new GridInventory(4, 4);
            var backpack = new GridInventory(4, 4);
            stash.Add(PrototypeContent.Cloth, 1);
            backpack.Add(PrototypeContent.Cloth, 3);

            Assert.That(workshop.Craft(Find(workshop, "r_bandage"), stash, backpack), Is.EqualTo(CraftBlock.None));

            Assert.That(stash.CountOf(PrototypeContent.Cloth.Id), Is.EqualTo(0));
            Assert.That(backpack.CountOf(PrototypeContent.Cloth.Id), Is.EqualTo(2));
            Assert.That(stash.CountOf(PrototypeContent.Bandage.Id), Is.EqualTo(1));
        }

        [Test]
        public void Craft_ReportsWhatIsMissing()
        {
            Workshop workshop = NewWorkshop();
            var stash = new GridInventory(6, 6);
            var backpack = new GridInventory(4, 4);

            Assert.That(workshop.CheckCraft(Find(workshop, "r_bandage"), stash, backpack), Is.EqualTo(CraftBlock.Materials));
            Assert.That(workshop.CheckCraft(Find(workshop, "r_shotgun_shell"), stash, backpack), Is.EqualTo(CraftBlock.WorkbenchLevel));

            stash.Add(PrototypeContent.Wood, 5);
            stash.Add(PrototypeContent.Wood, 1);
            stash.Add(PrototypeContent.ScrapMetal, 10);
            stash.Add(PrototypeContent.Cloth, 4);
            Assert.That(workshop.Upgrade(stash, backpack), Is.EqualTo(CraftBlock.None));
            Assert.That(workshop.Level, Is.EqualTo(2));
            Assert.That(stash.CountOf(PrototypeContent.ScrapMetal.Id), Is.EqualTo(4));

            // Level is now fine, but the pistol still needs its blueprint.
            Assert.That(workshop.CheckCraft(Find(workshop, "r_pistol"), stash, backpack), Is.EqualTo(CraftBlock.Blueprint));
        }

        [Test]
        public void Blueprint_MustBeRegisteredOnce_DuplicatesAreKept()
        {
            Workshop workshop = NewWorkshop();
            var backpack = new GridInventory(4, 4);
            backpack.Add(PrototypeContent.BlueprintPistol, 1);
            backpack.Add(PrototypeContent.BlueprintPistol, 1);
            backpack.Add(PrototypeContent.Wood, 1);

            Assert.That(workshop.RegisterBlueprint(backpack, backpack.Items[2]), Is.False);
            Assert.That(workshop.RegisterBlueprint(backpack, backpack.Items[0]), Is.True);
            Assert.That(workshop.KnowsBlueprint(PrototypeContent.BlueprintPistol), Is.True);
            Assert.That(backpack.CountOf(PrototypeContent.BlueprintPistol.Id), Is.EqualTo(1));

            Assert.That(workshop.RegisterBlueprint(backpack, backpack.Items[0]), Is.False);
            Assert.That(backpack.CountOf(PrototypeContent.BlueprintPistol.Id), Is.EqualTo(1));
        }

        [Test]
        public void Craft_WithNoRoomForTheResult_ChangesNothing()
        {
            Workshop workshop = NewWorkshop();
            // Six rounds need one free cell. Both grids are exactly full of scrap that the recipe only partly uses.
            var stash = new GridInventory(1, 1);
            var backpack = new GridInventory(1, 1);
            stash.Add(PrototypeContent.ScrapMetal, 10);
            backpack.Add(PrototypeContent.ScrapMetal, 10);

            Assert.That(workshop.Craft(Find(workshop, "r_pistol_ammo"), stash, backpack), Is.EqualTo(CraftBlock.NoRoom));

            Assert.That(stash.CountOf(PrototypeContent.ScrapMetal.Id), Is.EqualTo(10));
            Assert.That(backpack.CountOf(PrototypeContent.ScrapMetal.Id), Is.EqualTo(10));
            Assert.That(stash.CountOf(PrototypeContent.PistolAmmo.Id) + backpack.CountOf(PrototypeContent.PistolAmmo.Id), Is.EqualTo(0));
        }

        [Test]
        public void Dismantle_TurnsOneItemIntoMaterials()
        {
            Workshop workshop = NewWorkshop();
            var stash = new GridInventory(6, 4);
            stash.Add(PrototypeContent.Shotgun, 1);
            stash.Add(PrototypeContent.Wood, 1);

            Assert.That(workshop.Dismantle(stash, stash.Items[1], null), Is.False);
            Assert.That(workshop.Dismantle(stash, stash.Items[0], null), Is.True);

            Assert.That(stash.CountOf(PrototypeContent.Shotgun.Id), Is.EqualTo(0));
            Assert.That(stash.CountOf(PrototypeContent.ScrapMetal.Id), Is.EqualTo(4));
            Assert.That(stash.CountOf(PrototypeContent.Wood.Id), Is.EqualTo(2));
        }

        [Test]
        public void Shop_BuyTakesMoneyAndStock_AndDeliversTheBundle()
        {
            var shop = new Shop();
            ShopEntry ammo = shop.AddEntry(PrototypeContent.PistolAmmo, 10, 30, 2);
            shop.FillStock();
            var wallet = new Wallet(50);
            var backpack = new GridInventory(2, 2);
            var stash = new GridInventory(2, 2);

            Assert.That(shop.Buy(ammo, wallet, backpack, stash), Is.True);
            Assert.That(wallet.Money, Is.EqualTo(20));
            Assert.That(ammo.Stock, Is.EqualTo(1));
            Assert.That(backpack.CountOf(PrototypeContent.PistolAmmo.Id), Is.EqualTo(10));

            Assert.That(shop.Buy(ammo, wallet, backpack, stash), Is.False);
            Assert.That(ammo.Stock, Is.EqualTo(1));
        }

        [Test]
        public void Shop_BuyWithNoRoom_CostsNothing()
        {
            var shop = new Shop();
            ShopEntry pistol = shop.AddEntry(PrototypeContent.Pistol, 1, 100, 1);
            shop.FillStock();
            var wallet = new Wallet(500);
            var backpack = new GridInventory(1, 1);
            var stash = new GridInventory(1, 1);

            Assert.That(shop.Buy(pistol, wallet, backpack, stash), Is.False);
            Assert.That(wallet.Money, Is.EqualTo(500));
            Assert.That(pistol.Stock, Is.EqualTo(1));
        }

        [Test]
        public void Shop_SellPaysHalfValue_AndRefusesWorthlessItems()
        {
            var shop = new Shop();
            var wallet = new Wallet();
            var grid = new GridInventory(4, 4);
            grid.Add(PrototypeContent.CircuitBoard, 3);

            Assert.That(Shop.SellPrice(PrototypeContent.CircuitBoard), Is.EqualTo(30));
            Assert.That(shop.Sell(grid, grid.Items[0], 2, wallet), Is.EqualTo(60));
            Assert.That(wallet.Money, Is.EqualTo(60));
            Assert.That(grid.CountOf(PrototypeContent.CircuitBoard.Id), Is.EqualTo(1));

            Assert.That(shop.Sell(grid, grid.Items[0], 99, wallet), Is.EqualTo(30));
            Assert.That(grid.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void Shop_RestocksOnSleep()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            ShopEntry ammo = session.Shop.Entries[0];
            session.Wallet.Add(1000);
            while (session.Shop.Buy(ammo, session.Wallet, session.Backpack, session.Stash)) { }
            Assert.That(ammo.Stock, Is.EqualTo(0));

            session.Sleep();

            Assert.That(ammo.Stock, Is.EqualTo(ammo.MaxStock));
        }

        [Test]
        public void Prices_NeverLetBuyingAndResellingMakeMoney()
        {
            var shop = new Shop();
            PrototypeContent.SetUpShop(shop);
            foreach (ShopEntry entry in shop.Entries)
            {
                Assert.That(Shop.SellPrice(entry.Item) * entry.Bundle < entry.Price, Is.True);
            }
        }

        [Test]
        public void Save_KeepsWorkbenchBlueprintsAndShopStock()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.Stash.Add(PrototypeContent.Wood, 5);
            session.Stash.Add(PrototypeContent.Wood, 1);
            session.Stash.Add(PrototypeContent.ScrapMetal, 6);
            session.Stash.Add(PrototypeContent.Cloth, 4);
            session.Workshop.Upgrade(session.Stash, session.Backpack);
            session.Stash.Add(PrototypeContent.BlueprintMedKit, 1);
            session.Workshop.RegisterBlueprint(session.Stash, session.Stash.Items[session.Stash.Items.Count - 1]);
            session.Shop.Buy(session.Shop.Entries[0], session.Wallet, session.Backpack, session.Stash);

            GameSession loaded = PrototypeContent.LoadSession(SaveMapper.Capture(session, false), 2, out _);

            Assert.That(loaded.Workshop.Level, Is.EqualTo(2));
            Assert.That(loaded.Workshop.KnowsBlueprint(PrototypeContent.BlueprintMedKit), Is.True);
            Assert.That(loaded.Workshop.KnowsBlueprint(PrototypeContent.BlueprintPistol), Is.False);
            Assert.That(loaded.Shop.Entries[0].Stock, Is.EqualTo(session.Shop.Entries[0].MaxStock - 1));
        }

        [Test]
        public void SaveFromBeforeCrafting_LoadsWithDefaults()
        {
            var session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            SaveData data = SaveMapper.Capture(session, false);
            data.version = 1;
            data.workbenchLevel = 0;
            data.blueprints = null;
            data.shopSaved = false;
            data.shopStock = null;

            GameSession loaded = PrototypeContent.LoadSession(data, 2, out _);

            Assert.That(loaded.Workshop.Level, Is.EqualTo(1));
            Assert.That(loaded.Shop.Entries[0].Stock, Is.EqualTo(loaded.Shop.Entries[0].MaxStock));
        }
    }
}
