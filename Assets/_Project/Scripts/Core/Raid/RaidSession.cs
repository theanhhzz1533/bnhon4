using System.Collections.Generic;
using Bnhon.Core.Inventory;
using Bnhon.Core.Loot;

namespace Bnhon.Core.Raid
{
    public enum RaidState
    {
        InProgress,
        Extracted,
        Died
    }

    /// <summary>
    /// One trip outside. Loot goes into the backpack; it only becomes safe
    /// when the player extracts. Dying empties the backpack but never the
    /// secure pouch.
    /// </summary>
    public sealed class RaidSession
    {
        public GridInventory Backpack { get; }
        public GridInventory SecurePouch { get; }
        public RaidState State { get; private set; } = RaidState.InProgress;

        public RaidSession(GridInventory backpack, GridInventory securePouch)
        {
            Backpack = backpack;
            SecurePouch = securePouch;
        }

        public bool Extract()
        {
            if (State != RaidState.InProgress) return false;
            State = RaidState.Extracted;
            return true;
        }

        /// <summary>Ends the raid in death. Returns what was lost, so it can be left on the map as a corpse to recover.</summary>
        public List<LootDrop> Die()
        {
            var lost = new List<LootDrop>();
            if (State != RaidState.InProgress) return lost;

            foreach (var placed in Backpack.Items) lost.Add(new LootDrop(placed.Item, placed.Count));
            Backpack.Clear();
            State = RaidState.Died;
            return lost;
        }
    }
}
