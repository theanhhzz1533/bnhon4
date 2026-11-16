using System;

namespace Bnhon.Core.Farming
{
    /// <summary>
    /// The player's watering can. Each plot watered uses one charge; it is refilled at the
    /// well. Upgrading it at the workbench adds charges.
    /// </summary>
    public sealed class WateringCan
    {
        public const int MaxLevel = 3;
        public const int ChargesPerLevel = 3;

        private readonly int _baseCapacity;

        public int Level { get; private set; } = 1;
        public int Capacity => _baseCapacity + (Level - 1) * ChargesPerLevel;
        public int Water { get; private set; }

        public bool IsEmpty => Water <= 0;
        public bool IsFull => Water >= Capacity;

        public WateringCan(int capacity = 6)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            _baseCapacity = capacity;
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

        /// <summary>Makes the can one level bigger and fills it. Returns false at the top level.</summary>
        public bool Upgrade()
        {
            if (Level >= MaxLevel) return false;
            Level++;
            Refill();
            return true;
        }

        public void Restore(int level, int water)
        {
            Level = Math.Max(1, Math.Min(MaxLevel, level));
            Water = Math.Max(0, Math.Min(Capacity, water));
        }
    }
}
