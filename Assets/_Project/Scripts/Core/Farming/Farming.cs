using System;
using System.Collections.Generic;
using Bnhon.Core.Items;
using Bnhon.Core.Calendar;

namespace Bnhon.Core.Farming
{
    [Flags]
    public enum SeasonMask
    {
        None = 0,
        Spring = 1,
        Summer = 2,
        Autumn = 4,
        Winter = 8,
        All = Spring | Summer | Autumn | Winter
    }

    public sealed class CropDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public ItemDefinition Seed { get; }
        public ItemDefinition Produce { get; }
        public int DaysToMature { get; }

        /// <summary>Days until the next harvest after picking. 0 means the plant is removed on harvest.</summary>
        public int RegrowDays { get; }
        public int MinYield { get; }
        public int MaxYield { get; }
        public SeasonMask Seasons { get; }

        public CropDefinition(
            string id,
            string displayName,
            ItemDefinition seed,
            ItemDefinition produce,
            int daysToMature,
            SeasonMask seasons,
            int regrowDays = 0,
            int minYield = 1,
            int maxYield = 1)
        {
            if (daysToMature < 1) throw new ArgumentOutOfRangeException(nameof(daysToMature));
            if (regrowDays < 0 || regrowDays > daysToMature) throw new ArgumentOutOfRangeException(nameof(regrowDays));
            if (minYield < 1 || maxYield < minYield) throw new ArgumentOutOfRangeException(nameof(minYield));

            Id = id;
            DisplayName = displayName;
            Seed = seed ?? throw new ArgumentNullException(nameof(seed));
            Produce = produce ?? throw new ArgumentNullException(nameof(produce));
            DaysToMature = daysToMature;
            Seasons = seasons;
            RegrowDays = regrowDays;
            MinYield = minYield;
            MaxYield = maxYield;
        }

        public bool GrowsIn(Season season) => (Seasons & (SeasonMask)(1 << (int)season)) != 0;
    }

    public enum PlotState
    {
        Untilled,
        Tilled,
        Growing,
        Ready,
        Dead
    }

    /// <summary>
    /// One farm tile. A crop grows one day for every day it was watered, and
    /// dies after more than <see cref="MaxDryDays"/> dry days in a row or
    /// when the season changes to one it cannot grow in.
    /// </summary>
    public sealed class FarmPlot
    {
        public const int MaxDryDays = 2;

        public PlotState State { get; private set; } = PlotState.Untilled;
        public CropDefinition Crop { get; private set; }
        public int GrowthDays { get; private set; }
        public int DryDays { get; private set; }
        public bool IsWatered { get; private set; }

        /// <summary>Sets the plot from a save. A growing or ready plot whose crop no longer exists becomes tilled soil.</summary>
        public void Restore(PlotState state, CropDefinition crop, int growthDays, int dryDays, bool watered)
        {
            bool needsCrop = state == PlotState.Growing || state == PlotState.Ready;
            if (needsCrop && crop == null)
            {
                state = PlotState.Tilled;
                growthDays = 0;
                dryDays = 0;
            }

            State = state;
            Crop = needsCrop ? crop : null;
            GrowthDays = Math.Max(0, growthDays);
            DryDays = Math.Max(0, dryDays);
            IsWatered = watered && state != PlotState.Untilled && state != PlotState.Dead;
        }

        public bool Till()
        {
            if (State != PlotState.Untilled && State != PlotState.Dead) return false;
            State = PlotState.Tilled;
            Crop = null;
            GrowthDays = 0;
            DryDays = 0;
            return true;
        }

        public bool Plant(CropDefinition crop, Season season)
        {
            if (crop == null || State != PlotState.Tilled || !crop.GrowsIn(season)) return false;
            Crop = crop;
            GrowthDays = 0;
            DryDays = 0;
            State = PlotState.Growing;
            return true;
        }

        public bool Water()
        {
            if (State == PlotState.Untilled || State == PlotState.Dead || IsWatered) return false;
            IsWatered = true;
            return true;
        }

        /// <summary>Call once when a new day starts, with the season of the new day.</summary>
        public void OnNewDay(Season season)
        {
            bool hasCrop = State == PlotState.Growing || State == PlotState.Ready;
            if (hasCrop && !Crop.GrowsIn(season))
            {
                State = PlotState.Dead;
            }
            else if (State == PlotState.Growing)
            {
                if (IsWatered)
                {
                    GrowthDays++;
                    DryDays = 0;
                    if (GrowthDays >= Crop.DaysToMature) State = PlotState.Ready;
                }
                else
                {
                    DryDays++;
                    if (DryDays > MaxDryDays) State = PlotState.Dead;
                }
            }

            IsWatered = false;
        }

        /// <summary>Returns the number of produce items harvested, or 0 if the plot is not ready.</summary>
        public int Harvest(Random rng)
        {
            if (State != PlotState.Ready) return 0;
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int yield = rng.Next(Crop.MinYield, Crop.MaxYield + 1);
            if (Crop.RegrowDays > 0)
            {
                GrowthDays = Crop.DaysToMature - Crop.RegrowDays;
                State = PlotState.Growing;
            }
            else
            {
                Crop = null;
                GrowthDays = 0;
                State = PlotState.Tilled;
            }
            return yield;
        }
    }

    public sealed class Farm
    {
        private readonly List<FarmPlot> _plots = new List<FarmPlot>();

        public IReadOnlyList<FarmPlot> Plots => _plots;

        public Farm(int plotCount)
        {
            AddPlots(plotCount);
        }

        public void AddPlots(int count)
        {
            for (int i = 0; i < count; i++) _plots.Add(new FarmPlot());
        }

        public void OnNewDay(Season season)
        {
            foreach (var plot in _plots) plot.OnNewDay(season);
        }
    }
}
