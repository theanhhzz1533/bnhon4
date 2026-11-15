using System;

namespace Bnhon.Core.Calendar
{
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    /// <summary>Calendar: 7-day weeks, 28-day seasons, 4 seasons per year.</summary>
    public sealed class GameClock
    {
        public const int DaysPerWeek = 7;
        public const int DaysPerSeason = 28;
        public const int SeasonsPerYear = 4;

        /// <summary>Days elapsed since the start of the save (0 on the first day).</summary>
        public int TotalDays { get; private set; }

        public int Day => TotalDays % DaysPerSeason + 1;
        public Season Season => (Season)(TotalDays / DaysPerSeason % SeasonsPerYear);
        public int Year => TotalDays / (DaysPerSeason * SeasonsPerYear) + 1;

        /// <summary>0 is the first day of the week.</summary>
        public int DayOfWeek => TotalDays % DaysPerWeek;

        public event Action<GameClock> DayStarted;

        public GameClock(int totalDays = 0)
        {
            if (totalDays < 0) throw new ArgumentOutOfRangeException(nameof(totalDays));
            TotalDays = totalDays;
        }

        /// <summary>Sets the calendar from a save, without raising <see cref="DayStarted"/>.</summary>
        public void Restore(int totalDays)
        {
            TotalDays = Math.Max(0, totalDays);
        }

        public void AdvanceDay()
        {
            TotalDays++;
            DayStarted?.Invoke(this);
        }
    }
}
