using Bnhon.Core.Charms;

namespace Bnhon.Game.Combat
{
    /// <summary>What the worn charms add up to, for the code that applies them.</summary>
    public static class CharmEffects
    {
        public static float Get(CharmStat stat)
        {
            return GameRoot.Instance != null ? GameRoot.Instance.Session.Charms.Total(stat) : 0f;
        }

        /// <summary>1 minus the charm's fraction, never below 0.1: for things charms make smaller.</summary>
        public static float Less(CharmStat stat)
        {
            float value = 1f - Get(stat);
            return value < 0.1f ? 0.1f : value;
        }

        /// <summary>1 plus the charm's fraction: for things charms make bigger.</summary>
        public static float More(CharmStat stat)
        {
            return 1f + Get(stat);
        }
    }
}
