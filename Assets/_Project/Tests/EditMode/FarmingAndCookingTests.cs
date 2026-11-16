using Bnhon.Core.Calendar;
using Bnhon.Core.Content;
using Bnhon.Core.Crafting;
using Bnhon.Core.Farming;
using Bnhon.Core.Inventory;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using Bnhon.Core.Trading;
using Bnhon.Core.Vitals;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class FarmingAndCookingTests
    {
        private static FarmPlot GrowingPlot()
        {
            var plot = new FarmPlot();
            plot.Till();
            plot.Plant(PrototypeContent.SweetPotatoCrop, Season.Spring);
            return plot;
        }

        [Test]
        public void WateringCan_UsesOneChargePerPlot_AndNoneWhenPlotIsAlreadyWatered()
        {
            var can = new WateringCan(2);
            FarmPlot plot = GrowingPlot();

            Assert.That(can.WaterPlot(plot), Is.True);
            Assert.That(can.Water, Is.EqualTo(1));
            Assert.That(can.WaterPlot(plot), Is.False);
            Assert.That(can.Water, Is.EqualTo(1));
        }

        [Test]
        public void WateringCan_EmptyCanWatersNothing_UntilRefilled()
        {
            var can = new WateringCan(1);
            Assert.That(can.WaterPlot(GrowingPlot()), Is.True);

            FarmPlot dry = GrowingPlot();
            Assert.That(can.WaterPlot(dry), Is.False);
            Assert.That(dry.IsWatered, Is.False);

            can.Refill();
            Assert.That(can.WaterPlot(dry), Is.True);
        }

        [Test]
        public void EverySeason_HasAtLeastTwoCrops()
        {
            foreach (Season season in new[] { Season.Spring, Season.Summer, Season.Autumn })
            {
                int count = 0;
                foreach (CropDefinition crop in PrototypeContent.Crops)
                {
                    if (crop.GrowsIn(season)) count++;
                }
                Assert.That(count >= 2, Is.True);
            }

            Assert.That(PrototypeContent.CabbageCrop.GrowsIn(Season.Winter), Is.True);
            Assert.That(PrototypeContent.Crops.Count, Is.EqualTo(6));
        }

        [Test]
        public void EveryCrop_HasItsSeedAndProduceInTheItemList_AndTheShopSellsTheSeed()
        {
            var shop = new Shop();
            PrototypeContent.SetUpShop(shop);

            foreach (CropDefinition crop in PrototypeContent.Crops)
            {
                Assert.That(PrototypeContent.FindItem(crop.Seed.Id) != null, Is.True);
                Assert.That(PrototypeContent.FindItem(crop.Produce.Id) != null, Is.True);

                bool sold = false;
                foreach (ShopEntry entry in shop.Entries)
                {
                    if (entry.Item == crop.Seed) sold = true;
                }
                Assert.That(sold, Is.True);
            }
        }

        [Test]
        public void Buffs_AddUp_RunOut_AndDoNotStackWithThemselves()
        {
            var buffs = new BuffSet();
            buffs.Add(PrototypeContent.BuffEndurance);
            buffs.Add(PrototypeContent.BuffEndurance);
            buffs.Add(PrototypeContent.BuffNimble);

            Assert.That(buffs.Active.Count, Is.EqualTo(2));
            Assert.That(System.Math.Abs(buffs.Multiplier(BuffStat.StaminaRegen) - (1.3f)) < 0.001f, Is.True);
            Assert.That(System.Math.Abs(buffs.Multiplier(BuffStat.NeedsDrain) - (1f)) < 0.001f, Is.True);

            buffs.Tick(PrototypeContent.BuffEndurance.Seconds + 1f);
            Assert.That(buffs.Active.Count, Is.EqualTo(0));
            Assert.That(System.Math.Abs(buffs.Multiplier(BuffStat.StaminaRegen) - (1f)) < 0.001f, Is.True);
        }

        [Test]
        public void EatingTheSameMealAgain_RestartsTheTimer()
        {
            var buffs = new BuffSet();
            buffs.Add(PrototypeContent.BuffMending);
            buffs.Tick(100f);
            buffs.Add(PrototypeContent.BuffMending);

            Assert.That(System.Math.Abs(buffs.Active[0].Remaining - (PrototypeContent.BuffMending.Seconds)) < 0.001f, Is.True);
        }

        [Test]
        public void Kitchen_CooksAMealFromProduce_AndEatingItStartsItsBuff()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.Stash.Add(PrototypeContent.SweetPotato, 2);
            session.Stash.Add(PrototypeContent.Wood, 1);

            Recipe recipe = null;
            foreach (Recipe candidate in session.Kitchen.Recipes)
            {
                if (candidate.Result.Item == PrototypeContent.RoastedSweetPotato) recipe = candidate;
            }

            Assert.That(session.Kitchen.Craft(recipe, session.Stash, session.Backpack), Is.EqualTo(CraftBlock.None));
            Assert.That(session.Stash.CountOf(PrototypeContent.SweetPotato.Id), Is.EqualTo(0));

            PlacedItem meal = null;
            foreach (PlacedItem placed in session.Stash.Items)
            {
                if (placed.Item == PrototypeContent.RoastedSweetPotato) meal = placed;
            }
            ConsumableDefinition effect = PrototypeContent.FindConsumable(meal.Item.Id);

            Assert.That(session.Consume(session.Stash, meal, effect), Is.True);
            Assert.That(session.Buffs.Has(PrototypeContent.BuffEndurance.Id), Is.True);
        }

        [Test]
        public void EveryKitchenRecipe_NeedsNoBlueprintOrUpgrade_AndItsMealHasABuff()
        {
            var kitchen = new Workshop();
            PrototypeContent.SetUpKitchen(kitchen);

            Assert.That(kitchen.Recipes.Count, Is.EqualTo(4));
            foreach (Recipe recipe in kitchen.Recipes)
            {
                Assert.That(recipe.WorkbenchLevel, Is.EqualTo(1));
                Assert.That(recipe.Blueprint == null, Is.True);

                ConsumableDefinition effect = PrototypeContent.FindConsumable(recipe.Result.Item.Id);
                Assert.That(effect != null && effect.Buff != null, Is.True);
                Assert.That(PrototypeContent.FindBuff(effect.Buff.Id) == effect.Buff, Is.True);
            }
        }

        [Test]
        public void Sleeping_EndsBuffs_ButDoesNotRefillTheCan()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.Buffs.Add(PrototypeContent.BuffNimble);
            session.Farm.Plots[0].Till();
            session.WateringCan.WaterPlot(session.Farm.Plots[0]);

            session.Sleep();

            Assert.That(session.Buffs.Active.Count, Is.EqualTo(0));
            Assert.That(session.WateringCan.Water, Is.EqualTo(session.WateringCan.Capacity - 1));
        }

        [Test]
        public void Save_KeepsCanWaterAndBuffs()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            session.Farm.Plots[0].Till();
            session.WateringCan.WaterPlot(session.Farm.Plots[0]);
            session.Buffs.Add(PrototypeContent.BuffHydrated);
            session.Buffs.Tick(100f);

            SaveData data = SaveMapper.Capture(session, false);
            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(data, 2, out report);

            Assert.That(loaded.WateringCan.Water, Is.EqualTo(session.WateringCan.Capacity - 1));
            Assert.That(loaded.Buffs.Has(PrototypeContent.BuffHydrated.Id), Is.True);
            Assert.That(System.Math.Abs(loaded.Buffs.Active[0].Remaining - (PrototypeContent.BuffHydrated.Seconds - 100f)) < 0.01f, Is.True);
        }

        [Test]
        public void OlderSave_LoadsWithAFullCanAndNoBuffs()
        {
            GameSession session = PrototypeContent.CreateNewSession(Gender.Male, 1);
            SaveData data = SaveMapper.Capture(session, false);
            data.version = 2;
            data.canSaved = false;
            data.canWater = 0;
            data.buffs = null;

            LoadReport report;
            GameSession loaded = PrototypeContent.LoadSession(data, 2, out report);

            Assert.That(loaded.WateringCan.IsFull, Is.True);
            Assert.That(loaded.Buffs.Active.Count, Is.EqualTo(0));
        }
    }
}
