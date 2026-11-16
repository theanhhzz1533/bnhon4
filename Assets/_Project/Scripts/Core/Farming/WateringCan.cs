using System;

namespace Bnhon.Core.Farming
{
    /// <summary>The player's watering can. Each plot watered uses one charge; it is refilled at the well.</summary>
    public sealed class WateringCan
    {
        public int Capacity { get; }
        public int Water { get; private set; }

        public bool IsEmpty => Water <= 0;
        public bool IsFull => Water >= Capacity;

        public WateringCan(int capacity = 6)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            Water = capacity;
        }

        /// <summary>Waters a plot. Nothing is used if the can is empty or the plot does not need water.</summary>
        public bool WaterPlot(FarmPlot plot)
        {
            if (plot == null || IsEmpty) return false;
            if (!plot.Water()) return false;
            Water--;
            return true;
        }

        public void Refill()
        {
            Water = Capacity;
        }

        public void Restore(int water)
        {
            Water = Math.Max(0, Math.Min(Capacity, water));
        }
    }
}
