using System;
using Bnhon.Core.Calendar;
using Bnhon.Core.Farming;
using Bnhon.Core.Items;
using NUnit.Framework;

namespace Bnhon.Core.Tests
{
    public class FarmingTests
    {
        private static readonly ItemDefinition Seed = new ItemDefinition("seed", "Seed", ItemCategory.Seed);
        private static readonly ItemDefinition Produce = new ItemDefinition("produce", "Produce", ItemCategory.Produce);

        private static CropDefinition Crop(int days, int regrow = 0, SeasonMask seasons = SeasonMask.Spring)
        {
            return new CropDefinition("crop", "Crop", Seed, Produce, days, seasons, regrow, 2, 2);
        }

        private static FarmPlot PlantedPlot(CropDefinition crop)
        {
            var plot = new FarmPlot();
            plot.Till();
            plot.Plant(crop, Season.Spring);
            return plot;
        }

        [Test]
        public void Plant_RequiresTilledSoilAndRightSeason()
        {
            var plot = new FarmPlot();
            var crop = Crop(3);

            Assert.That(plot.Plant(crop, Season.Spring), Is.False);
            plot.Till();
            Assert.That(plot.Plant(crop, Season.Winter), Is.False);
            Assert.That(plot.Plant(crop, Season.Spring), Is.True);
            Assert.That(plot.State, Is.EqualTo(PlotState.Growing));
        }

        [Test]
        public void Crop_GrowsOnlyOnWateredDays()
        {
            var plot = PlantedPlot(Crop(2));

            plot.OnNewDay(Season.Spring);
            Assert.That(plot.GrowthDays, Is.EqualTo(0));

            plot.Water();
            plot.OnNewDay(Season.Spring);
            plot.Water();
            plot.OnNewDay(Season.Spring);

            Assert.That(plot.State, Is.EqualTo(PlotState.Ready));
        }

        [Test]
        public void Crop_DiesAfterThreeDryDaysInARow()
        {
            var plot = PlantedPlot(Crop(5));

            plot.OnNewDay(Season.Spring);
            plot.OnNewDay(Season.Spring);
            Assert.That(plot.State, Is.EqualTo(PlotState.Growing));

            plot.OnNewDay(Season.Spring);
            Assert.That(plot.State, Is.EqualTo(PlotState.Dead));
        }

        [Test]
        public void Crop_DiesWhenSeasonChanges()
        {
            var plot = PlantedPlot(Crop(5));
            plot.Water();

            plot.OnNewDay(Season.Summer);

            Assert.That(plot.State, Is.EqualTo(PlotState.Dead));
        }

        [Test]
        public void Harvest_SingleCropReturnsPlotToTilled()
        {
            var plot = PlantedPlot(Crop(1));
            plot.Water();
            plot.OnNewDay(Season.Spring);

            Assert.That(plot.Harvest(new Random(1)), Is.EqualTo(2));
            Assert.That(plot.State, Is.EqualTo(PlotState.Tilled));
            Assert.That(plot.Harvest(new Random(1)), Is.EqualTo(0));
        }

        [Test]
        public void Harvest_RegrowingCropNeedsOnlyRegrowDays()
        {
            var plot = PlantedPlot(Crop(4, regrow: 2));
            for (int i = 0; i < 4; i++)
            {
                plot.Water();
                plot.OnNewDay(Season.Spring);
            }

            plot.Harvest(new Random(1));
            Assert.That(plot.State, Is.EqualTo(PlotState.Growing));

            plot.Water();
            plot.OnNewDay(Season.Spring);
            Assert.That(plot.State, Is.EqualTo(PlotState.Growing));
            plot.Water();
            plot.OnNewDay(Season.Spring);
            Assert.That(plot.State, Is.EqualTo(PlotState.Ready));
        }

        [Test]
        public void Clock_RollsSeasonsAndYears()
        {
            var clock = new GameClock();
            Assert.That(clock.Day, Is.EqualTo(1));
            Assert.That(clock.Season, Is.EqualTo(Season.Spring));

            for (int i = 0; i < 28; i++) clock.AdvanceDay();
            Assert.That(clock.Day, Is.EqualTo(1));
            Assert.That(clock.Season, Is.EqualTo(Season.Summer));

            for (int i = 0; i < 28 * 3; i++) clock.AdvanceDay();
            Assert.That(clock.Season, Is.EqualTo(Season.Spring));
            Assert.That(clock.Year, Is.EqualTo(2));
        }
    }
}
